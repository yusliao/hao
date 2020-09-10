using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Data;
using System.Data.Entity;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CsvHelper;
using DistrictService;
using OMS.Models;
using OMS.Models.DTO;
using PushServer.Configuration;
using PushServer.ModelServer;
using Util;
using Util.Files;

namespace PushServer.Commands
{
    [Export(typeof(IOrderOption))]
    public class JDExcelOrderOption : OrderOptionBase
    {
        public override string Name => OMS.Models.OrderSource.JINGDONG;
        private string NameDesc = Util.Helpers.Reflection.GetDescription<OrderSource>(OrderSource.JINGDONG);


        public override IClientConfig clientConfig => AppServer.Instance.ConfigDictionary[Name];

       

        protected  List<FileInfo> GetExcelFiles()
        {
            FileScanner.ScanAllExcelFiles(new DirectoryInfo(clientConfig.ExcelOrderFolder));
            if (FileScanner.ScannedFiles.Any())
            {
                return FileScanner.ScannedFiles;
            }
            else
                return new List<FileInfo>();
        }
       
      

        protected override List<OrderEntity> FetchOrders()
        {

            var ordersList = new List<OrderEntity>();

            foreach (var file in this.GetExcelFiles())
            {
                OnUIMessageEventHandle($"正在解析ERP导出单文件：{file.FullName}");

                using (var csv = new CsvReader(new StreamReader(file.FullName, Encoding.Default)))
                {
                    ResolveOrders_NoPhone(csv, file.FullName, ref ordersList);
                }
            }
            return ordersList;
        }


