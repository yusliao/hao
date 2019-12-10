using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Data;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using CsvHelper;
using OMS.Models;
using OMS.Models.DTO;
using PushServer.Configuration;
using PushServer.ModelServer;
using Util;

namespace PushServer.Commands
{
    /// <summary>
    /// ERP相关操作
    /// </summary>
    public class ERPExcelOrderOption : OrderOptionBase
    {
        public override string Name => OrderSource.ERP;
        [ImportMany(typeof(IOrderOption))]
        private IEnumerable<IOrderOption> OrderOptSet { get; set; }

        public override IClientConfig clientConfig => AppServer.Instance.ConfigDictionary[Name];
        public ERPExcelOrderOption()
        {
            #region MEF配置
            MyComposePart();
            #endregion
        }
        void MyComposePart()
        {
            var catalog = new AssemblyCatalog(Assembly.GetExecutingAssembly());
            var container = new CompositionContainer(catalog);
            //将部件（part）和宿主程序添加到组合容器
            container.ComposeParts(this);
        }

        public override DataTable ExportExcel(List<OrderEntity> orders)
        {
            return null;
        }
        public  bool ImportErpToOMS()
        {
            var lst = FetchOrders();
            if (lst != null && lst.Any())
            {
                OnUIMessageEventHandle($"正在生成ERP回传物流订单,订单数量：{lst.Count}");
               
                OnPostCompleted(true, lst,OptionType.LogisticsExcel);
                OnExceptionMessageEventHandle(exceptionOrders);
                return true;
            }
            else
                return false;
        }
       

