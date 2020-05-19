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

using OMS.Models;
using OMS.Models.DTO;
using PushServer.Configuration;
using PushServer.ModelServer;

namespace PushServer.Commands
{
    /// <summary>
    /// 兴业银行积分
    /// 特点：EXCEL中提供订单号和订单商品编号，商品名称
    /// </summary>
    [Export(typeof(IOrderOption))]
    class OfflineExcelOrderOption : OrderOptionBase
    {
        public override string Name => OMS.Models.OrderSource.OFFLINE;

        public override IClientConfig clientConfig => AppServer.Instance.ConfigDictionary[Name];

        public override DataTable ExportExcel(List<OrderEntity> orders)
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("订单号");
            dt.Columns.Add("物流编号");
            dt.Columns.Add("物流单号");
            if (orders != null && orders.Any())
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
                                dr["物流单号"] = logisticsDetail.LogisticsNo;

                                dr["物流编号"] = db.logisticsInfoSet.FirstOrDefault(l => l.FullName == logisticsDetail.Logistics)?.BankLogisticsCode;

                                dt.Rows.Add(dr);
                            }
                        }

                    }
                }
            }
            return dt;

        }
        protected override List<OrderEntity> FetchOrders()
        {
            var ordersList = new List<OrderEntity>();

            foreach (var file in this.GetExcelFiles())
            {
                using (var excel = new NPOIExcel(file.FullName))
                {

                    var table = excel.ExcelToDataTable(null, 1);
                    if (table != null)
                        this.ResolveOrders(table, file.FullName, ordersList);
                    else
                    {
                        OnUIMessageEventHandle($"兴业积分APP导入文件：{file.FileName}解析完毕,当前订单数{ordersList.Count}");
                        continue;
                    }
                    
                }
                OnUIMessageEventHandle($"兴业积分APP导入文件：{file.FileName}解析完毕,当前订单数{ordersList.Count}");
            }
            return ordersList;
        }
        protected  List<DataFileInfo> GetExcelFiles()
        {
            var excelFileList = new List<DataFileInfo>();

            FileScanner.ScanAllFiles(new DirectoryInfo(clientConfig.ExcelOrderFolder), "*.xls");
            if (FileScanner.ScannedFiles.Any())
            {
                FileScanner.ScannedFiles.ForEach(file =>
                {
                    var dateStr = file.Name.Split('.').First().Split('_').Last().Substring(0, 8);
                    var fileDate = DateTime.ParseExact(dateStr, "yyyyMMdd", CultureInfo.InvariantCulture);

                    excelFileList.Add(new DataFileInfo(fileDate, file.Name, file.FullName));
                });
            }
            excelFileList = excelFileList.OrderBy(f => f.FileDate).ToList();
            
            return excelFileList;
        }


        protected  List<OrderEntity> ResolveOrders(DataTable excelTable,string file, List<OrderEntity> items)
        {

            BusinessOrderDTO orderDTO = new BusinessOrderDTO();
            orderDTO.orderStatus = OrderStatus.Confirmed;
            orderDTO.fileName = file;
            orderDTO.orderType = 0;
            orderDTO.orderStatus = OrderStatus.Confirmed;
           

            for (int i = 1; i < excelTable.Rows.Count; i++)
            {
                var row = excelTable.Rows[i];

                var orderDateStr = Convert.ToString(row["订购日期"]); //订单创建时间
                orderDTO.createdDate = DateTime.Parse(orderDateStr);

                orderDTO.source = Name;
                orderDTO.sourceDesc = Util.Helpers.Reflection.GetDescription<OrderSource>(Name);

                orderDTO.sourceSN = Convert.ToString(row["订购单号"]); //订单号
                if (string.IsNullOrEmpty(orderDTO.sourceSN))
                {
                    InputExceptionOrder(orderDTO, ExceptionType.SourceSnIsNull);
                    continue;
                }


                orderDTO.productName = Convert.ToString(row["商品名称"]); //商品名称
                orderDTO.productsku = Convert.ToString(row["商品代码"]); //商品编号
                orderDTO.productsku = Convert.ToString(row["单价"]); //商品编号
                orderDTO.productsku = Convert.ToString(row["优惠金额"]); //商品编号
                orderDTO.productsku = Convert.ToString(row["金额"]); //商品编号
                orderDTO.count = Convert.ToInt32(row["数量"]); //数量
                orderDTO.orderSN = string.Format("{0}-{1}_{2}", orderDTO.source, orderDTO.sourceSN, orderDTO.createdDate.ToString("yyyyMMdd"));
                if (CheckOrderInDataBase(orderDTO))
                    continue;
                var item = items.Find(o => o.OrderSn == orderDTO.orderSN);
                if (item == null)
                {
                   
                   


                    orderDTO.consigneeName = Convert.ToString(row["收货人"]); //收件人
                    orderDTO.consigneePhone = Convert.ToString(row["电话"]); //联系电话
                    orderDTO.consigneePhone2 = string.Empty;
                 

                    orderDTO.consigneeProvince = string.Empty;
                    orderDTO.consigneeCity = string.Empty;
                    orderDTO.consigneeCounty = string.Empty;
                    orderDTO.consigneeAddress = Convert.ToString(row[8]); //收货地区+详细地址
                    orderDTO.consigneeZipCode = Convert.ToString(row[9]); //邮编

                    //
                    if (string.IsNullOrEmpty(orderDTO.consigneeProvince)
                        && string.IsNullOrEmpty(orderDTO.consigneeCity) && !string.IsNullOrEmpty(orderDTO.consigneeAddress))
                    {
                        
                        var addrInfo = DistrictService.DistrictService.ResolveAddress(orderDTO.consigneeAddress);
                        orderDTO.consigneeProvince = addrInfo.Province;
                        orderDTO.consigneeCity = addrInfo.City;
                        orderDTO.consigneeCounty = addrInfo.County;
                     //   consigneeAddress = addrInfo.Address;
                    }

                   
                    var totalQuantity = orderDTO.count;
                   

                    //是否需要发票
                    orderDTO.Buyer.NeedInvoice  = Convert.ToString(row["是否开票"]); //是否需要发票
                    orderDTO.Buyer.InvoiceType = Convert.ToString(row["发票类别"]);
                    orderDTO.Buyer.InvoiceValue = Convert.ToSingle(row["税率"]);
                    orderDTO.Buyer.Paymentmark = Convert.ToString(row["付款约定"]);
                    orderDTO.Buyer.PaymentType = Convert.ToString(row["支付方式"]);
                    orderDTO.Buyer.ProjectName = Convert.ToString(row["所属项目"]);
                    orderDTO.Buyer.DeliverType = Convert.ToString(row["交货方式"]);
                    orderDTO.Buyer.ContractCode = Convert.ToString(row["合同编号"]);
                    orderDTO.Buyer.Name = Convert.ToString(row["采购方"]);
                    if(string.IsNullOrEmpty(orderDTO.Buyer.Name))
                    {
                        InputExceptionOrder(orderDTO, ExceptionType.PhoneNumOrPersonNameIsNull);

                        continue;
                    }
                    orderDTO.Buyer.InvoiceName = Convert.ToString(row["发票抬头"]); //发票抬头
                    if (orderDTO.Buyer.NeedInvoice.Equals("否"))
                        orderDTO.Buyer.InvoiceType = orderDTO.Buyer.InvoiceName = string.Empty;
                    if (string.IsNullOrEmpty(orderDTO.consigneeName))
                        orderDTO.consigneeName = orderDTO.Buyer.Name;
                    OrderEntity orderItem = OrderEntityService.CreateOrderEntity(orderDTO);
                    orderItem.OrderExtendInfo.Buyer = orderDTO.Buyer;
                    orderItem.OrderExtendInfo.Supplier = orderDTO.Supplier;
                    using (var db = new OMSContext())
                    {
                        //查找联系人
                        if (!string.IsNullOrEmpty(orderItem.Consignee.Phone))
                        {
                            OrderEntityService.InputBusinessConsigneeInfo(orderItem, db);

                        }
                        else //异常订单
                        {
                            InputExceptionOrder( orderDTO, ExceptionType.PhoneNumOrPersonNameIsNull);
                            continue;
                        }

                        if (!InputProductInfoWithoutSaveChange(db, orderDTO, orderItem))
                        {
                            continue;
                        }
                        items.Add(orderItem);
                        if (orderItem.OrderRepurchase == null)
                            orderItem.OrderRepurchase = new OrderRepurchase();
                        db.OrderRepurchases.Add(orderItem.OrderRepurchase);
                        db.OrderDateInfos.Add(orderItem.OrderDateInfo);
                      
                        db.SaveChanges();
                    }
                    
                }
                else
                {
                    orderDTO.productName = Convert.ToString(row[3]); //商品名称
                    orderDTO.productsku = Convert.ToString(row[2]); //商品编号

                    orderDTO.count = Convert.ToInt32(row[4]); //数量

                    using (var db = new OMSContext())
                    {
                        InputProductInfoWithoutSaveChange(db, orderDTO, item);
                    }

                    
                }
            }

            return items;
        }
        protected override  bool CheckOrderInDataBase(OrderDTO orderDTO)
        {
            /*检查订单是否已经存在在数据库中
             * 如果订单不存在于数据库中，返回false,结束
             * 如果订单存在于数据库中：
             * 检查订单是否取消订单，如果是取消订单，则减去相应商品的数量和重量信息；
             * 如果不是取消清单，则录入该订单商品
             */

            using (var db = new OMSContext())
            {
              
                var foo1 = db.OrderSet.Include(o => o.Products).FirstOrDefault(o => o.OrderSn == orderDTO.orderSN);//订单在数据库中
                if (foo1 != null)//系统中已经存在该订单
                {
                    //取消订单只存在于兴业积点渠道，这个渠道的商品没有商品编号
                    if (orderDTO.orderStatus == OrderStatus.Cancelled)//是否取消订单
                    {

                        var bar = db.ProductDictionarySet.FirstOrDefault(x => x.ProductNameInPlatform.Trim() == orderDTO.productName.Trim());
                        if (bar != null && !string.IsNullOrEmpty(bar.ProductCode))
                        {
                            var p1 = db.ProductsSet.Include(x => x.weightModel).FirstOrDefault(x => x.sku == bar.ProductCode);
                            if (p1 != null)
                            {
                                decimal weight = foo1 == null ? 0 : p1.QuantityPerUnit * orderDTO.count;
                                var p = foo1.Products.FirstOrDefault(o => o.sku == p1.sku);
                                if (p != null)
                                {

                                    p.ProductCount -= orderDTO.count;
                                    p.ProductWeight -= weight;

                                    db.SaveChanges();

                                }
                            }
                        }
                    }
                    else
                    {
                        InputProductInfoWithSaveChange(db, orderDTO, foo1);

                    }
                    return true;

                }
                else
                    return false;
            }
        }
        protected  bool InputProductInfoWithSaveChange(OMSContext db, OrderDTO orderDTO, OrderEntity item)
        {


            //判断该商品的ERP编号是否存在，不存在则停止录入
            string temp = orderDTO.productsku.Trim();
            var foo = db.ProductsSet.Include(p => p.weightModel).FirstOrDefault(p => p.sku.Trim() == temp);
            if (foo == null)
            {
                OnUIMessageEventHandle($"订单文件：{orderDTO.fileName}中平台单号：{orderDTO.sourceSN}（{orderDTO.productsku}）对应ERP商品记录未找到");
                InputExceptionOrder(orderDTO, ExceptionType.ProductCodeUnKnown);
                return false;
            }

            if (item.Products.FirstOrDefault(p => p.sku == foo.sku) == null)
            {
                decimal weight = foo == null ? 0 : foo.QuantityPerUnit * orderDTO.count;
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
                    AmounPerUnit = orderDTO.pricePerUnit,
                    DiscountFee = orderDTO.discountFee,
                    ProductCount = orderDTO.count,
                    ProductWeight = weight,
                    Source = orderDTO.source,
                    sku = foo.sku
                };
                item.Products.Add(orderProductInfo);
                OnUIMessageEventHandle($"订单文件：{orderDTO.fileName}中平台单号：{orderDTO.sourceSN}（{orderDTO.productsku}）解析完毕");
            }
            else
            {
                OnUIMessageEventHandle($"订单文件：{orderDTO.fileName}中平台单号：{orderDTO.sourceSN}（{orderDTO.productsku}）重复录入");
            }
            db.SaveChanges();


            return true;


        }
    }
}
