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
using PushServer.ModelServer;
using Util.Files;

namespace PushServer.Commands
{
    /// <summary>
    /// 银行-兴业银行积点商城
    /// 特点：提供订单编号和商品名称，不提供商品编号
    /// </summary>
    [Export(typeof(IOrderOption))]
    class CIBVIPCSVOrderOption : OrderOptionBase
    {
        public override string Name => OMS.Models.OrderSource.CIBVIP;

        public override IClientConfig clientConfig => AppServer.Instance.ConfigDictionary[Name];
        /// <summary>
        /// 积点，不需要告知客户物流信息，不需要反馈给银行
        /// </summary>
        /// <param name="orders"></param>
        /// <returns></returns>
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

            FileScanner.ScanAllExcelFiles(new DirectoryInfo(clientConfig.ExcelOrderFolder));
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
                orderDTO.source = this.Name;
                orderDTO.sourceDesc = Util.Helpers.Reflection.GetDescription<OrderSource>(orderDTO.source);
                orderDTO.sourceSN = csv.GetField<string>("订单编号").Trim();
                orderDTO.orderType = 0;
                if (string.IsNullOrEmpty(orderDTO.sourceSN))
                {
                    
                    InputExceptionOrder(orderDTO, ExceptionType.SourceSnIsNull);
                    
                    continue;
                }

               
               
                var orderDate = csv.GetField<string>("行权日期");
                var orderTime = csv.GetField<string>("行权时间");
                orderDTO.createdDate = DateTime.ParseExact(string.Format("{0}{1}", orderDate, orderTime), "yyyyMMddHHmmss", CultureInfo.InvariantCulture);
                orderDTO.orderSN_old = string.Format("{0}-{1}", orderDTO.source, orderDTO.sourceSN); //订单SN=来源+原来的SN
                orderDTO.orderSN = string.Format("{0}-{1}_{2}", orderDTO.source, orderDTO.sourceSN, orderDTO.createdDate.ToString("yyyyMMdd"));

                orderDTO.orderStatus = OrderStatus.Confirmed;
               
                var sourceStatus = csv.GetField<string>("订单状态").Trim();
                if (sourceStatus.Contains("撤消"))
                {
                    orderDTO.orderStatus = OrderStatus.Cancelled;
                    orderDTO.orderType = 2;
                }


                orderDTO.productName = csv.GetField<string>("服务项目").Trim();
                orderDTO.count = csv.GetField<int>("本人次数");

                orderDTO.consigneeName = csv.GetField<string>("使用人姓名").Trim();
                if (string.IsNullOrEmpty(orderDTO.consigneeName))
                {
                    orderDTO.consigneeName = csv.GetField<string>("姓名").Trim();
                }

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
                if (CheckOrderInDataBase(orderDTO))
                    continue;
                //内存中查找，没找到就新增对象，找到就关联新的商品
                var item = items.Find(o => o.OrderSn == orderDTO.orderSN);
                if (item == null)
                {
                    OrderEntity orderItem = OrderEntityService.CreateOrderEntity(orderDTO);
                    //处理订单与地址、收货人、商品的关联关系。消除重复项
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
                            continue;

                        }


                        if (!InputProductInfoWithoutSaveChange(db, orderDTO, orderItem))
                        {
                            continue;
                        }
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
                            items.Remove(item);
                            continue;
                        }
                        var foo = db.ProductsSet.Include(p => p.weightModel).FirstOrDefault(p => p.sku == bar.ProductCode);
                        if (foo == null)
                        {
                            Util.Logs.Log.GetLog(nameof(CIBVIPCSVOrderOption)).Error($"订单文件：{file}中平台商品名称：{orderDTO.productName}对应系统商品未找到");
                            
                            items.Remove(item);
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
                             
                            }

                        }
                        else
                        {
                            if (item.Products.FirstOrDefault(p => p.sku == foo.sku) == null)
                            {
                               
                                OrderProductInfo orderProductInfo = new OrderProductInfo()
                                {
                                    ProductPlatId = orderDTO.productsku,
                                    ProductPlatName = orderDTO.productName.Trim(),
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
                           
                        }
                    }
                }

                
            }

            return items;
        }

        
    }
}
