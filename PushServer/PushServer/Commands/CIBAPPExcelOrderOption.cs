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
using PushServer.Configuration;

namespace PushServer.Commands
{
    /// <summary>
    /// 兴业银行积分
    /// </summary>
    [Export(typeof(IOrderOption))]
    class CIBAPPExcelOrderOption : OrderOptionBase
    {
        public override string Name => OMS.Models.OrderSource.CIBAPP;

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
                        var dr = dt.NewRow();
                        dr["订单号"] = item.SourceSn;
                        dr["物流单号"] = item.OrderLogistics.LogisticsNo;
                      
                        dr["物流编号"] = db.logisticsInfoSet.FirstOrDefault(l => l.FullName == item.OrderLogistics.Logistics)?.BankLogisticsCode;
                       
                        dt.Rows.Add(dr);
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
                    this.ResolveOrders(table,file.FullName, ordersList);
                   
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
           

            var orderStatus = OrderStatus.Confirmed;

            var sourceStatus = excelTable.Columns[0].ColumnName.Split(',').First().Split('：').Last();
            switch (sourceStatus)
            {
                case "待发货":
                    orderStatus = OrderStatus.Confirmed;
                    break;
                case "待收货":
                    orderStatus = OrderStatus.Delivered;
                    break;
                case "待评价":
                case "已评价":
                    orderStatus = OrderStatus.Finished;
                    break;
            }