        protected override List<OrderEntity> FetchOrders()
        {
            var ordersList = new List<OrderEntity>();

            foreach (var file in this.GetExcelFiles())
            {
                OnUIMessageEventHandle($"正在解析ERP导出单文件：{file.FullName}");
                
                using (var csv = new CsvReader(new StreamReader(file.FullName, Encoding.Default)))
                {
                    ResolveOrders(csv, file.FullName, ref ordersList);
                }
            }
            return ordersList;
        }
        protected List<FileInfo> GetExcelFiles()
        {


            FileScanner.ScanAllFiles(new DirectoryInfo(Path.Combine(clientConfig.ExcelOrderFolder, "export")), "*.csv");
            if (FileScanner.ScannedFiles.Any())
            {
                return FileScanner.ScannedFiles;
            }
            else
                return new List<FileInfo>();
        }
        /// <summary>
        /// 解析订单
        /// </summary>
        /// <param name="csv">待解析目标对象</param>
        /// <param name="file">待解析目标文件名</param>
        /// <param name="items">已解析订单集合</param>
        protected void ResolveOrders(CsvReader csv, string file, ref List<OrderEntity> items)
        {
            csv.Read();
            csv.ReadHeader();
            List<OrderEntity> newOrderlst = new List<OrderEntity>();
            OrderDTO orderDTO = new OrderDTO();
            orderDTO.fileName = file;
            orderDTO.source = Name;
            orderDTO.sourceDesc = Util.Helpers.Reflection.GetDescription<OrderSource>(Name);
            while (csv.Read())
            {
                /*处理逻辑：
                 * 平台单号是否为空
                 * 销售订单 平台单号是否存在数据库中
                 * 
                 */ 

                using (var db = new OMSContext())
                {
                    
                    orderDTO.sourceSN = csv.GetField<string>("平台单号").Trim();
                    if(string.IsNullOrEmpty(orderDTO.sourceSN))
                    {
                        //TODO:
                        InputExceptionOrder(orderDTO, ExceptionType.SourceSnIsNull);
                        continue;
                    }
                    string ordertype = csv.GetField<string>("订单类型").Trim();
                   
                   
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

                    var order = db.OrderSet.Include(o => o.OrderLogistics).Include(o => o.Products).FirstOrDefault(o => o.SourceSn == orderDTO.sourceSN);
                    if (order != null)//找到该订单
                    {
                        string warehouse = csv.GetField<string>("仓库名称");
                        string pcode = csv.GetField<string>("商品代码").Trim();//ERP标识的商品编码
                        decimal weight = csv.GetField<string>("总重量").ToInt();
                        int count = csv.GetField<string>("数量").ToInt();
                        order.OrderType = orderDTO.orderType;
                        order.OrderStatus = (int)orderDTO.orderStatus;
                        order.OrderStatusDesc= Util.Helpers.Enum.GetDescription<OrderStatus>(orderDTO.orderStatus); 
                        OrderLogisticsDetail orderLogisticsDetail = new OrderLogisticsDetail();
                        orderLogisticsDetail.Logistics = csv.GetField<string>("物流公司");
                        orderLogisticsDetail.LogisticsNo = csv.GetField<string>("物流单号").Trim();
                       // orderLogisticsDetail.LogisticsPrice = csv.GetField<string>("物流费用")?.ToDecimalOrNull();
                        orderLogisticsDetail.PickingTime = DateTime.Parse(csv.GetField<string>("配货时间"));
                        //发货时间为空时，从配货时间中取日期作为发货时间

                        orderLogisticsDetail.SendingTime = csv.GetField<string>("发货时间") == "" ? orderLogisticsDetail.PickingTime.Value.Date : DateTime.Parse(csv.GetField<string>("发货时间"));
                        var templ = order.OrderLogistics.FirstOrDefault(o => o.LogisticsNo == orderLogisticsDetail.LogisticsNo);//是否已经存在该物流信息，防止重复添加
                        if (templ == null)
                        {
                            if (!string.IsNullOrEmpty(orderLogisticsDetail.LogisticsNo))//物流单号为空，不保存此信息
                            {
                                db.OrderLogisticsDetailSet.Add(orderLogisticsDetail);
                                order.OrderLogistics.Add(orderLogisticsDetail);
                                //关联物流商品信息
                                if (orderLogisticsDetail.LogisticsProducts == null)
                                    orderLogisticsDetail.LogisticsProducts = new List<LogisticsProductInfo>();
                                LogisticsProductInfo logisticsProductInfo = new LogisticsProductInfo()
                                {
                                    ProductCount = count,
                                    ProductPlatId = pcode,
                                    ProductPlatName = csv.GetField<string>("商品名称").Trim(),
                                    ProductWeight = weight,
                                    sku = pcode,
                                    Warehouse = warehouse,
                                    weightCode = csv.GetField<string>("规格代码").ToInt(),
                                    weightCodeDesc = csv.GetField<string>("规格名称").Trim()
                                };
                                orderLogisticsDetail.LogisticsProducts.Add(logisticsProductInfo);
                                db.LogisticsProductInfos.Add(logisticsProductInfo);
                            }

                        }
                        db.SaveChanges();
                        items.Add(order);

                    }
                    else//新增订单信息
                    {
                        // Util.Logs.Log.GetLog(nameof(AppServer)).Error($"ERP导出单：{file}。该文件中订单编号：{sn}在OMS系统中不存在");
                     
                        var item = ResolveOrdersFromERPExcel(csv, file, items);
                        if (item != null)
                            newOrderlst.Add(item);
                        
                       

                    }
                    OnUIMessageEventHandle($"ERP导出单：{file}。该文件中订单编号：{orderDTO.sourceSN}解析完毕");
                   
                }
            }
            if(newOrderlst.Any())
                InsertDB(newOrderlst);


        }
        /// <summary>
        /// 从ERP导出单解析订单对象
        /// </summary>
        /// <param name="csv"></param>
        /// <param name="file"></param>
        /// <param name="items">已解析订单集合</param>
        /// <returns></returns>
        private OrderEntity ResolveOrdersFromERPExcel(CsvReader csv, string file, List<OrderEntity> items)
        {
            var desc = csv.GetField<string>("店铺名称").Trim();
            var opt = this.OrderOptSet.FirstOrDefault(o => Util.Helpers.Reflection.GetDescription<OrderSource>(o.clientConfig.Name.ToUpper()) == desc);
            if (opt == null)
            {
                OnUIMessageEventHandle($"ERP导出单：{file}。未识别的订单渠道：{desc}");
                
                return null;
            }
            OrderDTO orderDTO = new OrderDTO();
            orderDTO.fileName = file;
            orderDTO.source = opt.clientConfig.Name;
            orderDTO.sourceDesc = desc;
            orderDTO.sourceSN = csv.GetField<string>("平台单号").Trim();
            if (string.IsNullOrEmpty(orderDTO.sourceSN))
                return null;
          
            orderDTO.orderSN = string.Format("{0}-{1}_{2}", orderDTO.source, orderDTO.sourceSN, DateTime.Now.ToString("yyyyMMdd"));
            var orderDate = csv.GetField<string>("付款时间");
            if (string.IsNullOrEmpty(orderDate))
                orderDate = csv.GetField<string>("配货时间");

            orderDTO.createdDate = DateTime.Parse(orderDate);


            orderDTO.productName = csv.GetField<string>("平台商品名称").Trim();
            orderDTO.productsku = csv.GetField<string>("商品代码").Trim();
            var quantity = orderDTO.count=  csv.GetField<string>("数量").ToInt();
            decimal weight  = csv.GetField<string>("总重量").ToInt();
            orderDTO.consigneeName = csv.GetField<string>("收货人").Trim();
            orderDTO.consigneePhone = csv.GetField<string>("收货人手机").Trim();
            if (string.IsNullOrEmpty(orderDTO.consigneePhone))
                orderDTO.consigneePhone = csv.GetField<string>("收货人电话").Trim();

            orderDTO.consigneePhone2 = csv.GetField<string>("收货人电话").Trim();

            orderDTO.consigneeProvince = csv.GetField<string>("省").Trim();
            orderDTO.consigneeCity = csv.GetField<string>("市").Trim();
            orderDTO.consigneeCounty = csv.GetField<string>("区/县").Trim();
            orderDTO.consigneeAddress = csv.GetField<string>("收货地址").Trim();
            orderDTO.consigneeZipCode = string.Empty;
            orderDTO.Warehouse = csv.GetField<string>("仓库名称").Trim();
            orderDTO.weightCode = csv.GetField<int>("规格代码");
            orderDTO.weightCodeDesc = csv.GetField<string>("规格名称");

            
            string ordertype = csv.GetField<string>("订单类型").Trim();
            orderDTO.OrderComeFrom = 2;
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
                    break;
            }
           
