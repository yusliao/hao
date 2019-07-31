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

using OMS.Models;
using PushServer.Configuration;
using Util.Files;

namespace PushServer.Commands
{
    [Export(typeof(IOrderOption))]
    class CIBVIPCSVOrderOption : OrderOptionBase
    {
        public override string Name => OMS.Models.OrderSource.CIBVIP;

        public override IClientConfig clientConfig => AppServer.Instance.ConfigDictionary[Name];

        public override DataTable ExportExcel(List<OrderEntity> orders)
        {
            return null;
        }

        protected override List<OrderEntity> FetchOrders()
        {
            var ordersList = new List<OrderEntity>();

            foreach (var file in this.GetCSVFiles())
            {
                using (var csv = new CsvReader(new StreamReader(file.FullName, Encoding.Default)))
                {
                    ResolveOrders(csv,file.FullName, ordersList);
                }
                OnUIMessageEventHandle($"兴业积点导入文件：{file.FileName}解析完毕，当前订单数{ordersList.Count}");
            }

          

            return ordersList;
        }
        private List<DataFileInfo> GetCSVFiles()
        {
            var excelFileList = new List<DataFileInfo>();

            FileScanner.ScanAllFiles(new DirectoryInfo(clientConfig.ExcelOrderFolder), "*.csv");
            if (FileScanner.ScannedFiles.Any())
            {
                FileScanner.ScannedFiles.ForEach(file =>
                {
                    var dateStr = file.Name.Split('.').First().Replace("预约报表", "").Trim();
                    var fileDate = DateTime.ParseExact(dateStr, "yyyyMMddHHmm", CultureInfo.InvariantCulture);

                    excelFileList.Add(new DataFileInfo(fileDate, file.Name, file.FullName));
                });
            }
         
            excelFileList = excelFileList.OrderBy(f => f.FileDate).ToList();

            return excelFileList;
        }
        private List<OrderEntity> ResolveOrders(CsvReader csv,string file,List<OrderEntity> items)
        {
            csv.Read();
            csv.ReadHeader();
            while (csv.Read())
            {
                var source = OrderSource.CIBVIP;
                var sourcedesc = Util.Helpers.Reflection.GetDescription<OrderSource>(source);
                var sourceSN = csv.GetField<string>("订单编号").Trim();
                var orderSN = string.Format("{0}-{1}", source, sourceSN); //订单SN=来源+原来的SN

                if (string.IsNullOrEmpty(sourceSN))
                    continue;

                var orderDate = csv.GetField<string>("行权日期");
                var orderTime = csv.GetField<string>("行权时间");
                var createdDate = DateTime.ParseExact(string.Format("{0}{1}", orderDate, orderTime), "yyyyMMddHHmmss", CultureInfo.InvariantCulture);
              

                var orderStatus = OrderStatus.Confirmed;
                var sourceStatus = csv.GetField<string>("订单状态").Trim();
                if (sourceStatus.Contains("撤消"))
                    orderStatus = OrderStatus.Cancelled;
                
                var productName = csv.GetField<string>("服务项目").Trim();
                var quantity = csv.GetField<int>("本人次数");

                var consigneeName = csv.GetField<string>("使用人姓名").Trim();
                var consigneePhone = csv.GetField<string>("手机号").Trim();
                var consigneePhone2 = string.Empty;

                var consigneeProvince = string.Empty;
                var consigneeCity = string.Empty;
                var consigneeCounty = string.Empty;
                var consigneeAddress = csv.GetField<string>("地址").Trim();
                var consigneeZipCode = string.Empty;
                //
                if (string.IsNullOrEmpty(consigneeProvince)
                    && string.IsNullOrEmpty(consigneeCity) && !string.IsNullOrEmpty(consigneeAddress))
                {
                    var addrInfo = DistrictService.DistrictService.ResolveAddress(consigneeAddress);
                    consigneeProvince = addrInfo.Province;
                    consigneeCity = addrInfo.City;
                    consigneeCounty = addrInfo.County;
                 
                }
                //数据库中查找订单，如果找到订单了就跳过
                using (var db = new OMSContext())
                {
                    var foo = db.OrderSet.Include(o=>o.Products).FirstOrDefault(o=>o.OrderSn==orderSN);//订单在数据库中
                    if (foo != null)
                    {
                        Util.Logs.Log.GetLog(nameof(CIBVIPCSVOrderOption)).Error($"订单{foo.OrderSn}已经存在");

                        if (orderStatus == OrderStatus.Cancelled)//是否取消订单
                        {
                            var bar = db.ProductDictionarySet.FirstOrDefault(x => x.ProductNameInPlatform == productName);
                            if (bar == null || string.IsNullOrEmpty(bar.ProductCode))
                            {
                                Util.Logs.Log.GetLog(nameof(CIBVIPCSVOrderOption)).Error($"订单文件：{file}中平台商品：{productName}未找到");
                                Util.Logs.Log.GetLog(nameof(CIBVIPCSVOrderOption)).Debug($"订单文件：{file}中平台商品：{productName}未找到.order:{Util.Helpers.Json.ToJson(foo)}");
                                continue;
                            }
                            var p1 = db.ProductsSet.Include(x => x.weightModel).FirstOrDefault(x => x.sku == bar.ProductCode);
                            if (p1 == null)
                            {
                                Util.Logs.Log.GetLog(nameof(CIBVIPCSVOrderOption)).Error($"订单文件：{file}中平台商品名称：{productName}对应系统商品未找到");
                                Util.Logs.Log.GetLog(nameof(CIBVIPCSVOrderOption)).Debug($"订单文件：{file}中平台商品名称：{productName}对应系统商品未找到.order:{Util.Helpers.Json.ToJson(foo)}");
                                continue;
                            }

                            decimal weight = foo == null ? 0 : p1.QuantityPerUnit * quantity;
                            var p = foo.Products.FirstOrDefault(o => o.sku == p1.sku);
                            if (p != null)
                            {
                                
                                p.ProductCount -= quantity;
                                p.ProductWeight -= weight;
                                
                                db.SaveChanges();
                            }

                        }
                       
                        continue;
                    }
                }
                //内存中查找，没找到就新增对象，找到就关联新的商品
                var item = items.Find(o => o.OrderSn == orderSN);
                if (item == null)
                {
                    var orderItem = new OrderEntity()
                    {
                        SourceSn = sourceSN,
                        OrderSn = orderSN,
                        Source = source,
                        SourceDesc = sourcedesc,
                        CreatedDate = createdDate,
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
                            Province = consigneeProvince,
                            County = consigneeCounty,
                            ZipCode = consigneeZipCode
                            
                        },
                        OrderDateInfo = new OrderDateInfo()
                        {
                            CreateTime = createdDate,
                            DayNum = createdDate.DayOfYear,
                            MonthNum = createdDate.Month,
                            WeekNum = Util.Helpers.Time.GetWeekNum(createdDate),
                            SeasonNum  =Util.Helpers.Time.GetSeasonNum(createdDate),
                            Year = createdDate.Year,
                            TimeStamp =  Util.Helpers.Time.GetUnixTimestamp(createdDate)
                        },
                       

                        OrderStatus = (int)orderStatus,
                        OrderStatusDesc = sourceStatus,


                        Remarks = string.Empty
                    };
                    if (orderItem.Products == null)
                        orderItem.Products = new List<OrderProductInfo>();
                    //处理订单与地址、收货人、商品的关联关系。消除重复项
                    using (var db = new OMSContext())
                    {
                        //查找联系人
                        if (!string.IsNullOrEmpty(orderItem.Consignee.Phone))
                        {
                            string md5 = Util.Helpers.Encrypt.Md5By32(orderItem.ConsigneeAddress.Address.Trim().Replace(" ", ""));
                            var s = db.CustomersSet.Include<CustomerEntity,ICollection<AddressEntity>>(c=>c.Addresslist).FirstOrDefault(c => c.Name == orderItem.Consignee.Name && c.Phone == orderItem.Consignee.Phone);
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

                                if (s.Addresslist.Any(a=>a.MD5==md5))
                                {
                                    var addr= s.Addresslist.First(a => a.MD5 == md5);
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
                                orderItem.OrderExtendInfo = new OrderExtendInfo() { IsReturningCustomer = false };
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
                            Util.Logs.Log.GetLog(nameof(CIBVIPCSVOrderOption)).Error($"订单文件：{file}中平台商品：{productName}未找到");
                          //  Util.Logs.Log.GetLog(nameof(CIBVIPCSVOrderOption)).Debug($"订单文件：{file}中平台商品：{productName}未找到.order:{Util.Helpers.Json.ToJson(orderItem)}");
                            if (bar == null)
                            {
                                ProductDictionary productDictionary = new ProductDictionary()
                                {
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
                            Util.Logs.Log.GetLog(nameof(CIBVIPCSVOrderOption)).Error($"订单文件：{file}中平台商品名称：{productName}对应系统商品未找到");
                          //  Util.Logs.Log.GetLog(nameof(CIBVIPCSVOrderOption)).Debug($"订单文件：{file}中平台商品名称：{productName}对应系统商品未找到.order:{Util.Helpers.Json.ToJson(orderItem)}");
                            continue;
                        }

                        decimal weight = foo == null ? 0 : foo.QuantityPerUnit * quantity;
                        OrderProductInfo orderProductInfo = new OrderProductInfo()
                        {
                         //   ProductPlatId = sourceSN,
                            ProductPlatName = productName,
                          //  Warehouse = orderItem.OrderLogistics.Logistics,
                            
                            MonthNum = createdDate.Month,
                            weightCode = foo.weightModel==null?0:foo.weightModel.Code,
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
                      
                      //  db.OrderProductSet.Add(orderProductInfo);
                        db.SaveChanges();
                    }

                }
                else
                {
                    using (var db = new OMSContext())
                    {
                        var bar = db.ProductDictionarySet.FirstOrDefault(p => p.ProductNameInPlatform == productName);
                        if (bar == null || string.IsNullOrEmpty(bar.ProductCode))
                        {
                            Util.Logs.Log.GetLog(nameof(CIBVIPCSVOrderOption)).Error($"订单文件：{file}中平台商品：{productName}未找到");
                          //  Util.Logs.Log.GetLog(nameof(CIBVIPCSVOrderOption)).Debug($"订单文件：{file}中平台商品：{productName}未找到.order:{Util.Helpers.Json.ToJson(item)}");
                            if (bar == null)
                            {
                                ProductDictionary productDictionary = new ProductDictionary()
                                {
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
                            Util.Logs.Log.GetLog(nameof(CIBVIPCSVOrderOption)).Error($"订单文件：{file}中平台商品名称：{productName}对应系统商品未找到");
                          //  Util.Logs.Log.GetLog(nameof(CIBVIPCSVOrderOption)).Debug($"订单文件：{file}中平台商品名称：{productName}对应系统商品未找到.order:{Util.Helpers.Json.ToJson(item)}");
                            continue;
                        }
                        
                        decimal weight = foo == null ? 0 : foo.QuantityPerUnit * quantity;
                        if (orderStatus == OrderStatus.Cancelled)
                        {
                            var p = item.Products.FirstOrDefault(o => o.sku == foo.sku);
                            if (p != null)
                            {
                                p.ProductCount -= quantity;
                                p.ProductWeight -= weight;
                                db.SaveChanges();
                            }

                        }
                        else
                        {
                            OrderProductInfo orderProductInfo = new OrderProductInfo()
                            {
                               // ProductPlatId = productName,
                                ProductPlatName = productName,
                              //  Warehouse = item.OrderLogistics.Logistics,
                                MonthNum = createdDate.Month,
                                weightCode = foo.weightModel == null ? 0 : foo.weightModel.Code,
                                weightCodeDesc = foo.weightModel == null ? string.Empty : $"{foo.weightModel.Value}g",
                                OrderSn = item.OrderSn,
                                TotalAmount = 0,
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

                
            }

            return items;
        }
    }
}
