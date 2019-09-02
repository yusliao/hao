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
using DistrictService;
using OMS.Models;
using PushServer.Configuration;
using Util.Files;

namespace PushServer.Commands
{
    [Export(typeof(IOrderOption))]
    public class XunXiaoExcelOrderOption : OrderOptionBase
    {
        public override string Name => OMS.Models.OrderSource.XUNXIAO;
        private string NameDesc = Util.Helpers.Reflection.GetDescription<OrderSource>(OrderSource.XUNXIAO);


        public override IClientConfig clientConfig => AppServer.Instance.ConfigDictionary[Name];

       

        protected  List<DataFileInfo> GetExcelFiles()
        {
            var excelFileList = new List<DataFileInfo>();

           
            //FileScanner.ScanAllFiles(new DirectoryInfo(clientConfig.ExcelOrderFolder), "*.xlsx");
            //if (FileScanner.ScannedFiles.Any())
            //{
            //    FileScanner.ScannedFiles.ForEach(file =>
            //    {
            //        var dateStr = file.Name.Split('.').First().Split('_').Last().Substring(0, 8);
            //        var fileDate = DateTime.ParseExact(dateStr, "yyyyMMdd", CultureInfo.InvariantCulture);

            //        excelFileList.Add(new DataFileInfo(fileDate, file.Name, file.FullName));
            //    });
            //    var dt = string.IsNullOrEmpty(clientConfig.LastSyncDate) == true ? DateTime.Now : DateTime.Parse(clientConfig.LastSyncDate);
            //    if (!string.IsNullOrEmpty(clientConfig.LastSyncDate))
            //        excelFileList = excelFileList.Where(e => e.FileDate > dt).ToList();
            //}

            //excelFileList = excelFileList.OrderBy(f => f.FileDate).ToList();
           
            return excelFileList;
        }
       
      

