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
                        if(item.OrderLogistics!=null&&item.OrderLogistics.Any())
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

            OrderDTO orderDTO = new OrderDTO();
            orderDTO.orderStatus = OrderStatus.Confirmed;
            orderDTO.fileName = file;

            foreach (DataRow row in excelTable.Rows)
            {
                if (row["礼品名称"] == DBNull.Value
                   || row["兑换礼品数量"] == DBNull.Value
                   || row["领取人姓名"] == DBNull.Value)
                    continue;

                orderDTO.source = OrderSource.CIB;
                orderDTO.sourceDesc = Util.Helpers.Reflection.GetDescription<OrderSource>(OrderSource.CIB);
                var id = Convert.ToString(row["兑换流水编号"]);
              
               

                var sOrderDate = Convert.ToString(row["兑换登记日期"]);
                orderDTO.createdDate = DateTime.Parse(sOrderDate.Insert(4, "-").Insert(7, "-"));

                var sourceAccount = string.Empty;
                if (excelTable.Columns.Contains("持卡人证件号码"))
                    sourceAccount = Convert.ToString(row["持卡人证件号码"]);

                orderDTO.productName = Convert.ToString(row["礼品名称"]);
                orderDTO.productsku = Convert.ToString(row["礼品编号"]);
                orderDTO.count = Convert.ToInt32(row["兑换礼品数量"]);

                var customerName = string.Empty;
                var customerPhone = string.Empty;
                var customerPhone2 = string.Empty;

                orderDTO.consigneeName = Convert.ToString(row["领取人姓名"]);

                orderDTO.consigneeAddress = Convert.ToString(row["递送地址"]);

                var consigneeAddressUnit = string.Empty;
                if (excelTable.Columns.Contains("单位"))
                    consigneeAddressUnit = Convert.ToString(row["单位"]);

                //特殊处理：

                //合并详细地址和单位地址，并且将单位地址设置为空（解决地址被分开，识别度降低问题）

                if (!string.IsNullOrEmpty(consigneeAddressUnit))
                    orderDTO.consigneeAddress = string.Format("{0}{1}", orderDTO.consigneeAddress, consigneeAddressUnit);

                orderDTO.consigneeZipCode = Convert.ToString(row["递送地址邮编"]);

                if (excelTable.Columns.Contains("持卡人姓名"))
                    customerName = Convert.ToString(row["持卡人姓名"]);
                else
                    customerName = Convert.ToString(row["领取人姓名"]);

                //if (excelTable.Columns.Contains("领取人联系电话"))
                //    consigneePhone = Convert.ToString(row["领取人联系电话"]);
                //else if (excelTable.Columns.Contains("领取人联系手机"))
                //    consigneePhone = Convert.ToString(row["领取人联系手机"]);

                if (excelTable.Columns.Contains("分机"))
                    orderDTO.consigneePhone2 = Convert.ToString(row["分机"]);

                if (excelTable.Columns.Contains("手机号码"))
                    customerPhone= orderDTO.consigneePhone = Convert.ToString(row["手机号码"]);
                else
                    customerPhone = orderDTO.consigneePhone;//NOTE: no necessery!
                orderDTO.sourceSN = $"{id}_{orderDTO.createdDate.ToString("yyyyMMdd")}_{customerPhone}_{orderDTO.productsku}";
                orderDTO.orderSN = string.Format("{0}-{1}", orderDTO.source, orderDTO.sourceSN); //订单SN=来源+原来的SN
                if (excelTable.Columns.Contains("领取人所在省份"))
                    orderDTO.consigneeProvince = Convert.ToString(row["领取人所在省份"]);

                if (excelTable.Columns.Contains("领取人所在城市"))
                    orderDTO.consigneeCity = Convert.ToString(row["领取人所在城市"]);

                if (excelTable.Columns.Contains("领取人所在县区"))
                    orderDTO.consigneeCounty = Convert.ToString(row["领取人所在县区"]);

                //
                if (string.IsNullOrEmpty(orderDTO.consigneeProvince)
                    && string.IsNullOrEmpty(orderDTO.consigneeCity) && !string.IsNullOrEmpty(orderDTO.consigneeAddress))
                {
                    var addrInfo = DistrictService.DistrictService.ResolveAddress(orderDTO.consigneeAddress);
                    orderDTO.consigneeProvince = addrInfo.Province;
                    orderDTO.consigneeCity = addrInfo.City;
                    orderDTO.consigneeCounty = addrInfo.County;
                  //  consigneeAddress = addrInfo.Address;
                }

                orderDTO.consigneeName = orderDTO.consigneeName.Split(' ').First();
                customerName = customerName.Split(' ').First();

                //修正处理：
                //当持卡人和领取人一致的时候，领取人的联系号码比较乱（银行的输入/客户填写问题）
                //我们使用持卡人的号码修正领取人的联系号码，以确保领取人的联系号码更有效！
                //if (customerName.Equals(consigneeName) && string.IsNullOrEmpty(consigneePhone))
                //    consigneePhone = customerPhone;
                //订单SN=来源+原来的SN
               
                using (var db = new OMSContext())
                {
                    var foo = db.OrderSet.Find(orderDTO.orderSN);
                    if (foo != null)
                    {
                        Util.Logs.Log.GetLog(nameof(CIBExcelOrderOption)).Error($"订单{foo.OrderSn}已经存在");
                        continue;

                    }
                }
                var item = items.Find(o => o.OrderSn == orderDTO.orderSN);
                if (item == null)
                {
                    var orderItem = new OrderEntity()
                    {
                        SourceSn = orderDTO.sourceSN,
                        Source = orderDTO.source,
                        SourceDesc = orderDTO.sourceDesc,
                        CreatedDate = orderDTO.createdDate,
                        OrderSn = orderDTO.orderSN,
                        Customer = new CustomerEntity()
                        {
                            Name = customerName,
                            Phone = customerPhone,
                            Phone2 = customerPhone2,
                            CreateDate = orderDTO.createdDate
                        },
                        Consignee = new CustomerEntity()
                        {
                            Name = orderDTO.consigneeName,
                            Phone = orderDTO.consigneePhone,
                            Phone2 = orderDTO.consigneePhone2
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
                        InsertOrUpdateProductInfo(db, orderDTO, item);
                       
                    }
                    
                }
            }

            return items;
        }

       
    }
}
