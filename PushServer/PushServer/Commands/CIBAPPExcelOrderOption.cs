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

                        if (item.OrderLogistics != null && item.OrderLogistics.Any())
                        {
                            foreach (var logisticsDetail in item.OrderLogistics)
                            {
                                var dr = dt.NewRow();
                                dr["订单号"] = item.SourceSn;
                                dr["物流单号"] = logisticsDetail.LogisticsNo;

                                dr["物流编号"] = db.logisticsInfoSet.FirstOrDefault(l => l.FullName == logisticsDetail.Logistics).BankLogisticsCode;

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

            OrderDTO orderDTO = new OrderDTO();
            orderDTO.orderStatus = OrderStatus.Confirmed;
            orderDTO.fileName = file;
            var sourceStatus = excelTable.Columns[0].ColumnName.Split(',').First().Split('：').Last();
            switch (sourceStatus)
            {
                case "待发货":
                    orderDTO.orderStatus = OrderStatus.Confirmed;
                    break;
                case "待收货":
                    orderDTO.orderStatus = OrderStatus.Delivered;
                    break;
                case "待评价":
                case "已评价":
                    orderDTO.orderStatus = OrderStatus.Finished;
                    break;
            }

            for (int i = 1; i < excelTable.Rows.Count; i++)
            {
                var row = excelTable.Rows[i];

                var orderDateStr = Convert.ToString(row[0]); //订单创建时间
                orderDTO.createdDate = DateTime.Parse(orderDateStr);

                orderDTO.source = OrderSource.CIBAPP;
                orderDTO.sourceDesc = Util.Helpers.Reflection.GetDescription<OrderSource>(OrderSource.CIBAPP);

                orderDTO.sourceSN = Convert.ToString(row[1]); //订单号
                if (string.IsNullOrEmpty(orderDTO.sourceSN))
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
                orderDTO.orderSN = string.Format("{0}-{1}", orderDTO.source, orderDTO.sourceSN);
                using (var db = new OMSContext())
                {
                    var foo = db.OrderSet.Find(orderDTO.orderSN);
                    if (foo != null)
                    {
                        Util.Logs.Log.GetLog(nameof(CIBAPPExcelOrderOption)).Error($"订单{foo.OrderSn}已经存在");
                        continue;

                    }
                }
                var item = items.Find(o => o.OrderSn == orderDTO.orderSN);
                if (item == null)
                {
                    decimal unitPrice = 0M;
                    orderDTO.productName = Convert.ToString(row[3]); //商品名称
                    orderDTO.productsku = Convert.ToString(row[2]); //商品编号
                    if (orderDTO.productName.Equals("水清清特供稻花香精装礼盒2.5KG"))
                        unitPrice = 56.00M;
                    else if (orderDTO.productName.Equals("水清清冠军优选四季经典4KG*1提"))
                        unitPrice = 108.00M;

                    orderDTO.count = Convert.ToInt32(row[4]); //数量
                    var productProps = Convert.ToString(row[5]); //商品属性


                    orderDTO.consigneeName = Convert.ToString(row[6]); //收件人
                    orderDTO.consigneePhone = Convert.ToString(row[7]); //联系电话
                    var consigneePhone2 = string.Empty;


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

                    var totalAmount = 0;//?
                    var totalQuantity = orderDTO.count;
                   

                    //是否需要发票
                    var invoiceFlag = Convert.ToString(row["是否需要发票"]); //是否需要发票
                    var invoiceType = string.Empty;
                    var invoiceName = Convert.ToString(row["发票抬头"]); //发票抬头
                    if (invoiceFlag.Equals("否"))
                        invoiceType = invoiceName = string.Empty;

                    var orderItem = new OrderEntity()
                    {
                        SourceSn = orderDTO.sourceSN,
                        Source = orderDTO.source,
                        SourceDesc = orderDTO.sourceDesc,
                        CreatedDate = orderDTO.createdDate,
                        OrderSn = orderDTO.orderSN,
                        Consignee = new CustomerEntity()
                        {
                            Name = orderDTO.consigneeName,
                            Phone = orderDTO.consigneePhone,
                            Phone2 = consigneePhone2,
                            CreateDate = orderDTO.createdDate
                        },
                        ConsigneeAddress = new AddressEntity()
                        {
                            Address = orderDTO.consigneeAddress,
                            City = orderDTO.consigneeCity,
                            County = orderDTO.consigneeCounty,
                            Province = orderDTO.consigneeProvince,
                            ZipCode = orderDTO.consigneeZipCode
                        },
                       
                        OrderDateInfo = new OrderDateInfo()
                        {
                            CreateTime = orderDTO.createdDate,
                            DayNum = orderDTO.createdDate.DayOfYear,
                            MonthNum = orderDTO.createdDate.Month,
                            WeekNum = Util.Helpers.Time.GetWeekNum(orderDTO.createdDate),
                            SeasonNum = Util.Helpers.Time.GetSeasonNum(orderDTO.createdDate),
                            Year = orderDTO.createdDate.Year,
                            TimeStamp = Util.Helpers.Time.GetUnixTimestamp(orderDTO.createdDate)
                        },
                        OrderStatus = (int)orderDTO.orderStatus,
                        OrderStatusDesc = Util.Helpers.Enum.GetDescription(typeof(OrderStatus), orderDTO.orderStatus),


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

                        InsertOrUpdateProductInfo(db, orderDTO, orderItem);
                       
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
                        InsertOrUpdateProductInfo(db, orderDTO, item);
                    }

                    
                }
            }

            return items;
        }
    }
}