            //这里的订单都是数据库中没有的订单
            //已解析集合中查找，没找到就新增对象，找到就关联新的商品
            var item = items.Find(o => o.OrderSn == orderDTO.orderSN);
            if (item == null)//集合中不存在该订单对象
            {
                var orderItem = OrderEntityService.CreateOrderEntity(orderDTO);
                
                //售后单与原始订单建立联系
                if (orderItem.OrderType == 1)
                {
                    using (var db = new OMSContext())
                    {
                        string sn = orderItem.SourceSn.Substring(0, orderItem.SourceSn.Length - 2);//通过截断字符串匹配原始订单号

                        var sourceorder = db.OrderSet.FirstOrDefault(o => o.SourceSn == sn.Trim() && o.OrderType == 0);
                        if (sourceorder != null)
                        {
                            var targetorder = db.OrderSet.FirstOrDefault(o => o.SourceSn == orderItem.SourceSn);//售后单是否已经入库
                            if (targetorder == null)
                                InputOrderSubRecordInfo(csv, orderDTO, quantity, weight, orderItem, db, sourceorder);
                            else
                            {

                                OrderLogisticsDetail orderLogisticsDetail = new OrderLogisticsDetail();
                                orderLogisticsDetail.OrderSn = targetorder.OrderSn;
                                orderLogisticsDetail.Logistics = csv.GetField<string>("物流公司");
                                orderLogisticsDetail.LogisticsNo = csv.GetField<string>("物流单号").Trim();
                              //  orderLogisticsDetail.LogisticsPrice = csv.GetField<string>("物流费用").ToDecimalOrNull();
                                orderLogisticsDetail.PickingTime = DateTime.Parse(csv.GetField<string>("配货时间"));
                                //发货时间为空时，从配货时间中取日期作为发货时间
                                orderLogisticsDetail.SendingTime = csv.GetField<string>("发货时间") == "" ? orderLogisticsDetail.PickingTime.Value.Date : DateTime.Parse(csv.GetField<string>("发货时间"));
                                var templ = targetorder.OrderLogistics.FirstOrDefault(o => o.LogisticsNo == orderLogisticsDetail.LogisticsNo && !string.IsNullOrEmpty(orderLogisticsDetail.LogisticsNo));//是否已经存在该物流信息，防止重复添加
                                if (templ == null)
                                {
                                    if (!string.IsNullOrEmpty(orderLogisticsDetail.LogisticsNo))//物流单号为空，不保存此信息
                                    {
                                        //db.OrderLogisticsDetailSet.Add(orderLogisticsDetail);
                                        targetorder.OrderLogistics.Add(orderLogisticsDetail);
                                        if (orderLogisticsDetail.LogisticsProducts == null)
                                            orderLogisticsDetail.LogisticsProducts = new List<LogisticsProductInfo>();
                                        LogisticsProductInfo logisticsProductInfo = new LogisticsProductInfo()
                                        {
                                            ProductCount = quantity,
                                            ProductPlatId = orderDTO.productsku,
                                            ProductPlatName = orderDTO.productName,
                                            ProductWeight = weight,
                                            sku = orderDTO.productsku,
                                            Warehouse = orderDTO.Warehouse,
                                            weightCode = orderDTO.weightCode,
                                            weightCodeDesc = orderDTO.weightCodeDesc
                                        };
                                        orderLogisticsDetail.LogisticsProducts.Add(logisticsProductInfo);
                                        db.LogisticsProductInfos.Add(logisticsProductInfo);
                                    }

                                }
                                else
                                {
                                    LogisticsProductInfo logisticsProductInfo = new LogisticsProductInfo()
                                    {
                                        ProductCount = quantity,
                                        ProductPlatId = orderDTO.productsku,
                                        ProductPlatName = orderDTO.productName,
                                        ProductWeight = weight,
                                        sku = orderDTO.productsku,
                                        Warehouse = orderDTO.Warehouse,
                                        weightCode = orderDTO.weightCode,
                                        weightCodeDesc = orderDTO.weightCodeDesc
                                    };
                                    templ.LogisticsProducts.Add(logisticsProductInfo);
                                    db.LogisticsProductInfos.Add(logisticsProductInfo);
                                }

                                db.SaveChanges();

                            }
                        }
                        else//通过收货人找到最近的一个销售订单
                        {
                            InputExceptionOrder(orderDTO, ExceptionType.OrderNoExistedFromSubOrder);
                            return null;
                            OrderEntity lastorder = null;
                            try
                            {
                                lastorder = db.OrderSet.Include(o => o.Consignee).Where(o => o.Consignee.Name == orderDTO.consigneeName && o.Consignee.Phone == orderDTO.consigneePhone && o.OrderType == 0).FirstOrDefault();
                            }
                            catch (Exception)
                            {
                                InputExceptionOrder(orderDTO, ExceptionType.OrderNoExistedFromSubOrder);
                                return null;
                            }
                            
                            if (lastorder != null)
                            {
                                var targetorder = db.OrderSet.FirstOrDefault(o => o.SourceSn == orderItem.SourceSn);//售后单是否已经入库
                                if (targetorder == null)
                                    return InputOrderSubRecordInfo(csv, orderDTO, quantity, weight, orderItem, db, sourceorder);
                                else
                                {

                                    OrderLogisticsDetail orderLogisticsDetail = new OrderLogisticsDetail();
                                    orderLogisticsDetail.OrderSn = targetorder.OrderSn;
                                    orderLogisticsDetail.Logistics = csv.GetField<string>("物流公司");
                                    orderLogisticsDetail.LogisticsNo = csv.GetField<string>("物流单号").Trim();
                                  //  orderLogisticsDetail.LogisticsPrice = csv.GetField<string>("物流费用").ToDecimalOrNull();
                                    orderLogisticsDetail.PickingTime = DateTime.Parse(csv.GetField<string>("配货时间"));
                                    //发货时间为空时，从配货时间中取日期作为发货时间
                                    orderLogisticsDetail.SendingTime = csv.GetField<string>("发货时间") == "" ? orderLogisticsDetail.PickingTime.Value.Date : DateTime.Parse(csv.GetField<string>("发货时间"));
                                    var templ = targetorder.OrderLogistics.FirstOrDefault(o => o.LogisticsNo == orderLogisticsDetail.LogisticsNo);//是否已经存在该物流信息，防止重复添加
                                    if (templ == null)
                                    {
                                        if (!string.IsNullOrEmpty(orderLogisticsDetail.LogisticsNo))//物流单号为空，不保存此信息
                                        {
                                            //db.OrderLogisticsDetailSet.Add(orderLogisticsDetail);
                                            targetorder.OrderLogistics.Add(orderLogisticsDetail);
                                            if (orderLogisticsDetail.LogisticsProducts == null)
                                                orderLogisticsDetail.LogisticsProducts = new List<LogisticsProductInfo>();
                                            LogisticsProductInfo logisticsProductInfo = new LogisticsProductInfo()
                                            {
                                                ProductCount = quantity,
                                                ProductPlatId = orderDTO.productsku,
                                                ProductPlatName = orderDTO.productName,
                                                ProductWeight = weight,
                                                sku = orderDTO.productsku,
                                                Warehouse = orderDTO.Warehouse,
                                                weightCode = orderDTO.weightCode,
                                                weightCodeDesc = orderDTO.weightCodeDesc
                                            };
                                            orderLogisticsDetail.LogisticsProducts.Add(logisticsProductInfo);
                                            db.LogisticsProductInfos.Add(logisticsProductInfo);
                                        }

                                    }
                                    else
                                    {
                                        LogisticsProductInfo logisticsProductInfo = new LogisticsProductInfo()
                                        {
                                            ProductCount = quantity,
                                            ProductPlatId = orderDTO.productsku,
                                            ProductPlatName = orderDTO.productName,
                                            ProductWeight = weight,
                                            sku = orderDTO.productsku,
                                            Warehouse = orderDTO.Warehouse,
                                            weightCode = orderDTO.weightCode,
                                            weightCodeDesc = orderDTO.weightCodeDesc
                                        };
                                        templ.LogisticsProducts.Add(logisticsProductInfo);
                                        db.LogisticsProductInfos.Add(logisticsProductInfo);
                                    }

                                    db.SaveChanges();

                                }
                                return null;
                            }
                            
                        }
                    }
                    return null;


                }
                if (orderItem.OrderType == 4)//将周期购订单与原始订单建立联系
                {
                    using (var db = new OMSContext())
                    {
                        string sn = orderItem.SourceSn.Substring(0, orderItem.SourceSn.Length - 2);//通过截断字符串匹配原始订单号

                        var sourceorder = db.OrderSet.FirstOrDefault(o => o.SourceSn == sn.Trim() && o.OrderType == 0);
                        if (sourceorder != null)
                        {
                            var targetorder = db.OrderSet.FirstOrDefault(o => o.SourceSn == orderItem.SourceSn);//周期购单是否已经入库
                            if (targetorder == null)//入库周期购订单
                                InputOrderSubRecordInfo(csv, orderDTO, quantity, weight, orderItem, db, sourceorder);
                            else //更新物流信息
                            {

                                OrderLogisticsDetail orderLogisticsDetail = new OrderLogisticsDetail();
                                orderLogisticsDetail.OrderSn = targetorder.OrderSn;
                                orderLogisticsDetail.Logistics = csv.GetField<string>("物流公司");
                                orderLogisticsDetail.LogisticsNo = csv.GetField<string>("物流单号").Trim();
                                //  orderLogisticsDetail.LogisticsPrice = csv.GetField<string>("物流费用").ToDecimalOrNull();
                                orderLogisticsDetail.PickingTime = DateTime.Parse(csv.GetField<string>("配货时间"));
                                //发货时间为空时，从配货时间中取日期作为发货时间
                                orderLogisticsDetail.SendingTime = csv.GetField<string>("发货时间") == "" ? orderLogisticsDetail.PickingTime.Value.Date : DateTime.Parse(csv.GetField<string>("发货时间"));
                                var templ = targetorder.OrderLogistics.FirstOrDefault(o => o.LogisticsNo == orderLogisticsDetail.LogisticsNo && !string.IsNullOrEmpty(orderLogisticsDetail.LogisticsNo));//是否已经存在该物流信息，防止重复添加
                                if (templ == null)
                                {
                                    if (!string.IsNullOrEmpty(orderLogisticsDetail.LogisticsNo))//物流单号为空，不保存此信息
                                    {
                                        //db.OrderLogisticsDetailSet.Add(orderLogisticsDetail);
                                        targetorder.OrderLogistics.Add(orderLogisticsDetail);
                                        if (orderLogisticsDetail.LogisticsProducts == null)
                                            orderLogisticsDetail.LogisticsProducts = new List<LogisticsProductInfo>();
                                        LogisticsProductInfo logisticsProductInfo = new LogisticsProductInfo()
                                        {
                                            ProductCount = quantity,
                                            ProductPlatId = orderDTO.productsku,
                                            ProductPlatName = orderDTO.productName,
                                            ProductWeight = weight,
                                            sku = orderDTO.productsku,
                                            Warehouse = orderDTO.Warehouse,
                                            weightCode = orderDTO.weightCode,
                                            weightCodeDesc = orderDTO.weightCodeDesc
                                        };
                                        orderLogisticsDetail.LogisticsProducts.Add(logisticsProductInfo);
                                        db.LogisticsProductInfos.Add(logisticsProductInfo);
                                    }

                                }
                                else
                                {
                                    LogisticsProductInfo logisticsProductInfo = new LogisticsProductInfo()
                                    {
                                        ProductCount = quantity,
                                        ProductPlatId = orderDTO.productsku,
                                        ProductPlatName = orderDTO.productName,
                                        ProductWeight = weight,
                                        sku = orderDTO.productsku,
                                        Warehouse = orderDTO.Warehouse,
                                        weightCode = orderDTO.weightCode,
                                        weightCodeDesc = orderDTO.weightCodeDesc
                                    };
                                    templ.LogisticsProducts.Add(logisticsProductInfo);
                                    db.LogisticsProductInfos.Add(logisticsProductInfo);
                                }

                                db.SaveChanges();

                            }
                        }
                        else//通过收货人找到最近的一个销售订单
                        {
                            InputExceptionOrder(orderDTO, ExceptionType.OrderNoExistedFromSubOrder);
                            return null;
                            OrderEntity lastorder = null;
                            try
                            {
                                lastorder = db.OrderSet.Include(o => o.Consignee).Where(o => o.Consignee.Name == orderDTO.consigneeName && o.Consignee.Phone == orderDTO.consigneePhone && o.OrderType == 0).FirstOrDefault();
                            }
                            catch (Exception)
                            {
                                InputExceptionOrder(orderDTO, ExceptionType.OrderNoExistedFromSubOrder);
                                return null;
                            }

                            if (lastorder != null)
                            {
                                var targetorder = db.OrderSet.FirstOrDefault(o => o.SourceSn == orderItem.SourceSn);//售后单是否已经入库
                                if (targetorder == null)
                                    return InputOrderSubRecordInfo(csv, orderDTO, quantity, weight, orderItem, db, sourceorder);
                                else
                                {

                                    OrderLogisticsDetail orderLogisticsDetail = new OrderLogisticsDetail();
                                    orderLogisticsDetail.OrderSn = targetorder.OrderSn;
                                    orderLogisticsDetail.Logistics = csv.GetField<string>("物流公司");
                                    orderLogisticsDetail.LogisticsNo = csv.GetField<string>("物流单号").Trim();
                                    //  orderLogisticsDetail.LogisticsPrice = csv.GetField<string>("物流费用").ToDecimalOrNull();
                                    orderLogisticsDetail.PickingTime = DateTime.Parse(csv.GetField<string>("配货时间"));
                                    //发货时间为空时，从配货时间中取日期作为发货时间
                                    orderLogisticsDetail.SendingTime = csv.GetField<string>("发货时间") == "" ? orderLogisticsDetail.PickingTime.Value.Date : DateTime.Parse(csv.GetField<string>("发货时间"));
                                    var templ = targetorder.OrderLogistics.FirstOrDefault(o => o.LogisticsNo == orderLogisticsDetail.LogisticsNo);//是否已经存在该物流信息，防止重复添加
                                    if (templ == null)
                                    {
                                        if (!string.IsNullOrEmpty(orderLogisticsDetail.LogisticsNo))//物流单号为空，不保存此信息
                                        {
                                            //db.OrderLogisticsDetailSet.Add(orderLogisticsDetail);
                                            targetorder.OrderLogistics.Add(orderLogisticsDetail);
                                            if (orderLogisticsDetail.LogisticsProducts == null)
                                                orderLogisticsDetail.LogisticsProducts = new List<LogisticsProductInfo>();
                                            LogisticsProductInfo logisticsProductInfo = new LogisticsProductInfo()
                                            {
                                                ProductCount = quantity,
                                                ProductPlatId = orderDTO.productsku,
                                                ProductPlatName = orderDTO.productName,
                                                ProductWeight = weight,
                                                sku = orderDTO.productsku,
                                                Warehouse = orderDTO.Warehouse,
                                                weightCode = orderDTO.weightCode,
                                                weightCodeDesc = orderDTO.weightCodeDesc
                                            };
                                            orderLogisticsDetail.LogisticsProducts.Add(logisticsProductInfo);
                                            db.LogisticsProductInfos.Add(logisticsProductInfo);
                                        }

                                    }
                                    else
                                    {
                                        LogisticsProductInfo logisticsProductInfo = new LogisticsProductInfo()
                                        {
                                            ProductCount = quantity,
                                            ProductPlatId = orderDTO.productsku,
                                            ProductPlatName = orderDTO.productName,
                                            ProductWeight = weight,
                                            sku = orderDTO.productsku,
                                            Warehouse = orderDTO.Warehouse,
                                            weightCode = orderDTO.weightCode,
                                            weightCodeDesc = orderDTO.weightCodeDesc
                                        };
                                        templ.LogisticsProducts.Add(logisticsProductInfo);
                                        db.LogisticsProductInfos.Add(logisticsProductInfo);
                                    }

                                    db.SaveChanges();

                                }
                                return null;
                            }

                        }
                    }
                    return null;


                }
                else if (orderItem.OrderType == 0)
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

                        OrderLogisticsDetail orderLogisticsDetail = new OrderLogisticsDetail();
                        orderLogisticsDetail.OrderSn = orderItem.OrderSn;
                        orderLogisticsDetail.Logistics = csv.GetField<string>("物流公司");
                        orderLogisticsDetail.LogisticsNo = csv.GetField<string>("物流单号").Trim();
                      //  orderLogisticsDetail.LogisticsPrice = csv.GetField<string>("物流费用").ToDecimalOrNull();
                        orderLogisticsDetail.PickingTime = DateTime.Parse(csv.GetField<string>("配货时间"));
                        //发货时间为空时，从配货时间中取日期作为发货时间
                        orderLogisticsDetail.SendingTime = csv.GetField<string>("发货时间") == "" ? orderLogisticsDetail.PickingTime.Value.Date : DateTime.Parse(csv.GetField<string>("发货时间"));
                        if (orderItem.OrderLogistics == null)
                            orderItem.OrderLogistics = new List<OrderLogisticsDetail>();
                        if (!string.IsNullOrEmpty(orderLogisticsDetail.LogisticsNo))//物流单号为空，不保存此信息
                        {
                            //     db.OrderLogisticsDetailSet.Add(orderLogisticsDetail);
                            if (orderLogisticsDetail.LogisticsProducts == null)
                                orderLogisticsDetail.LogisticsProducts = new List<LogisticsProductInfo>();

                            orderItem.OrderLogistics.Add(orderLogisticsDetail);
                            LogisticsProductInfo logisticsProductInfo = new LogisticsProductInfo()
                            {
                                ProductCount = quantity,
                                ProductPlatId = orderDTO.productsku,
                                ProductPlatName = orderDTO.productName,
                                ProductWeight = weight,
                                sku = orderDTO.productsku,
                                Warehouse = orderDTO.Warehouse,
                                weightCode = orderDTO.weightCode,
                                weightCodeDesc = orderDTO.weightCodeDesc
                            };
                            orderLogisticsDetail.LogisticsProducts.Add(logisticsProductInfo);
                            // db.LogisticsProductInfos.Add(logisticsProductInfo);
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
                using (var db = new OMSContext())
                {
                    OrderLogisticsDetail orderLogisticsDetail = new OrderLogisticsDetail();
                    orderLogisticsDetail.OrderSn = item.OrderSn;
                    orderLogisticsDetail.Logistics = csv.GetField<string>("物流公司");
                    orderLogisticsDetail.LogisticsNo = csv.GetField<string>("物流单号").Trim();
                   // orderLogisticsDetail.LogisticsPrice = csv.GetField<string>("物流费用").ToDecimalOrNull();
                    orderLogisticsDetail.PickingTime = DateTime.Parse(csv.GetField<string>("配货时间"));
                    //发货时间为空时，从配货时间中取日期作为发货时间
                    orderLogisticsDetail.SendingTime = csv.GetField<string>("发货时间") == "" ? orderLogisticsDetail.PickingTime.Value.Date : DateTime.Parse(csv.GetField<string>("发货时间"));
                    var templ = item.OrderLogistics.FirstOrDefault(o => o.LogisticsNo == orderLogisticsDetail.LogisticsNo&&!string.IsNullOrEmpty(orderLogisticsDetail.LogisticsNo));//是否已经存在该物流信息，防止重复添加
                    if (templ == null)
                    {
                       
                        //db.OrderLogisticsDetailSet.Add(orderLogisticsDetail);
                        item.OrderLogistics.Add(orderLogisticsDetail);
                        if (orderLogisticsDetail.LogisticsProducts == null)
                            orderLogisticsDetail.LogisticsProducts = new List<LogisticsProductInfo>();
                        LogisticsProductInfo logisticsProductInfo = new LogisticsProductInfo()
                        {
                            ProductCount = quantity,
                            ProductPlatId = orderDTO.productsku,
                            ProductPlatName = orderDTO.productName,
                            ProductWeight = weight,
                            sku = orderDTO.productsku,
                            Warehouse = orderDTO.Warehouse,
                            weightCode = orderDTO.weightCode,
                            weightCodeDesc = orderDTO.weightCodeDesc
                        };
                        orderLogisticsDetail.LogisticsProducts.Add(logisticsProductInfo);

                    }
                    else if(!string.IsNullOrEmpty(orderLogisticsDetail.LogisticsNo))
                    {
                        LogisticsProductInfo logisticsProductInfo = new LogisticsProductInfo()
                        {
                            ProductCount = quantity,
                            ProductPlatId = orderDTO.productsku,
                            ProductPlatName = orderDTO.productName,
                            ProductWeight = weight,
                            sku = orderDTO.productsku,
                            Warehouse = orderDTO.Warehouse,
                            weightCode = orderDTO.weightCode,
                            weightCodeDesc = orderDTO.weightCodeDesc
                        };
                        templ.LogisticsProducts.Add(logisticsProductInfo);
                    }

                }
                return null;
            }

        }

