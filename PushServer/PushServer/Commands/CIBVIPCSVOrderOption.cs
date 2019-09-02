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
using OMS.Models.DTO;
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
                    var dateStr = file.Name.Split('.').First().Replace("预约报表", "").Trim().Substring(0,"yyyyMMddHHmm".Length);
                    
                    var fileDate = DateTime.ParseExact(dateStr,"yyyyMMddHHmm",CultureInfo.CurrentCulture.DateTimeFormat);

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
                OrderDTO orderDTO = new OrderDTO();
                orderDTO.fileName = file;
                orderDTO.source = OrderSource.CIBVIP;
                orderDTO.sourceDesc = Util.Helpers.Reflection.GetDescription<OrderSource>(orderDTO.source);
                orderDTO.sourceSN = csv.GetField<string>("订单编号").Trim();
                orderDTO.orderSN = string.Format("{0}-{1}", orderDTO.source, orderDTO.sourceSN); //订单SN=来源+原来的SN

                if (string.IsNullOrEmpty(orderDTO.sourceSN))
                    continue;

                var orderDate = csv.GetField<string>("行权日期");
                var orderTime = csv.GetField<string>("行权时间");
                orderDTO.createdDate = DateTime.ParseExact(string.Format("{0}{1}", orderDate, orderTime), "yyyyMMddHHmmss", CultureInfo.InvariantCulture);


                orderDTO.orderStatus = OrderStatus.Confirmed;
                var sourceStatus = csv.GetField<string>("订单状态").Trim();
                if (sourceStatus.Contains("撤消"))
                    orderDTO.orderStatus = OrderStatus.Cancelled;

                orderDTO.productName = csv.GetField<string>("服务项目").Trim();
                orderDTO.count = csv.GetField<int>("本人次数");

                orderDTO.consigneeName = csv.GetField<string>("使用人姓名").Trim();
                orderDTO.consigneePhone = csv.GetField<string>("手机号").Trim();
                

              
                orderDTO.consigneeAddress = csv.GetField<string>("地址").Trim();
               
                if (string.IsNullOrEmpty(orderDTO.consigneeProvince)
                    && string.IsNullOrEmpty(orderDTO.consigneeCity) && !string.IsNullOrEmpty(orderDTO.consigneeAddress))
                {
                    var addrInfo = DistrictService.DistrictService.ResolveAddress(orderDTO.consigneeAddress);
                    orderDTO.consigneeProvince = addrInfo.Province;
                    orderDTO.consigneeCity = addrInfo.City;
                    orderDTO.consigneeCounty = addrInfo.County;
                 
                }
                //数据库中查找订单，如果找到订单了就跳过
                using (var db = new OMSContext())
                {
                    var foo = db.OrderSet.Include(o=>o.Products).FirstOrDefault(o=>o.OrderSn== orderDTO.orderSN);//订单在数据库中
                    if (foo != null)
                    {
                        Util.Logs.Log.GetLog(nameof(CIBVIPCSVOrderOption)).Error($"订单{foo.OrderSn}已经存在");

                        if (orderDTO.orderStatus == OrderStatus.Cancelled)//是否取消订单
                        {
                            var bar = db.ProductDictionarySet.FirstOrDefault(x => x.ProductNameInPlatform == orderDTO.productName);
                            if (bar == null || string.IsNullOrEmpty(bar.ProductCode))
                            {
                                Util.Logs.Log.GetLog(nameof(CIBVIPCSVOrderOption)).Error($"订单文件：{file}中平台商品：{orderDTO.productName}未找到");
                                Util.Logs.Log.GetLog(nameof(CIBVIPCSVOrderOption)).Debug($"订单文件：{file}中平台商品：{orderDTO.productName}未找到.order:{Util.Helpers.Json.ToJson(foo)}");
                                continue;
                            }
                            var p1 = db.ProductsSet.Include(x => x.weightModel).FirstOrDefault(x => x.sku == bar.ProductCode);
                            if (p1 == null)
                            {
                                Util.Logs.Log.GetLog(nameof(CIBVIPCSVOrderOption)).Error($"订单文件：{file}中平台商品名称：{orderDTO.productName}对应系统商品未找到");
                                Util.Logs.Log.GetLog(nameof(CIBVIPCSVOrderOption)).Debug($"订单文件：{file}中平台商品名称：{orderDTO.productName}对应系统商品未找到.order:{Util.Helpers.Json.ToJson(foo)}");
                                continue;
                            }

                            decimal weight = foo == null ? 0 : p1.QuantityPerUnit * orderDTO.count;
                            var p = foo.Products.FirstOrDefault(o => o.sku == p1.sku);
                            if (p != null)
                            {
                                
                                p.ProductCount -= orderDTO.count;
                                p.ProductWeight -= weight;
                                
                                db.SaveChanges();
                            }

                        }
                       
                        continue;
                    }
                }
                //内存中查找，没找到就新增对象，找到就关联新的商品
                var item = items.Find(o => o.OrderSn == orderDTO.orderSN);
                if (item == null)
                {
                    var orderItem = new OrderEntity()
                    {
                        SourceSn = orderDTO.sourceSN,
                        OrderSn = orderDTO.orderSN,
                        Source = orderDTO.source,
                        SourceDesc = orderDTO.sourceDesc,
                        CreatedDate = orderDTO.createdDate,
                        Consignee = new CustomerEntity()
                        {
                            Name = orderDTO.consigneeName,
                            Phone = orderDTO.consigneePhone,
                            Phone2 = orderDTO.consigneePhone2,
                            CreateDate = orderDTO.createdDate
                        },
                        ConsigneeAddress = new AddressEntity()
                        {
                            Address = orderDTO.consigneeAddress,
                            City = orderDTO.consigneeCity,
                            Province = orderDTO.consigneeProvince,
                            County = orderDTO.consigneeCounty,
                            ZipCode = orderDTO.consigneeZipCode
                            
                        },
                        OrderDateInfo = new OrderDateInfo()
                        {
                            CreateTime = orderDTO.createdDate,
                            DayNum = orderDTO.createdDate.DayOfYear,
                            MonthNum = orderDTO.createdDate.Month,
                            WeekNum = Util.Helpers.Time.GetWeekNum(orderDTO.createdDate),
                            SeasonNum  =Util.Helpers.Time.GetSeasonNum(orderDTO.createdDate),
                            Year = orderDTO.createdDate.Year,
                            TimeStamp =  Util.Helpers.Time.GetUnixTimestamp(orderDTO.createdDate)
                        },
                       

                        OrderStatus = (int)orderDTO.orderStatus,
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


                        InsertOrUpdateProductInfo(db, orderDTO, orderItem);
                        items.Add(orderItem);

                        db.OrderRepurchases.Add(orderItem.OrderRepurchase);
                        db.OrderDateInfos.Add(orderItem.OrderDateInfo);
                      
                     
                        db.SaveChanges();
                    }

                }
                else
                {
                    using (var db = new OMSContext())
                    {
                        var bar = db.ProductDictionarySet.FirstOrDefault(p => p.ProductNameInPlatform == orderDTO.productName);
                        if (bar == null || string.IsNullOrEmpty(bar.ProductCode))
                        {
                            Util.Logs.Log.GetLog(nameof(CIBVIPCSVOrderOption)).Error($"订单文件：{file}中平台商品：{orderDTO.productName}未找到");
                          //  Util.Logs.Log.GetLog(nameof(CIBVIPCSVOrderOption)).Debug($"订单文件：{file}中平台商品：{productName}未找到.order:{Util.Helpers.Json.ToJson(item)}");
                            if (bar == null)
                            {
                                ProductDictionary productDictionary = new ProductDictionary()
                                {
                                    ProductNameInPlatform = orderDTO.productName
                                };
                                db.ProductDictionarySet.Add(productDictionary);
                                db.SaveChanges();
                            }
                            continue;
                        }
                        var foo = db.ProductsSet.Include(p => p.weightModel).FirstOrDefault(p => p.sku == bar.ProductCode);
                        if (foo == null)
                        {
                            Util.Logs.Log.GetLog(nameof(CIBVIPCSVOrderOption)).Error($"订单文件：{file}中平台商品名称：{orderDTO.productName}对应系统商品未找到");
                          //  Util.Logs.Log.GetLog(nameof(CIBVIPCSVOrderOption)).Debug($"订单文件：{file}中平台商品名称：{productName}对应系统商品未找到.order:{Util.Helpers.Json.ToJson(item)}");
                            continue;
                        }
                        
                        decimal weight = foo == null ? 0 : foo.QuantityPerUnit * orderDTO.count;
                        if (orderDTO.orderStatus == OrderStatus.Cancelled)
                        {
                            var p = item.Products.FirstOrDefault(o => o.sku == foo.sku);
                            if (p != null)
                            {
                                p.ProductCount -= orderDTO.count;
                                p.ProductWeight -= weight;
                                db.SaveChanges();
                            }

                        }
                        else
                        {
                            InsertOrUpdateProductInfo(db, orderDTO, item);
                           
                        }
                    }
                }

                
            }

            return items;
        }
    }
}