        /// <summary>
        /// 解析订单，订单来源ERP导出单
        /// </summary>
        /// <param name="csv">待解析目标对象</param>
        /// <param name="file">待解析目标文件名</param>
        /// <param name="items">已解析订单集合</param>
        protected void ResolveOrders(CsvReader csv, string file, ref List<OrderEntity> items)
        {
           
            csv.Read();
            csv.ReadHeader();
         
            OrderDTO orderDTO = new OrderDTO();
            orderDTO.fileName = file;
            List<string> badRecord = new List<string>();
            csv.Configuration.BadDataFound = context => badRecord.Add(context.RawRecord);
            while (csv.Read())
            {
                using (var db = new OMSContext())
                {
                    var desc = csv.GetField<string>("店铺名称").Trim();
                    
                    var config = AppServer.Instance.ConfigDictionary.Values.FirstOrDefault(c => c.Tag.Contains(desc));
                    if (config == null)
                    {
                        OnUIMessageEventHandle($"ERP导出单：{file}。未识别的订单渠道：{desc}");

                        continue;
                    }
                    else if (config.Name != OrderSource.JINGDONG && config.Name != OrderSource.TIANMAO)
                        continue;

                    orderDTO.sourceSN = csv.GetField<string>("平台单号").Trim();


                    if (string.IsNullOrEmpty(orderDTO.sourceSN))
                    {
                        //TODO:
                        InputExceptionOrder(orderDTO, ExceptionType.SourceSnIsNull);
                        continue;
                    }
                    string ordertype = csv.GetField<string>("订单类型").Trim();
                  
                    orderDTO.fileName = file;
                    orderDTO.source = config.Name;
                    orderDTO.sourceDesc = desc;
                    orderDTO.sourceSN = csv.GetField<string>("平台单号").Trim();

                    switch (ordertype)
                    {
                        case "销售订单":
                            orderDTO.orderType = 0;
                            orderDTO.orderStatus = OrderStatus.Delivered;
                            break;
                        case "换货订单":
                        case "补发货订单":
                            orderDTO.orderType = 1;
                            orderDTO.orderStatus = OrderStatus.Delivered;
                            break;
                        case "退货退钱订单":
                            orderDTO.orderType = 2;
                            orderDTO.orderStatus = OrderStatus.Cancelled;
                            break;
                        default:
                            orderDTO.orderType = 0;
                            orderDTO.orderStatus = OrderStatus.Delivered;
                            break;
                    }

                    var order = db.OrderSet.FirstOrDefault(o => o.SourceSn == orderDTO.sourceSN);
                    
                    if (order==null)//新增订单信息
                    {
                        // Util.Logs.Log.GetLog(nameof(AppServer)).Error($"ERP导出单：{file}。该文件中订单编号：{sn}在OMS系统中不存在");

                        ResolveOrdersFromERPExcel(csv, orderDTO, items);

                    }
                    else
                    {

                    }
                    OnUIMessageEventHandle($"ERP导出单：{file}。该文件中订单编号：{orderDTO.sourceSN}解析完毕");

                }
            }
          

            if (badRecord.Count > 0)
            {
                foreach (var item in badRecord)
                {
                    Util.Logs.Log.GetLog(nameof(ERPExcelOrderOption)).Debug(item);
                }

            }


        }
        /// <summary>
        /// 解析订单，订单来源京东仓，抓不到用户手机号
        /// </summary>
        /// <param name="csv"></param>
        /// <param name="file"></param>
        /// <param name="items"></param>
        protected void ResolveOrders_NoPhone(CsvReader csv, string file, ref List<OrderEntity> items)
        {

            csv.Read();
            csv.ReadHeader();

            OrderDTO orderDTO = new OrderDTO();
            orderDTO.fileName = file;
            List<string> badRecord = new List<string>();
            csv.Configuration.BadDataFound = context => badRecord.Add(context.RawRecord);
            while (csv.Read())
            {
                using (var db = new OMSContext())
                {

                    orderDTO.sourceSN = csv.GetField<string>("订单号").Trim();//京东订单号已科学计数的格式显示，需要变回数值
                    orderDTO.sourceSN = decimal.Parse(orderDTO.sourceSN, NumberStyles.Float).ToString();
                    var status = csv.GetField<string>("订单状态").Trim();

                    if (string.IsNullOrEmpty(orderDTO.sourceSN))
                    {
                        //TODO:
                        InputExceptionOrder(orderDTO, ExceptionType.SourceSnIsNull);
                        continue;
                    }
                    else if (status == "(删除)等待出库")
                        continue;
                    string ordertype = csv.GetField<string>("订单类型").Trim();

                    orderDTO.fileName = file;
                    orderDTO.source = OrderSource.JINGDONG;
                    orderDTO.sourceDesc = Util.Helpers.Reflection.GetDescription<OrderSource>(OrderSource.JINGDONG);
                  

                    switch (ordertype)
                    {
                        case "销售订单":
                            orderDTO.orderType = 0;
                            orderDTO.orderStatus = OrderStatus.Delivered;
                            break;
                        case "换货订单":
                        case "补发货订单":
                            orderDTO.orderType = 1;
                            orderDTO.orderStatus = OrderStatus.Delivered;
                            break;
                        case "退货退钱订单":
                            orderDTO.orderType = 2;
                            orderDTO.orderStatus = OrderStatus.Cancelled;
                            break;
                        default:
                            orderDTO.orderType = 0;
                            orderDTO.orderStatus = OrderStatus.Delivered;
                            break;
                    }

                    var order = db.OrderSet.FirstOrDefault(o => o.SourceSn == orderDTO.sourceSN&&o.Source==orderDTO.source);

                    if (order == null)//新增订单信息
                    {
                        // Util.Logs.Log.GetLog(nameof(AppServer)).Error($"ERP导出单：{file}。该文件中订单编号：{sn}在OMS系统中不存在");

                        ResolveOrdersFromJD(csv, orderDTO, items);

                    }
                    else
                    {

                    }
                    OnUIMessageEventHandle($"ERP导出单：{file}。该文件中订单编号：{orderDTO.sourceSN}解析完毕");

                }
            }


            if (badRecord.Count > 0)
            {
                foreach (var item in badRecord)
                {
                    Util.Logs.Log.GetLog(nameof(ERPExcelOrderOption)).Debug(item);
                }

            }


        }
        /// <summary>
        /// 从ERP导出单解析订单对象
        /// </summary>
        /// <param name="csv"></param>
        /// <param name="file"></param>
        /// <param name="items">已解析订单集合</param>
        /// <returns></returns>
        private OrderEntity ResolveOrdersFromERPExcel(CsvReader csv, OrderDTO orderDTO, List<OrderEntity> items)
        {

            orderDTO.orderSN = string.Format("{0}-{1}_{2}", orderDTO.source, orderDTO.sourceSN, DateTime.Now.ToString("yyyyMMdd"));
            var orderDate = csv.GetField<string>("付款时间");
            if (string.IsNullOrEmpty(orderDate))
                orderDate = csv.GetField<string>("配货时间");

            orderDTO.createdDate = DateTime.Parse(orderDate);


            orderDTO.productName = csv.GetField<string>("平台商品名称").Trim();
            orderDTO.productsku = csv.GetField<string>("商品代码").Trim();
            var quantity = orderDTO.count = csv.GetField<string>("订购数").ToInt();
            decimal weight = csv.GetField<string>("总重量").ToInt();
            orderDTO.consigneeName = csv.GetField<string>("收货人").Trim();
            orderDTO.consigneePhone = csv.GetField<string>("收货人手机").Trim();
            //京东天猫订单独有的金额信息
            orderDTO.pricePerUnit = csv.GetField<decimal>("实际单价");
            orderDTO.totalAmount = csv.GetField<decimal>("让利后金额");
            orderDTO.discountFee = csv.GetField<decimal>("让利金额");
           
            orderDTO.consigneeAddress = csv.GetField<string>("收货地址").Trim();
           

            var addrInfo = DistrictService.DistrictService.ResolveAddress(orderDTO.consigneeAddress);
            orderDTO.consigneeProvince = addrInfo.Province;
            orderDTO.consigneeCity = addrInfo.City;
            orderDTO.consigneeCounty = addrInfo.County;
                //   consigneeAddress = addrInfo.Address;
            
            orderDTO.consigneeZipCode = string.Empty;
            int weightcode = 0;
            csv.TryGetField<int>("规格代码", out weightcode);
            orderDTO.weightCode = weightcode;
            orderDTO.weightCodeDesc = csv.GetField<string>("规格名称");


            string ordertype = csv.GetField<string>("订单类型").Trim();
            orderDTO.OrderComeFrom = 0;


            /* 生成订单对象，从items集合中查找是否已经录入该订单对象
            * 如果items中已经有该订单对象则创建商品子对象及物流商品对象
            * 如果items中没有该订单对象则创建并关联各个子对象，将订单对象录入items
            * 重要提示：本方法解析csv对象，并转化为全新的订单对象，需要将订单的所有内容（重点是商品对象）都完整录入OMS系统中
            * 
            */
            var item = items.Find(o => o.OrderSn == orderDTO.orderSN);
            if (item == null)//集合中不存在该订单对象
            {
                var orderItem = OrderEntityService.CreateOrderEntity(orderDTO);


                if (orderItem.OrderType == 0)
                {
                    using (var db = new OMSContext())
                    {


                        //查找联系人
                        if (!string.IsNullOrEmpty(orderItem.Consignee.Phone))
                        {
                            OrderEntityService.InputConsigneeInfo(orderItem, db);

                        }
                        else //异常订单
                        {
                            InputExceptionOrder(orderDTO, ExceptionType.PhoneNumOrPersonNameIsNull);
                            return null;
                        }



                        if (!InputProductInfoWithoutSaveChange(db, orderDTO, orderItem))
                        {
                            return null;
                        }

                        db.OrderRepurchases.Add(orderItem.OrderRepurchase);
                        db.OrderDateInfos.Add(orderItem.OrderDateInfo);

                        //  db.OrderProductSet.Add(orderProductInfo);
                        db.SaveChanges();
                        items.Add(orderItem);
                        return orderItem;
                    }
                }
                else
                    return null;

            }
            else
            {
                orderDTO.consigneeName = csv.GetField<string>("收货人").Trim();
                orderDTO.consigneePhone = csv.GetField<string>("收货人手机").Trim();

                orderDTO.count = csv.GetField<string>("订购数").ToInt();

                using (var db = new OMSContext())
                {
                    InputProductInfoWithoutSaveChange(db, orderDTO, item);
                }
                return null;
            }



        }
        private OrderEntity ResolveOrdersFromJD(CsvReader csv, OrderDTO orderDTO, List<OrderEntity> items)
        {

            orderDTO.orderSN = string.Format("{0}-{1}_{2}", orderDTO.source, orderDTO.sourceSN, DateTime.Now.ToString("yyyyMMdd"));
            var orderDate = csv.GetField<string>("付款确认时间");
            if (string.IsNullOrEmpty(orderDate))
                orderDate = csv.GetField<string>("下单时间");

            orderDTO.createdDate = DateTime.Parse(orderDate);


            orderDTO.productName = csv.GetField<string>("商品名称").Trim();
            orderDTO.productsku = csv.GetField<string>("商品ID").Trim();
            var quantity = orderDTO.count = csv.GetField<string>("订购数量").ToInt();
           // decimal weight = csv.GetField<string>("总重量").ToInt();
            orderDTO.consigneeName = csv.GetField<string>("客户姓名").Trim();
            orderDTO.consigneePersonCard = csv.GetField<string>("下单帐号").Trim();
            orderDTO.consigneePhone = string.Empty;
            //京东天猫订单独有的金额信息
            orderDTO.pricePerUnit = csv.GetField<decimal>("京东价")/quantity;
            orderDTO.totalAmount = csv.GetField<decimal>("应付金额");
            orderDTO.discountFee = csv.GetField<decimal>("订单金额")-orderDTO.totalAmount;

            orderDTO.consigneeAddress = csv.GetField<string>("客户地址").Trim();


            var addrInfo = DistrictService.DistrictService.ResolveAddress(orderDTO.consigneeAddress);
            orderDTO.consigneeProvince = addrInfo.Province;
            orderDTO.consigneeCity = addrInfo.City;
            orderDTO.consigneeCounty = addrInfo.County;
            //   consigneeAddress = addrInfo.Address;

            orderDTO.consigneeZipCode = string.Empty;
            //int weightcode = 0;
            //csv.TryGetField<int>("规格代码", out weightcode);
            //orderDTO.weightCode = weightcode;
            //orderDTO.weightCodeDesc = csv.GetField<string>("规格名称");
            
            orderDTO.OrderComeFrom = 0;


            /* 生成订单对象，从items集合中查找是否已经录入该订单对象
            * 如果items中已经有该订单对象则创建商品子对象及物流商品对象
            * 如果items中没有该订单对象则创建并关联各个子对象，将订单对象录入items
            * 重要提示：本方法解析csv对象，并转化为全新的订单对象，需要将订单的所有内容（重点是商品对象）都完整录入OMS系统中
            * 
            */
            var item = items.Find(o => o.OrderSn == orderDTO.orderSN);
            if (item == null)//集合中不存在该订单对象
            {
                var orderItem = OrderEntityService.CreateOrderEntity(orderDTO);
                orderItem.Consignee.PersonCard = orderDTO.consigneePersonCard;

                if (orderItem.OrderType == 0)
                {
                    using (var db = new OMSContext())
                    {


                        //查找联系人
                        if (!string.IsNullOrEmpty(orderItem.Consignee.Phone)||!string.IsNullOrEmpty(orderItem.Consignee.PersonCard))
                        {
                            OrderEntityService.InputConsigneeInfo(orderItem, db);

                        }
                        else //异常订单
                        {
                            InputExceptionOrder(orderDTO, ExceptionType.PhoneNumOrPersonNameIsNull);
                            return null;
                        }



                        if (!InputProductInfoWithoutSaveChange_JD(db, orderDTO, orderItem))
                        {
                            return null;
                        }

                        db.OrderRepurchases.Add(orderItem.OrderRepurchase);
                        db.OrderDateInfos.Add(orderItem.OrderDateInfo);

                        //  db.OrderProductSet.Add(orderProductInfo);
                        db.SaveChanges();
                        items.Add(orderItem);
                        return orderItem;
                    }
                }
                else
                    return null;

            }
            else
            {
                orderDTO.consigneeName = csv.GetField<string>("客户姓名").Trim();
                orderDTO.consigneePhone = string.Empty;

                orderDTO.count = csv.GetField<string>("订购数量").ToInt();

                using (var db = new OMSContext())
                {
                    InputProductInfoWithoutSaveChange_JD(db, orderDTO, item);
                }
                return null;
            }



        }
        public override DataTable ExportExcel(List<OrderEntity> orders)
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("订单号");
            dt.Columns.Add("货单号");
            dt.Columns.Add("发货日期");
            dt.Columns.Add("发货时间");
            dt.Columns.Add("物流公司编码");
            dt.Columns.Add("物流公司");
            dt.Columns.Add("客服电话");
            dt.Columns.Add("客服网址");
            dt.Columns.Add("物流公司投递员");
            dt.Columns.Add("投递员手机号码");
            dt.Columns.Add("处理备注");
            dt.Columns.Add("物流平台");
            if(orders!=null&&orders.Any())
            {
                using (var db = new OMSContext())
                {
                    foreach (var item in orders)
                    {
                        if (item.OrderLogistics != null && item.OrderLogistics.Any())
                        {
                            foreach (var logisticsDetail in item.OrderLogistics)
                            {
                                var dr = dt.NewRow();
                                dr["订单号"] = item.SourceSn;
                                dr["货单号"] = logisticsDetail.LogisticsNo;
                                dr["发货日期"] = logisticsDetail.SendingTime.HasValue ? logisticsDetail.SendingTime.Value.ToShortDateString() : logisticsDetail.PickingTime.Value.ToShortDateString();
                                dr["发货时间"] = logisticsDetail.SendingTime.HasValue ? logisticsDetail.SendingTime.Value.ToShortTimeString() : logisticsDetail.PickingTime.Value.ToShortTimeString();
                                dr["物流公司编码"] = db.logisticsInfoSet.FirstOrDefault(l => l.FullName == logisticsDetail.Logistics)?.BankLogisticsCode;
                                dr["物流公司"] = logisticsDetail.Logistics;
                                dt.Rows.Add(dr);
                            }
                        }

                       
                    }
                }
            }
            return dt;
            
        }
        protected override bool InputProductInfoWithoutSaveChange(OMSContext db, OrderDTO orderDTO, OrderEntity item)
        {

            if (string.IsNullOrEmpty(orderDTO.productsku))//京东订单的productsku===sku,若值为null ,意味着用户取消了订单，这个订单不计入OMS
                return false;
            var foo = db.ProductsSet.Include(p => p.weightModel).FirstOrDefault(p => p.sku.Trim() == orderDTO.productsku);



           
            if (foo == null)
            {
                OnUIMessageEventHandle($"订单文件：{orderDTO.fileName}中平台单号：{orderDTO.sourceSN}（{orderDTO.productsku}）对应ERP商品记录未找到");
                InputExceptionOrder(orderDTO, ExceptionType.ProductCodeUnKnown);
                return false;
            }
            /*
             * 订单来源是ERP，同时商品SKU是周期购商品的SKU，判定为周期购日常发货订单
             * 周期购日常发货订单不纳入日常统计中，为了和客户下的周期购订单区分开
             * 统计报表中只统计销售订单
             * 
             */
            if (foo.sku == "S0010030002" || foo.sku == "S0010040002")//标识该订单是周期购订单
                item.OrderType += 4;
            var bar = item.Products.FirstOrDefault(p => p.sku == foo.sku);
            decimal weight = foo == null ? 0 : foo.QuantityPerUnit * orderDTO.count;
            if (bar == null)
            {

                OrderProductInfo orderProductInfo = new OrderProductInfo()
                {
                    ProductPlatId = orderDTO.productsku,
                    ProductPlatName = orderDTO.productName,
                    //   Warehouse = item.OrderLogistics.Logistics,
                    MonthNum = orderDTO.createdDate.Month,
                    weightCode = foo.weightModel == null ? 0 : foo.weightModel.Code,
                    weightCodeDesc = foo.weightModel == null ? string.Empty : $"{foo.weightModel.Value}g",
                    OrderSn = orderDTO.orderSN,
                    TotalAmount = orderDTO.totalAmount,
                    DiscountFee = orderDTO.discountFee,
                    AmounPerUnit = orderDTO.pricePerUnit,
                    ProductCount = orderDTO.count,
                    ProductWeight = weight,
                    Source = orderDTO.source,
                    sku = foo.sku
                };
                item.Products.Add(orderProductInfo);

            }
            else
            {
                bar.ProductWeight += weight;
                bar.ProductCount += orderDTO.count;
            }



            OnUIMessageEventHandle($"订单文件：{orderDTO.fileName}中平台单号：{orderDTO.sourceSN}（{orderDTO.productsku}）解析完毕");
            return true;
        }
        /// <summary>
        /// 订单商品来自京东仓，商品ID需要做对应关系 
        /// </summary>
        /// <param name="db"></param>
        /// <param name="orderDTO"></param>
        /// <param name="item"></param>
        /// <returns></returns>
        protected  bool InputProductInfoWithoutSaveChange_JD(OMSContext db, OrderDTO orderDTO, OrderEntity item)
        {
            ProductDictionary pd = null;
            pd = db.ProductDictionarySet.FirstOrDefault(p => p.ProductId.Trim() == orderDTO.productsku.Trim() && orderDTO.productsku != null && p.ProductCode != null);
            if (pd == null)
            {
                OnUIMessageEventHandle($"订单文件：{orderDTO.fileName}中平台单号：{orderDTO.sourceSN}（{orderDTO.productsku}）录入失败");
                InputExceptionOrder(orderDTO, ExceptionType.ProductIdUnKnown);
                if (db.ProductDictionarySet.FirstOrDefault(p => p.ProductId == orderDTO.productsku) == null)
                {
                    ProductDictionary productDictionary = new ProductDictionary()
                    {
                        ProductId = orderDTO.productsku,
                        Source = orderDTO.source,
                        ProductNameInPlatform = orderDTO.productName.Trim()
                    };
                    db.ProductDictionarySet.Add(productDictionary);
                    db.SaveChanges();
                }
                return false;
            }
            string temp = pd.ProductCode.Trim();//"S0010040003\t"
            var foo = db.ProductsSet.Include(p => p.weightModel).FirstOrDefault(p => p.sku.Trim() == temp);




            if (foo == null)
            {
                OnUIMessageEventHandle($"订单文件：{orderDTO.fileName}中平台单号：{orderDTO.sourceSN}（{orderDTO.productsku}）对应ERP商品记录未找到");
                InputExceptionOrder(orderDTO, ExceptionType.ProductCodeUnKnown);
                return false;
            }
            /*
             * 订单来源是ERP，同时商品SKU是周期购商品的SKU，判定为周期购日常发货订单
             * 周期购日常发货订单不纳入日常统计中，为了和客户下的周期购订单区分开
             * 统计报表中只统计销售订单
             * 
             */
            if (foo.sku == "S0010030002" || foo.sku == "S0010040002")//标识该订单是周期购订单
                item.OrderType += 4;
            var bar = item.Products.FirstOrDefault(p => p.sku == foo.sku);
            decimal weight = foo == null ? 0 : foo.QuantityPerUnit * orderDTO.count;
            if (bar == null)
            {

                OrderProductInfo orderProductInfo = new OrderProductInfo()
                {
                    ProductPlatId = orderDTO.productsku,
                    ProductPlatName = orderDTO.productName,
                    //   Warehouse = item.OrderLogistics.Logistics,
                    MonthNum = orderDTO.createdDate.Month,
                    weightCode = foo.weightModel == null ? 0 : foo.weightModel.Code,
                    weightCodeDesc = foo.weightModel == null ? string.Empty : $"{foo.weightModel.Value}g",
                    OrderSn = orderDTO.orderSN,
                    TotalAmount = orderDTO.totalAmount,
                    DiscountFee = orderDTO.discountFee,
                    AmounPerUnit = orderDTO.pricePerUnit,
                    ProductCount = orderDTO.count,
                    ProductWeight = weight,
                    Source = orderDTO.source,
                    sku = foo.sku
                };
                item.Products.Add(orderProductInfo);

            }
            else
            {
                bar.ProductWeight += weight;
                bar.ProductCount += orderDTO.count;
            }



            OnUIMessageEventHandle($"订单文件：{orderDTO.fileName}中平台单号：{orderDTO.sourceSN}（{orderDTO.productsku}）解析完毕");
            return true;
        }
    }
}