        protected override List<OrderEntity> FetchOrders()
        {

            var ordersList = new List<OrderEntity>();

            foreach (var file in this.GetExcelFiles())
            {
                using (var excel = new NPOIExcel(file.FullName))
                {
                    var table = excel.ExcelToDataTable(null, 3);
                    if (table != null)
                        this.ResolveOrders(table, file.FullName, ordersList);
                    else
                    {
                        OnUIMessageEventHandle($"{NameDesc}导入文件：{file.FileName}解析完毕,当前订单数{ordersList.Count}");
                        continue;
                    }
                   
                }
             //   file.FileName = file.
            }

            return ordersList;
        }

       
        protected  List<OrderEntity> ResolveOrders(DataTable excelTable,string file, List<OrderEntity> items)
        {
            

            foreach (DataRow row in excelTable.Rows)
            {
                if (row["商品名称"] == DBNull.Value||row["商品编码"]==DBNull.Value)
                    continue;

                var source = Name;
                var sourceSN = Convert.ToString(row["订单编号"]);

                var sOrderDate = sourceSN.Substring(0, 8);
                var currentYear = DateTime.Now.Year.ToString();
                if (sOrderDate.StartsWith("90"))
                    sOrderDate = sOrderDate.Remove(0, 2).Insert(0, currentYear.Substring(0, 2));
                var createdDate = DateTime.ParseExact(sOrderDate, "yyyyMMdd", CultureInfo.InvariantCulture);
                var orderSN = string.Format("{0}-{1}", source, sourceSN); //订单SN=来源+原来的SN


                var item = items.Find(o => o.OrderSn == orderSN);
                using (var db = new OMSContext())
                {
                    var foo = db.OrderSet.Find(orderSN);
                    if (foo != null)
                    {
                        Util.Logs.Log.GetLog(nameof(TMExcelOrderOption)).Error($"订单{foo.OrderSn}已经存在");
                        continue;

                    }
                }
                if (item == null)
                {
                    var sourceAccount = string.Empty;

                    var productName = Convert.ToString(row["商品名称"]);
                    var productsku = Convert.ToString(row["商品编码"]);
                    var quantity = Convert.ToInt32(row["数量"]);
                    var itemPriceStr = Convert.ToString(row["商品价格（广发售价）"]).Replace("￥", "").Trim();
                    var itemPrice = Convert.ToDecimal(itemPriceStr);

                    var totalAmount = itemPrice * quantity;

                    var consigneeName = Convert.ToString(row["收货人"]);
                    var consigneePhone = Convert.ToString(row["收货人电话1"]);
                    
                    var consigneePhone2 = Convert.ToString(row["收货人电话2"]);

                   
                    var consigneeProvince = string.Empty;
                    var consigneeCity = string.Empty;
                    var consigneeCounty = string.Empty;
                    var consigneeAddress = Convert.ToString(row["收货地址"]);
                    var consigneeZipCode = Convert.ToString(row["收货人邮编"]);


                    var addrInfo = DistrictService.DistrictService.ResolveAddress(consigneeAddress);
                    consigneeProvince = addrInfo.Province;
                    consigneeCity = addrInfo.City;
                    consigneeCounty = addrInfo.County;
                 //   consigneeAddress = addrInfo.Address;


                    //NOTE: #此处设置为现金支付金额#
                    var invoiceType = string.Empty;
                    var invoiceName = Convert.ToString(row["发票抬头"]);
                    var cashAmountStr = Convert.ToString(row["现金支付金额"]).Replace("￥", "").Trim();
                    var cashAmount = Convert.ToDecimal(cashAmountStr);
                    if (!string.IsNullOrEmpty(invoiceName))
                        invoiceType = string.Format("{0:F2}", cashAmount);

                    var userRemarks = Convert.ToString(row["用户留言"]);
                    var serviceRemarks = Convert.ToString(row["客服留言"]);
                    var deliverRemarks = Convert.ToString(row["送货时间要求"]);

                    var remarks = new StringBuilder();
                    if (!string.IsNullOrEmpty(userRemarks))
                        remarks.AppendLine(string.Format("[用户留言] {0}", userRemarks));
                    if (!string.IsNullOrEmpty(serviceRemarks))
                        remarks.AppendLine(string.Format("[客服留言] {0}", serviceRemarks));
                    if (!string.IsNullOrEmpty(deliverRemarks))
                        remarks.AppendLine(string.Format("[送货时间要求] {0}", deliverRemarks));
                    var orderItem = new OrderEntity()
                    {
                        OrderSn = orderSN,
                        Source = OrderSource.CGB,
                        SourceDesc = Util.Helpers.Reflection.GetDescription<OrderSource>(OrderSource.CGB),
                        SourceSn = sourceSN,
                        CreatedDate = createdDate,
                        Consignee = new CustomerEntity()
                        {
                            Name = consigneeName,
                            Phone = consigneePhone,
                            Phone2 = consigneePhone2 == consigneePhone ? null : consigneePhone2,
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
                        OrderDateInfo = new OrderDateInfo()
                        {
                            CreateTime = createdDate,
                            MonthNum = createdDate.Month,
                            WeekNum = Util.Helpers.Time.GetWeekNum(createdDate),
                            SeasonNum = Util.Helpers.Time.GetSeasonNum(createdDate),
                            Year = createdDate.Year,
                            DayNum = createdDate.DayOfYear,
                            TimeStamp = Util.Helpers.Time.GetUnixTimestamp(createdDate)
                        },
                     

                        OrderStatus = (int)OrderStatus.Confirmed,
                        OrderStatusDesc = Util.Helpers.Enum.GetDescription(typeof(OrderStatus), OrderStatus.Confirmed),

                        Remarks = remarks.ToString()
                    };
                    if (orderItem.Products == null)
                        orderItem.Products = new List<OrderProductInfo>();
                    using (var db = new OMSContext())
                    {
                       // 查找联系人
                        if (!string.IsNullOrEmpty(orderItem.Consignee.Phone))
                        {
                            string md5 = Util.Helpers.Encrypt.Md5By32(orderItem.ConsigneeAddress.Address.Trim().Replace(" ", ""));
                            var s = db.CustomersSet.Include<CustomerEntity, ICollection<AddressEntity>>(c => c.Addresslist).FirstOrDefault(c => c.Name == orderItem.Consignee.Name && c.Phone == orderItem.Consignee.Phone);
                            if (s != null)
                            {

                                orderItem.Consignee = s;
                                orderItem.OrderExtendInfo = new OrderExtendInfo() { IsReturningCustomer = true };
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
                        //查找商品字典
                        var bar = db.ProductDictionarySet.FirstOrDefault(p => p.ProductId == productsku);
                        if (bar == null || string.IsNullOrEmpty(bar.ProductCode))
                        {
                            bar = db.ProductDictionarySet.FirstOrDefault(p => p.ProductNameInPlatform == productName);
                            if (bar == null || string.IsNullOrEmpty(bar.ProductCode))
                            {
                                Util.Logs.Log.GetLog(nameof(TMExcelOrderOption)).Error($"订单文件：{file}中平台商品：{productName}未找到");
                              //  Util.Logs.Log.GetLog(nameof(CIBVIPCSVOrderOption)).Debug($"订单文件：{file}中平台商品：{productName}未找到.order:{Util.Helpers.Json.ToJson(orderItem)}");
                                continue;
                            }
                           
                        }
                        if (string.IsNullOrEmpty(bar.ProductNameInPlatform))
                        {
                            bar.ProductNameInPlatform = productName;
                            db.SaveChanges();
                        }
                        var foo = db.ProductsSet.Include(p => p.weightModel).FirstOrDefault(p => p.sku == bar.ProductCode);
                        if (foo == null)
                        {
                            Util.Logs.Log.GetLog(nameof(TMExcelOrderOption)).Error($"订单文件：{file}中平台商品名称：{productName}对应系统商品未找到");
                            Util.Logs.Log.GetLog(nameof(TMExcelOrderOption)).Debug($"订单文件：{file}中平台商品名称：{productName}对应系统商品未找到.order:{Util.Helpers.Json.ToJson(orderItem)}");
                            continue;
                        }

                        decimal weight = foo == null ? 0 : foo.QuantityPerUnit * quantity;
                        OrderProductInfo orderProductInfo = new OrderProductInfo()
                        {
                            ProductPlatId = productsku,
                            ProductPlatName = productName,
                         //   Warehouse = orderItem.OrderLogistics.Logistics,
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
                        db.OrderRepurchases.Add(orderItem.OrderRepurchase);
                        db.OrderDateInfos.Add(orderItem.OrderDateInfo);
                       
                      
                        db.SaveChanges();
                    }
                   
                }
                else
                {
                    var productName = Convert.ToString(row["商品名称"]);
                    var productsku = Convert.ToString(row["商品编码"]);
                    var quantity = Convert.ToInt32(row["数量"]);
                    var itemPriceStr = Convert.ToString(row["商品价格（广发售价）"]).Replace("￥", "").Trim();
                    var itemPrice = Convert.ToDecimal(itemPriceStr);

                    var totalAmount = itemPrice * quantity;

                    using (var db = new OMSContext())
                    {
                        var bar = db.ProductDictionarySet.FirstOrDefault(p => p.ProductId == productsku);
                        if (bar == null || string.IsNullOrEmpty(bar.ProductCode))
                        {
                            bar = db.ProductDictionarySet.FirstOrDefault(p => p.ProductNameInPlatform == productName);
                            if (bar == null || string.IsNullOrEmpty(bar.ProductCode))
                            {
                                Util.Logs.Log.GetLog(nameof(TMExcelOrderOption)).Error($"订单文件：{file}中平台商品：{productName}未找到");
                                Util.Logs.Log.GetLog(nameof(TMExcelOrderOption)).Debug($"订单文件：{file}中平台商品：{productName}未找到.order:{Util.Helpers.Json.ToJson(item)}");
                                continue;
                            }
                        }
                        if (string.IsNullOrEmpty(bar.ProductNameInPlatform))
                        {
                            bar.ProductNameInPlatform = productName;
                            db.SaveChanges();
                        }
                        var foo = db.ProductsSet.Include(p => p.weightModel).FirstOrDefault(p => p.sku == bar.ProductCode);
                        if (foo == null)
                        {
                            Util.Logs.Log.GetLog(nameof(TMExcelOrderOption)).Error($"订单文件：{file}中平台商品名称：{productName}对应系统商品未找到");
                            Util.Logs.Log.GetLog(nameof(TMExcelOrderOption)).Debug($"订单文件：{file}中平台商品名称：{productName}对应系统商品未找到.order:{Util.Helpers.Json.ToJson(item)}");
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
                            OrderSn = item.OrderSn,
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
                                dr["物流公司编码"] = db.logisticsInfoSet.FirstOrDefault(l => l.FullName == logisticsDetail.Logistics).BankLogisticsCode;
                                dr["物流公司"] = logisticsDetail.Logistics;
                                dt.Rows.Add(dr);
                            }
                        }

                       
                    }
                }
            }
            return dt;
            
        }
    }
}