        private OrderEntity InputOrderSubRecordInfo(CsvReader csv, OrderDTO orderDTO, int quantity, decimal weight, OrderEntity orderItem, OMSContext db, OrderEntity sourceorder)
        {
            if (sourceorder.OrderOptionRecords == null)
                sourceorder.OrderOptionRecords = new List<OrderOptionRecord>();
            var opt = new OrderOptionRecord()
            {
                SourceOrder = sourceorder,
                SubOrder = orderItem
            };
            sourceorder.OrderOptionRecords.Add(opt);
            db.OrderSet.Add(orderItem);

            //查找联系人
            if (!string.IsNullOrEmpty(orderItem.Consignee.Phone))
            {
                var s = db.CustomersSet.Include<CustomerEntity, ICollection<AddressEntity>>(c => c.Addresslist).FirstOrDefault(c => c.Name == orderItem.Consignee.Name && c.Phone == orderItem.Consignee.Phone);
                if (s != null)//通过姓名和手机号匹配是否是老用户
                {
                    orderItem.Consignee = s;
                    //收获地址取MD5值进行比对，不同则新增到收货人地址列表中
                    string md5 = Util.Helpers.Encrypt.Md5By32(orderItem.ConsigneeAddress.Address.Trim().Replace(" ", ""));
                    if (s.Addresslist.Any(a => a.MD5 == md5))
                    {
                        var addr = s.Addresslist.First(a => a.MD5 == md5);
                        orderItem.ConsigneeAddress = addr;//替换地址对象
                    }
                    else
                    {
                        orderItem.ConsigneeAddress.MD5 = md5;
                        s.Addresslist.Add(orderItem.ConsigneeAddress);
                    }
                }
                else//新用户
                {
                   
                    string md5 = Util.Helpers.Encrypt.Md5By32(orderItem.ConsigneeAddress.Address.Trim().Replace(" ", ""));
                    orderItem.ConsigneeAddress.MD5 = md5;
                    if (orderItem.Consignee.Addresslist == null)
                        orderItem.Consignee.Addresslist = new List<AddressEntity>();
                    orderItem.Consignee.Addresslist.Add(orderItem.ConsigneeAddress);
                    db.AddressSet.Add(orderItem.ConsigneeAddress);
                    db.CustomersSet.Add(orderItem.Consignee);
                }

            }
            else //异常订单
            {
                InputExceptionOrder(orderDTO, ExceptionType.PhoneNumOrPersonNameIsNull);
                return null;
            }
          
            OrderLogisticsDetail orderLogisticsDetail = new OrderLogisticsDetail();
            orderLogisticsDetail.OrderSn = orderItem.OrderSn;
            orderLogisticsDetail.Logistics = csv.GetField<string>("物流公司");
            orderLogisticsDetail.LogisticsNo = csv.GetField<string>("物流单号").Trim();
           // orderLogisticsDetail.LogisticsPrice = csv.GetField<string>("物流费用").ToDecimalOrNull();
            orderLogisticsDetail.PickingTime = DateTime.Parse(csv.GetField<string>("配货时间"));
            //发货时间为空时，从配货时间中取日期作为发货时间
            orderLogisticsDetail.SendingTime = csv.GetField<string>("发货时间") == "" ? orderLogisticsDetail.PickingTime.Value.Date : DateTime.Parse(csv.GetField<string>("发货时间"));
            if (orderItem.OrderLogistics == null)
                orderItem.OrderLogistics = new List<OrderLogisticsDetail>();
            if (!string.IsNullOrEmpty(orderLogisticsDetail.LogisticsNo))//物流单号为空，不保存此信息
            {
                //     db.OrderLogisticsDetailSet.Add(orderLogisticsDetail);
                if (orderLogisticsDetail.LogisticsProducts == null)
                    orderLogisticsDetail.LogisticsProducts = new List<LogisticsProductInfo>();

                orderItem.OrderLogistics.Add(orderLogisticsDetail);
                LogisticsProductInfo logisticsProductInfo = new LogisticsProductInfo()
                {
                    ProductCount = quantity,
                    ProductPlatId = orderDTO.productsku,
                    ProductPlatName = orderDTO.productName,
                    ProductWeight = weight,
                    sku = orderDTO.productsku,
                    Warehouse = orderDTO.Warehouse,
                    weightCode = orderDTO.weightCode,
                    weightCodeDesc = orderDTO.weightCodeDesc
                };
                orderLogisticsDetail.LogisticsProducts.Add(logisticsProductInfo);
                db.LogisticsProductInfos.Add(logisticsProductInfo);
            }
            db.SaveChanges();

            return null;
        }

