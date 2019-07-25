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
    class CIBExcelOrderOption : OrderOptionBase
    {
        public override string Name => OMS.Models.OrderSource.CIB;

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

                        dr["物流编号"] = db.logisticsInfoSet.FirstOrDefault(l => l.FullName == item.OrderLogistics.Logistics).BankLogisticsCode;

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
                    var table = excel.ExcelToDataTable(null, true);
                    if (table != null)
                        this.ResolveOrders(table, file.FullName, ordersList);
                    else
                    {
                        OnUIMessageEventHandle($"兴业积分PC导入文件：{file.FileName}解析完毕,当前订单数{ordersList.Count}");
                        continue;
                    }
                }
                OnUIMessageEventHandle($"兴业积分PC导入文件：{file.FileName}解析完毕,当前订单数{ordersList.Count}");
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
                    var dateStr = file.Name.Split('.').First().Split('-').Last().Substring(0, 8);
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


            foreach (DataRow row in excelTable.Rows)
            {
                if (row["礼品名称"] == DBNull.Value
                   || row["兑换礼品数量"] == DBNull.Value
                   || row["领取人姓名"] == DBNull.Value)
                    continue;

                var source = OrderSource.CIB;
                var sourceDesc = Util.Helpers.Reflection.GetDescription<OrderSource>(OrderSource.CIB);
                var sourceSN = $"pc{DateTime.Now.ToString("yyyyMMddHHmmssfff")}";
                var orderSN = string.Format("{0}-{1}", source, sourceSN); //订单SN=来源+原来的SN

                var sOrderDate = Convert.ToString(row["兑换登记日期"]);
                var createdDate = DateTime.Parse(sOrderDate.Insert(4, "-").Insert(7, "-"));

                var sourceAccount = string.Empty;
                if (excelTable.Columns.Contains("持卡人证件号码"))
                    sourceAccount = Convert.ToString(row["持卡人证件号码"]);

                var productName = Convert.ToString(row["礼品名称"]);
                var productsku = Convert.ToString(row["礼品编号"]);
                var quantity = Convert.ToInt32(row["兑换礼品数量"]);

                var customerName = string.Empty;
                var customerPhone = string.Empty;
                var customerPhone2 = string.Empty;

                var consigneeName = Convert.ToString(row["领取人姓名"]);
                var consigneePhone = string.Empty;
                var consigneePhone2 = string.Empty;

                var consigneeProvince = string.Empty;
                var consigneeCity = string.Empty;
                var consigneeCounty = string.Empty;
                var consigneeAddress = Convert.ToString(row["递送地址"]);

                var consigneeAddressUnit = string.Empty;
                if (excelTable.Columns.Contains("单位"))
                    consigneeAddressUnit = Convert.ToString(row["单位"]);

                //特殊处理：

                //合并详细地址和单位地址，并且将单位地址设置为空（解决地址被分开，识别度降低问题）

                if (!string.IsNullOrEmpty(consigneeAddressUnit))
                    consigneeAddress = string.Format("{0}{1}", consigneeAddress, consigneeAddressUnit);

                var consigneeZipCode = Convert.ToString(row["递送地址邮编"]);

                if (excelTable.Columns.Contains("持卡人姓名"))
                    customerName = Convert.ToString(row["持卡人姓名"]);
                else
                    customerName = Convert.ToString(row["领取人姓名"]);

                //if (excelTable.Columns.Contains("领取人联系电话"))
                //    consigneePhone = Convert.ToString(row["领取人联系电话"]);
                //else if (excelTable.Columns.Contains("领取人联系手机"))
                //    consigneePhone = Convert.ToString(row["领取人联系手机"]);

                if (excelTable.Columns.Contains("分机"))
                    consigneePhone2 = Convert.ToString(row["分机"]);

                if (excelTable.Columns.Contains("手机号码"))
                    customerPhone=consigneePhone = Convert.ToString(row["手机号码"]);
                else
                    customerPhone = consigneePhone;//NOTE: no necessery!

                if (excelTable.Columns.Contains("领取人所在省份"))
                    consigneeProvince = Convert.ToString(row["领取人所在省份"]);

                if (excelTable.Columns.Contains("领取人所在城市"))
                    consigneeCity = Convert.ToString(row["领取人所在城市"]);

                if (excelTable.Columns.Contains("领取人所在县区"))
                    consigneeCounty = Convert.ToString(row["领取人所在县区"]);

                //
                if (string.IsNullOrEmpty(consigneeProvince)
                    && string.IsNullOrEmpty(consigneeCity) && !string.IsNullOrEmpty(consigneeAddress))
                {
                    var addrInfo = DistrictService.DistrictService.ResolveAddress(consigneeAddress);
                    consigneeProvince = addrInfo.Province;
                    consigneeCity = addrInfo.City;
                    consigneeCounty = addrInfo.County;
                  //  consigneeAddress = addrInfo.Address;
                }

                consigneeName = consigneeName.Split(' ').First();
                customerName = customerName.Split(' ').First();

                //修正处理：
                //当持卡人和领取人一致的时候，领取人的联系号码比较乱（银行的输入/客户填写问题）
                //我们使用持卡人的号码修正领取人的联系号码，以确保领取人的联系号码更有效！
                //if (customerName.Equals(consigneeName) && string.IsNullOrEmpty(consigneePhone))
                //    consigneePhone = customerPhone;
                //订单SN=来源+原来的SN
               
                using (var db = new OMSContext())
                {
                    var foo = db.OrderSet.Find(orderSN);
                    if (foo != null)
                    {
                        Util.Logs.Log.GetLog(nameof(CIBExcelOrderOption)).Error($"订单{foo.OrderSn}已经存在");
                        continue;

                    }
                }
                var item = items.Find(o => o.OrderSn == orderSN);
                if (item == null)
                {
                    var orderItem = new OrderEntity()
                    {
                        SourceSn = sourceSN,
                        Source = source,
                        SourceDesc = sourceDesc,
                        CreatedDate = createdDate,
                        OrderSn = orderSN,
                        Customer = new CustomerEntity()
                        {
                            Name = customerName,
                            Phone = customerPhone,
                            Phone2 = customerPhone2,
                            CreateDate = createdDate
                        },
                        Consignee = new CustomerEntity()
                        {
                            Name = consigneeName,
                            Phone = consigneePhone,
                            Phone2 = consigneePhone2
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
                            TimeStamp = Util.Helpers.Time.GetUnixTimestamp(createdDate)
                        },
                        OrderLogistics = new OrderLogisticsDetail()
                        {
                            Logistics = consigneeProvince== "新疆维吾尔自治区"? "顺丰速运": "中通速递"
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
                                orderItem.OrderExtendInfo = new OrderExtendInfo() { IsReturningCustomer = true };
                                DateTime startSeasonTime, endSeasonTime, startYearTime, endYearTime, startWeekTime, endWeekTime;
                                Util.Helpers.Time.GetTimeBySeason(orderItem.CreatedDate.Year, Util.Helpers.Time.GetSeasonNum(orderItem.CreatedDate), out startSeasonTime, out endSeasonTime);
                                Util.Helpers.Time.GetTimeByYear(orderItem.CreatedDate.Year,  out startYearTime, out endYearTime);
                                Util.Helpers.Time.GetTimeByWeek(orderItem.CreatedDate.Year,Util.Helpers.Time.GetWeekNum(orderItem.CreatedDate), out startWeekTime, out endWeekTime);
                              
                                orderItem.OrderRepurchase = new OrderRepurchase()
                                {
                                    DailyRepurchase = true,
                                    MonthRepurchase = s.CreateDate.Value.Date<new DateTime(orderItem.CreatedDate.Year,orderItem.CreatedDate.Month,1).Date?true:false,
                                    SeasonRepurchase = s.CreateDate.Value.Date < startSeasonTime.Date ? true : false,
                                    WeekRepurchase = s.CreateDate.Value.Date < startWeekTime.Date?true:false,
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

                                orderItem.OrderExtendInfo = new OrderExtendInfo() { IsReturningCustomer = false };

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
                        var bar = db.ProductDictionarySet.FirstOrDefault(p => p.ProductId == productsku);
                        if (bar == null || string.IsNullOrEmpty(bar.ProductCode))
                        {
                            Util.Logs.Log.GetLog(nameof(CIBExcelOrderOption)).Error($"订单文件：{file}中平台商品：{productsku}未找到");
                          //  Util.Logs.Log.GetLog(nameof(CIBExcelOrderOption)).Debug($"订单文件：{file}中平台商品：{productName}未找到.order:{Util.Helpers.Json.ToJson(orderItem)}");
                          
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
                            Util.Logs.Log.GetLog(nameof(CIBExcelOrderOption)).Error($"订单文件：{file}中平台商品名称：{productName}对应系统商品未找到");
                         //   Util.Logs.Log.GetLog(nameof(CIBExcelOrderOption)).Debug($"订单文件：{file}中平台商品名称：{productName}对应系统商品未找到.order:{Util.Helpers.Json.ToJson(orderItem)}");
                            continue;
                        }

                        decimal weight = foo==null?0:foo.QuantityPerUnit * quantity;
                        OrderProductInfo orderProductInfo = new OrderProductInfo()
                        {
                            ProductPlatId = productsku,
                            ProductPlatName = productName,
                         //   Warehouse = orderItem.OrderLogistics.Logistics,
                            MonthNum = createdDate.Month,
                            weightCode = foo.weightModel == null ? 0 : foo.weightModel.Code,
                            weightCodeDesc = foo.weightModel == null ? string.Empty : $"{foo.weightModel.Value}g",
                            OrderSn = orderItem.OrderSn,
                            TotalAmount = 0,
                            ProductCount = quantity,
                            ProductWeight = weight,
                            Source = source,
                            sku = foo.sku
                        };
                        orderItem.Products.Add(orderProductInfo);
                        items.Add(orderItem);
                        db.OrderRepurchases.Add(orderItem.OrderRepurchase);
                        db.OrderDateInfos.Add(orderItem.OrderDateInfo);
                        db.OrderLogisticsDetailSet.Add(orderItem.OrderLogistics);
                       
                        db.SaveChanges();
                    }
                    
                }
                else
                {
                    var totalAmount = 0 * quantity;

                    using (var db = new OMSContext())
                    {
                        var bar = db.ProductDictionarySet.FirstOrDefault(p => p.ProductId == productsku);
                        if (bar == null || string.IsNullOrEmpty(bar.ProductCode))
                        {
                            Util.Logs.Log.GetLog(nameof(CIBExcelOrderOption)).Error($"订单文件：{file}中平台商品：{productName}未找到");
                          //  Util.Logs.Log.GetLog(nameof(CIBExcelOrderOption)).Debug($"订单文件：{file}中平台商品：{productName}未找到.order:{Util.Helpers.Json.ToJson(item)}");
                           
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
                            Util.Logs.Log.GetLog(nameof(CIBExcelOrderOption)).Error($"订单文件：{file}中平台商品名称：{productName}对应系统商品未找到");
                           // Util.Logs.Log.GetLog(nameof(CIBExcelOrderOption)).Debug($"订单文件：{file}中平台商品名称：{productName}对应系统商品未找到.order:{Util.Helpers.Json.ToJson(item)}");
                            continue;
                        }


                        decimal weight = foo == null ? 0 : foo.QuantityPerUnit * quantity;
                        OrderProductInfo orderProductInfo = new OrderProductInfo()
                        {
                            ProductPlatId = productsku,
                            ProductPlatName = productName,
                          //  Warehouse = item.OrderLogistics.Logistics,
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

       
    }
}