            for (int i = 1; i < excelTable.Rows.Count; i++)
            {
                var row = excelTable.Rows[i];

                var orderDateStr = Convert.ToString(row[0]); //订单创建时间
                var createdDate = DateTime.Parse(orderDateStr);

                var source = OrderSource.CIBAPP;
                var sourceDesc = Util.Helpers.Reflection.GetDescription<OrderSource>(OrderSource.CIBAPP);
                var sourceAccount = string.Empty;
                var sourceSN = Convert.ToString(row[1]); //订单号
                if (string.IsNullOrEmpty(sourceSN))
                    continue;

                //Added By: BingYi 20180728
                //CIBAPP目前包含两种支付方式: 本金支付/积分支付 
                //如果为 本金支付,该订单 应该改属于 [兴业分期商城]
               // var paymentType = Convert.ToString(row["支付方式"]).Trim();
                //if (paymentType.Equals("本金支付") || paymentType.Equals("分期支付"))
                //{
                //    source = OrderSource.CIBSTM;
                //    sourceDesc = Util.Helpers.Reflection.GetDescription<OrderSource>(OrderSource.CIBSTM);
                //}

                //订单SN=来源+原来的SN
                var orderSN = string.Format("{0}-{1}", source, sourceSN);
                using (var db = new OMSContext())
                {
                    var foo = db.OrderSet.Find(orderSN);
                    if (foo != null)
                    {
                        Util.Logs.Log.GetLog(nameof(CIBAPPExcelOrderOption)).Error($"订单{foo.OrderSn}已经存在");
                        continue;

                    }
                }
                var item = items.Find(o => o.OrderSn == orderSN);
                if (item == null)
                {
                    decimal unitPrice = 0M;
                    var productName = Convert.ToString(row[3]); //商品名称
                    var productsku = Convert.ToString(row[2]); //商品编号
                    if (productName.Equals("水清清特供稻花香精装礼盒2.5KG"))
                        unitPrice = 56.00M;
                    else if (productName.Equals("水清清冠军优选四季经典4KG*1提"))
                        unitPrice = 108.00M;

                    var quantity = Convert.ToInt32(row[4]); //数量
                    var productProps = Convert.ToString(row[5]); //商品属性
                  

                    var consigneeName = Convert.ToString(row[6]); //收件人
                    var consigneePhone = Convert.ToString(row[7]); //联系电话
                    var consigneePhone2 = string.Empty;

                   
                    var consigneeProvince = string.Empty;
                    var consigneeCity = string.Empty;
                    var consigneeCounty = string.Empty;
                    var consigneeAddress = Convert.ToString(row[8]); //收货地区+详细地址
                    var consigneeZipCode = Convert.ToString(row[9]); //邮编

                    //
                    if (string.IsNullOrEmpty(consigneeProvince)
                        && string.IsNullOrEmpty(consigneeCity) && !string.IsNullOrEmpty(consigneeAddress))
                    {
                        
                        var addrInfo = DistrictService.DistrictService.ResolveAddress(consigneeAddress);
                        consigneeProvince = addrInfo.Province;
                        consigneeCity = addrInfo.City;
                        consigneeCounty = addrInfo.County;
                     //   consigneeAddress = addrInfo.Address;
                    }

                    var totalAmount = 0;//?
                    var totalQuantity = quantity;
                    var totalPayment = 0;

                    //是否需要发票
                    var invoiceFlag = Convert.ToString(row["是否需要发票"]); //是否需要发票
                    var invoiceType = string.Empty;
                    var invoiceName = Convert.ToString(row["发票抬头"]); //发票抬头
                    if (invoiceFlag.Equals("否"))
                        invoiceType = invoiceName = string.Empty;

                    var orderItem = new OrderEntity()
                    {
                        SourceSn = sourceSN,
                        Source = source,
                        SourceDesc = sourceDesc,
                        CreatedDate = createdDate,
                        OrderSn = orderSN,
                        Consignee = new CustomerEntity()
                        {
                            Name = consigneeName,
                            Phone = consigneePhone,
                            Phone2 = consigneePhone2,
                            CreateDate = createdDate
                        },
                        ConsigneeAddress = new AddressEntity()
                        {
                            Address = consigneeAddress,
                            City = consigneeCity,
                            County = consigneeCounty,
                            Province = consigneeProvince,
                            ZipCode = consigneeZipCode
                        },
                        OrderLogistics = new OrderLogisticsDetail()
                        {
                            Logistics = consigneeProvince == "新疆维吾尔自治区" ? "顺丰标准快递" : "中通快递"
                        },
                        OrderDateInfo = new OrderDateInfo()
                        {
                            CreateTime = createdDate,
                            MonthNum = createdDate.Month,
                            WeekNum = Util.Helpers.Time.GetWeekNum(createdDate),
                            SeasonNum = Util.Helpers.Time.GetSeasonNum(createdDate),
                            Year = createdDate.Year,
                            TimeStamp = Util.Helpers.Time.GetUnixTimestamp(createdDate)
                        },
                        OrderStatus = (int)orderStatus,
                        OrderStatusDesc = Util.Helpers.Enum.GetDescription(typeof(OrderStatus), orderStatus),


                        Remarks = string.Empty
                    };
                    if (orderItem.Products == null)
                        orderItem.Products = new List<OrderProductInfo>();
                    using (var db = new OMSContext())
                    {
                        //查找联系人
                        if (!string.IsNullOrEmpty(orderItem.Consignee.Phone))
                        {
                            string md5 = Util.Helpers.Encrypt.Md5By32(orderItem.ConsigneeAddress.Address.Trim().Replace(" ", ""));
                            var s = db.CustomersSet.Include<CustomerEntity, ICollection<AddressEntity>>(c => c.Addresslist).FirstOrDefault(c => c.Name == orderItem.Consignee.Name && c.Phone == orderItem.Consignee.Phone);
                            if (s != null)
                            {

                                orderItem.Consignee = s;
                                
                                DateTime startSeasonTime, endSeasonTime, startYearTime, endYearTime, startWeekTime, endWeekTime;
                                Util.Helpers.Time.GetTimeBySeason(orderItem.CreatedDate.Year, Util.Helpers.Time.GetSeasonNum(orderItem.CreatedDate), out startSeasonTime, out endSeasonTime);
                                Util.Helpers.Time.GetTimeByYear(orderItem.CreatedDate.Year, out startYearTime, out endYearTime);
                                Util.Helpers.Time.GetTimeByWeek(orderItem.CreatedDate.Year, Util.Helpers.Time.GetWeekNum(orderItem.CreatedDate), out startWeekTime, out endWeekTime);

                                orderItem.OrderRepurchase = new OrderRepurchase()
                                {
                                    DailyRepurchase = true,
                                    MonthRepurchase = s.CreateDate.Value.Date < new DateTime(orderItem.CreatedDate.Year, orderItem.CreatedDate.Month, 1).Date ? true : false,
                                    SeasonRepurchase = s.CreateDate.Value.Date < startSeasonTime.Date ? true : false,
                                    WeekRepurchase = s.CreateDate.Value.Date < startWeekTime.Date ? true : false,
                                    YearRepurchase = s.CreateDate.Value.Date < startYearTime.Date ? true : false,

                                };
                              
                                //更新收件人与地址的关系

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
                            else//没找到备案的收货人
                            {
                              
                                orderItem.OrderRepurchase = new OrderRepurchase();
                              
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
                            ExceptionOrder exceptionOrder = new ExceptionOrder()
                            {
                                OrderFileName = file,
                                OrderInfo = Util.Helpers.Json.ToJson(orderItem),
                                Source = this.Name
                            };
                            db.ExceptionOrders.Add(exceptionOrder);
                            db.SaveChanges();
                            continue;
                        }

                        var bar = db.ProductDictionarySet.FirstOrDefault(p => p.ProductNameInPlatform == productName);
                        if (bar == null || string.IsNullOrEmpty(bar.ProductCode))
                        {
                            Util.Logs.Log.GetLog(nameof(CIBAPPExcelOrderOption)).Error($"订单文件：{file}中平台商品：{productName}未找到");
                           // Util.Logs.Log.GetLog(nameof(CIBAPPExcelOrderOption)).Debug($"订单文件：{file}中平台商品：{productName}未找到.order:{Util.Helpers.Json.ToJson(orderItem)}");
                          
                            if (bar == null)
                            {
                                ProductDictionary productDictionary = new ProductDictionary()
                                {
                                    ProductId = productsku,
                                    ProductNameInPlatform = productName
                                };
                                db.ProductDictionarySet.Add(productDictionary);
                                db.SaveChanges();
                            }
                            continue;
                        }
                        var foo = db.ProductsSet.Include(p => p.weightModel).FirstOrDefault(p => p.sku == bar.ProductCode);
                        if (foo == null)
                        {
                            Util.Logs.Log.GetLog(nameof(CIBAPPExcelOrderOption)).Error($"订单文件：{file}中平台商品名称：{productName}对应系统商品未找到");
                           // Util.Logs.Log.GetLog(nameof(CIBAPPExcelOrderOption)).Debug($"订单文件：{file}中平台商品名称：{productName}对应系统商品未找到.order:{Util.Helpers.Json.ToJson(orderItem)}");
                            continue;
                        }

                        decimal weight = foo==null?0:foo.QuantityPerUnit * quantity;
                        OrderProductInfo orderProductInfo = new OrderProductInfo()
                        {
                            ProductPlatId = productsku,
                            ProductPlatName = productName,
                        //    Warehouse = orderItem.OrderLogistics.Logistics,
                            MonthNum = createdDate.Month,
                            weightCode = foo.weightModel == null ? 0 : foo.weightModel.Code,
                            weightCodeDesc = foo.weightModel == null ? string.Empty : $"{foo.weightModel.Value}g",
                            OrderSn = orderItem.OrderSn,
                            TotalAmount = totalAmount,
                            ProductCount = quantity,
                            ProductWeight = weight,
                            Source = source,
                            sku = foo.sku
                        };
                        orderItem.Products.Add(orderProductInfo);
                       
                        items.Add(orderItem);
                        if (orderItem.OrderRepurchase == null)
                            orderItem.OrderRepurchase = new OrderRepurchase();
                        db.OrderRepurchases.Add(orderItem.OrderRepurchase);
                        db.OrderDateInfos.Add(orderItem.OrderDateInfo);
                        db.OrderLogisticsDetailSet.Add(orderItem.OrderLogistics);
                        db.OrderProductSet.Add(orderProductInfo);
                        db.SaveChanges();
                    }
                    
                }
                else
                {
                    var productName = Convert.ToString(row[3]); //商品名称
                    var productsku = Convert.ToString(row[2]); //商品编号
                   

                    var quantity = Convert.ToInt32(row[4]); //数量

                    var totalAmount = 0 * quantity;

                    using (var db = new OMSContext())
                    {
                        var bar = db.ProductDictionarySet.FirstOrDefault(p => p.ProductNameInPlatform == productName);
                        if (bar == null || string.IsNullOrEmpty(bar.ProductCode))
                        {
                            Util.Logs.Log.GetLog(nameof(CIBAPPExcelOrderOption)).Error($"订单文件：{file}中平台商品：{productName}未找到");
                           // Util.Logs.Log.GetLog(nameof(CIBAPPExcelOrderOption)).Debug($"订单文件：{file}中平台商品：{productName}未找到.order:{Util.Helpers.Json.ToJson(item)}");
                          
                            if (bar == null)
                            {
                                ProductDictionary productDictionary = new ProductDictionary()
                                {
                                    ProductId = productsku,
                                    ProductNameInPlatform = productName
                                };
                                db.ProductDictionarySet.Add(productDictionary);
                                db.SaveChanges();
                            }
                            continue;
                        }
                        var foo = db.ProductsSet.Include(p => p.weightModel).FirstOrDefault(p => p.sku == bar.ProductCode);
                        if (foo == null)
                        {
                            Util.Logs.Log.GetLog(nameof(CIBAPPExcelOrderOption)).Error($"订单文件：{file}中平台商品名称：{productName}对应系统商品未找到");
                          //  Util.Logs.Log.GetLog(nameof(CIBAPPExcelOrderOption)).Debug($"订单文件：{file}中平台商品名称：{productName}对应系统商品未找到.order:{Util.Helpers.Json.ToJson(item)}");
                            continue;
                        }

                        decimal weight = foo == null ? 0 : foo.QuantityPerUnit * quantity;
                        OrderProductInfo orderProductInfo = new OrderProductInfo()
                        {
                            ProductPlatId = productsku,
                            ProductPlatName = productName,
                         //   Warehouse = item.OrderLogistics.Logistics,
                            MonthNum = createdDate.Month,
                            weightCode = foo.weightModel == null ? 0 : foo.weightModel.Code,
                            weightCodeDesc = foo.weightModel == null ? string.Empty : $"{foo.weightModel.Value}g",
                            OrderSn= item.OrderSn,
                            TotalAmount = totalAmount,
                            ProductCount = quantity,
                            ProductWeight = weight,
                            Source = source,
                            sku = foo.sku
                        };
                        if (item.Products.FirstOrDefault(p => p.sku == foo.sku) == null)
                        {
                            item.Products.Add(orderProductInfo);
                            db.OrderProductSet.Add(orderProductInfo);
                            db.SaveChanges();
                        }
                    }

                    
                }
            }

            return items;
        }
    }
}