        /// <summary>
        /// 插入商品记录
        /// </summary>
        /// <param name="db"></param>
        /// <param name="orderDTO"></param>
        /// <param name="item"></param>
        protected override bool InputProductInfoWithoutSaveChange(OMSContext db, OrderDTO orderDTO,OrderEntity item)
        {
            ProductDictionary pd = null;
            pd = db.ProductDictionarySet.FirstOrDefault(p => p.ProductCode.Trim() == orderDTO.productsku.Trim() && orderDTO.productsku != null && p.ProductCode != null);
            if (pd == null)
            {
                OnUIMessageEventHandle($"订单文件：{orderDTO.fileName}中平台单号：{orderDTO.sourceSN}（{orderDTO.productsku}）录入失败");
                InputExceptionOrder(orderDTO, ExceptionType.ProductCodeUnKnown);
                if (db.ProductDictionarySet.FirstOrDefault(p => p.ProductId == orderDTO.productsku) == null)
                {
                    ProductDictionary productDictionary = new ProductDictionary()
                    {
                        ProductCode = orderDTO.productsku,
                        ProductNameInPlatform = orderDTO.productName,
                        Source = orderDTO.source

                    };
                    db.ProductDictionarySet.Add(productDictionary);
                    db.SaveChanges();
                }
                return false;
            }
           


            // var foo = db.ProductsSet.Include(p => p.weightModel).FirstOrDefault(p => p.sku.Trim() == "S0010040003\t".Trim());
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
                    //  TotalAmount = totalAmount,
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
      
        public bool ExportExcel<T>(OptionType optionType, List<T> obj)
        {
           
            switch (optionType)
            {
                case OptionType.None:
                    break;
                case OptionType.ErpExcel:
                    OnUIMessageEventHandle("ERP导入单开始创建");
                    CreateErpExcel(obj as List<OrderEntity>);
                    break;
                case OptionType.LogisticsExcel:
                    OnUIMessageEventHandle("兴业银行回传单开始创建");
                    CreateLogisticsExcel(obj as List<OrderEntity>);
                    break;
                case OptionType.ExceptionExcel:
                    OnUIMessageEventHandle("错误信息单开始创建");
                    CreateExceptionExcel(obj as List<ExceptionOrder>);
                    break;
                default:
                    break;
            }
           
            return true;
        }
        protected void CreateErpExcel(List<OMS.Models.OrderEntity> obj)
        {
            DataTable dt = new DataTable();
            #region create columns


            dt.Columns.Add("店铺");
            dt.Columns.Add("订单编号");
            dt.Columns.Add("买家会员");
            dt.Columns.Add("支付金额");
            dt.Columns.Add("商品名称");
            dt.Columns.Add("商品代码");
            dt.Columns.Add("规格代码");
            dt.Columns.Add("规格名称");
            dt.Columns.Add("是否赠品");
            dt.Columns.Add("数量");
            dt.Columns.Add("价格");
            dt.Columns.Add("商品备注");
            dt.Columns.Add("运费");
            dt.Columns.Add("买家留言");
            dt.Columns.Add("收货人");
            dt.Columns.Add("联系电话");
            dt.Columns.Add("联系手机");
            dt.Columns.Add("收货地址");
            dt.Columns.Add("省");
            dt.Columns.Add("市");
            dt.Columns.Add("区");
            dt.Columns.Add("邮编");
            dt.Columns.Add("订单创建时间");
            dt.Columns.Add("订单付款时间");
            dt.Columns.Add("发货时间");
            dt.Columns.Add("物流单号");
            dt.Columns.Add("物流公司");
            dt.Columns.Add("卖家备注");
            dt.Columns.Add("发票种类");
            dt.Columns.Add("发票类型");
            dt.Columns.Add("发票抬头");
            dt.Columns.Add("纳税人识别号");
            dt.Columns.Add("开户行");
            dt.Columns.Add("账号");

            dt.Columns.Add("地址");
            dt.Columns.Add("电话");
            dt.Columns.Add("是否手机订单");
            dt.Columns.Add("是否货到付款");
            dt.Columns.Add("支付方式");
            dt.Columns.Add("支付交易号");
            dt.Columns.Add("真实姓名");
            dt.Columns.Add("身份证号");
            dt.Columns.Add("仓库名称");
            dt.Columns.Add("预计发货时间");
            dt.Columns.Add("预计送达时间");
            dt.Columns.Add("订单类型");
            dt.Columns.Add("是否分销商订单");
            #endregion

            using (var db = new OMSContext())
            {
                foreach (var item in obj)
                {
                    foreach (var productInfo in item.Products)
                    {
                        var dr = dt.NewRow();
                        dr["店铺"] = item.SourceDesc;
                        dr["订单编号"] = item.SourceSn;
                        dr["买家会员"] = item.Customer == null ? Util.Helpers.Encrypt.AesDecrypt(item.Consignee.Name) : Util.Helpers.Encrypt.AesDecrypt(item.Customer.Name);
                        dr["商品名称"] = productInfo.ProductPlatName;
                        dr["商品代码"] = productInfo.sku;
                        dr["规格代码"] = productInfo.weightCode == 0 ? string.Empty : productInfo.weightCode.ToString();
                        dr["数量"] = productInfo.ProductCount.ToString();
                        dr["价格"] = productInfo.TotalAmount.ToString();
                        dr["商品备注"] = item.Remarks;
                        dr["订单付款时间"] = item.CreatedDate.ToString("yyyy-MM-dd HH:mm:ss");
                        dr["收货人"] = Util.Helpers.Encrypt.AesDecrypt(item.Consignee.Name);
                        dr["联系手机"] = Util.Helpers.Encrypt.AesDecrypt(item.Consignee.Phone) ?? Util.Helpers.Encrypt.AesDecrypt(item.Consignee.Phone2);
                        dr["收货地址"] = Util.Helpers.Encrypt.AesDecrypt(item.ConsigneeAddress.Address);
                        dr["省"] = item.ConsigneeAddress.Province;
                        dr["市"] = item.ConsigneeAddress.City;
                        dr["区"] = item.ConsigneeAddress.County;
                        dr["邮编"] = item.ConsigneeAddress.ZipCode;
                        var foo = db.CustomStrategies.Include(c => c.Customer).FirstOrDefault(c => c.Customer.CustomerId == item.Consignee.CustomerId);
                        if (foo != null && Util.Helpers.Enum.Parse<CustomStrategyEnum>(foo.StrategyValue).HasFlag(CustomStrategyEnum.EmptyPackage))
                            dr["卖家备注"] = Util.Helpers.Enum.GetDescription<CustomStrategyEnum>(CustomStrategyEnum.EmptyPackage);
                        //dr["仓库名称"] = productInfo.Warehouse;
                        dt.Rows.Add(dr);

                    }

                }

            }
            var filename = System.IO.Path.Combine(clientConfig.ExcelOrderFolder, "import", $"ERP-{obj[0].Source}导入订单{DateTime.Now.ToString("yyyyMMddHHmmss")}.xlsx");
            NPOIExcel.Export(dt, filename);
            OnUIMessageEventHandle($"ERP-{obj[0].Source}导入订单生成成功。文件名:{filename}");
           
            
        }
        protected void CreateLogisticsExcel(List<OMS.Models.OrderEntity> lst)
        {
            var glst = lst.GroupBy(o => o.Source).ToList();
            DataTable cib_dt = new DataTable();
            cib_dt.Columns.Add("订单号");
            cib_dt.Columns.Add("物流编号");
            cib_dt.Columns.Add("物流单号");
            int taskcount = glst.Count;
            foreach (var item in glst)
            {
                var option = OrderOptSet.FirstOrDefault(i => i.clientConfig.Name == item.Key);
                if (option != null)
                {
                    System.Threading.ThreadPool.QueueUserWorkItem(o =>
                    {
                        var dt = option.ExportExcel(item.ToList());
                        if (option.clientConfig.Name == OrderSource.CIB || option.clientConfig.Name == OrderSource.CIBAPP 
                        || option.clientConfig.Name == OrderSource.CIBEVT
                        || option.clientConfig.Name == OrderSource.CIBSTM)
                        {
                            if (dt != null)
                            {
                                cib_dt.Merge(dt);
                            }
                        }
                        else
                        {
                            if (dt != null&&dt.Rows.Count>0)
                            {
                                string filename = Path.Combine(clientConfig.ExcelOrderFolder, "logistics", $"ERP-{item.Key}回传订单{DateTime.Now.ToString("yyyyMMddHHmmss")}.xlsx");
                                NPOIExcel.Export(dt, filename);
                                OnUIMessageEventHandle($"ERP-{item.Key}回传订单生成成功。文件名:{filename}");
                               
                            }
                        }
                        taskcount--;
                    });
                }
                else
                {
                    if(item.Key== OrderSource.CIBEVT||item.Key==OrderSource.CIBSTM)
                    {
                        System.Threading.ThreadPool.QueueUserWorkItem(o =>
                        {
                            var opt = OrderOptSet.FirstOrDefault(i => i.clientConfig.Name == OrderSource.CIBAPP);
                            var dt = opt.ExportExcel(item.ToList());
                           
                            if (dt != null)
                            {
                                cib_dt.Merge(dt);
                            }
                           
                            taskcount--;
                        });
                    }
                }
            }
            while (taskcount > 0)
            {
                System.Threading.Thread.Sleep(1000);
            }
            if (cib_dt.Rows.Count > 0)
            {
                string filename = Path.Combine(clientConfig.ExcelOrderFolder, "logistics", $"ERP-兴业银行积分回传订单{DateTime.Now.ToString("yyyyMMddHHmmss")}.xlsx");
                NPOIExcel.Export(cib_dt, filename);
                OnUIMessageEventHandle($"ERP-兴业银行积分回传订单生成成功。文件名:{filename}");
               
            }
        }
        protected void CreateExceptionExcel(List<OMS.Models.ExceptionOrder> lst)
        {
            
            DataTable dt = new DataTable();
            dt.Columns.Add("订单号");
            dt.Columns.Add("来源渠道");
            dt.Columns.Add("来源文件名");
            dt.Columns.Add("异常描述");
            dt.Columns.Add("异常编码");
           
           
            foreach (var item in lst)
            {
                var dr = dt.NewRow();
                dr["订单号"] = item.SourceSn;
                dr["来源渠道"] = item.Source;
                dr["来源文件名"] = item.OrderFileName;
                dr["异常描述"] = item.ErrorMessage;
                dr["异常编码"] = item.ErrorCode;
             
                dt.Rows.Add(dr);
            }
          
            if (dt.Rows.Count > 0)
            {
                string filename = Path.Combine(clientConfig.ExcelOrderFolder, "Error", $"ERP-异常信息采集{DateTime.Now.ToString("yyyyMMddHHmmss")}.xlsx");
                NPOIExcel.Export(dt, filename);
                OnUIMessageEventHandle($"ERP-异常信息采集。文件名:{filename}");

            }
        }
    }
}
