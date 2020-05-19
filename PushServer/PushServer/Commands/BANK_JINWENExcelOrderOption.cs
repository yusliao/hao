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
using OMS.Models.DTO;
using PushServer.Configuration;
using PushServer.ModelServer;
using Util.Files;

namespace PushServer.Commands
{
    /// <summary>
    /// 银行-金文科技代发
    /// </summary>
    [Export(typeof(IOrderOption))]
    public class BANK_JINWENExcelOrderOption : OrderOptionBase
    {
        /// <summary>
        /// 为了解决多个渠道来源对应一个店铺的解析问题
        /// 通过bankName 来区分不同渠道来源
        /// </summary>
        public class DataFileInfo_jinwen:DataFileInfo
        {
            public DataFileInfo_jinwen( DateTime fileDate, string fileName, string fullName, string bankName) :base(fileDate,fileName,fullName)
            {
                BankName = bankName;
            }
            
            public string BankName { get; set; }
           
        }
        public override string Name => OMS.Models.OrderSource.BANK_JINWEN;
        private string NameDesc = Util.Helpers.Reflection.GetDescription<OrderSource>(OrderSource.BANK_JINWEN);


        public override IClientConfig clientConfig => AppServer.Instance.ConfigDictionary[Name];

       

        protected  List<DataFileInfo_jinwen> GetExcelFiles()
        {
            var excelFileList = new List<DataFileInfo_jinwen>();
           
            string[] paths = clientConfig.ExcelOrderFolder.Split(',');
            //1 农行
            // FileScanner.ScanAllFiles(new DirectoryInfo(paths[0]), "(*.xlsx|*.xls)");
            FileScanner.ScanAllExcelFiles(new DirectoryInfo(paths[0]));
            if (FileScanner.ScannedFiles.Any())
            {
                FileScanner.ScannedFiles.ForEach(file =>
                {
                    //var dateStr = $"{DateTime.Now.Year}{file.Name.Split('-').First().Substring(2, 4)}";
                    //var fileDate = DateTime.ParseExact(dateStr, "yyyyMMdd", CultureInfo.InvariantCulture);
                    

                    excelFileList.Add(new DataFileInfo_jinwen(file.CreationTime, file.Name, file.FullName,"ABC"));
                });

            }
            //2 中信
            int skip = FileScanner.ScannedFiles.Count;
            FileScanner.ScanAllExcelFiles(new DirectoryInfo(paths[1]));
            if (FileScanner.ScannedFiles.Any())
            {
                FileScanner.ScannedFiles.Skip(skip).ToList().ForEach(file =>
                {
                    //var dateStr = $"{DateTime.Now.Year}{file.Name.Split('-')[1]}";
                    //var fileDate = DateTime.ParseExact(dateStr, "yyyyMMdd", CultureInfo.InvariantCulture);


                    excelFileList.Add(new DataFileInfo_jinwen(file.CreationTime, file.Name, file.FullName, "CITIC"));
                });

            }
            //3 南京
            skip = FileScanner.ScannedFiles.Count;
            FileScanner.ScanAllExcelFiles(new DirectoryInfo(paths[2]));
            if (FileScanner.ScannedFiles.Any())
            {
                FileScanner.ScannedFiles.Skip(skip).ToList().ForEach(file =>
                {
                    //var dateStr = $"{DateTime.Now.Year}{file.Name.Split('-').First().Substring(3,4)}";
                    //var fileDate = DateTime.ParseExact(dateStr, "yyyyMMdd", CultureInfo.InvariantCulture);


                    excelFileList.Add(new DataFileInfo_jinwen(file.CreationTime, file.Name, file.FullName,"NANJING"));
                });

            }

            return excelFileList;
        }
       
      

        protected override List<OrderEntity> FetchOrders()
        {

            var ordersList = new List<OrderEntity>();

            foreach (var file in this.GetExcelFiles())
            {
                switch (file.BankName)
                {
                    case "ABC":
                        using (var excel = new NPOIExcel(file.FullName))
                        {
                            var table = excel.ExcelToDataTable(null, true);
                            if (table != null)
                                this.ResolveOrders_ABC(table, file, ordersList);
                            else
                            {
                                OnUIMessageEventHandle($"{NameDesc}导入文件：{file.FileName}解析完毕,当前订单数{ordersList.Count}");
                                continue;
                            }

                        }
                        break;
                    case "CITIC"://中信
                        using (var excel = new NPOIExcel(file.FullName))
                        {
                            var table = excel.ExcelToDataTable(null, true);
                            if (table != null)
                                this.ResolveOrders_CITIC(table, file, ordersList);
                            else
                            {
                                OnUIMessageEventHandle($"{NameDesc}导入文件：{file.FileName}解析完毕,当前订单数{ordersList.Count}");
                                continue;
                            }

                        }
                        break;
                    case "NANJING":
                        using (var excel = new NPOIExcel(file.FullName))
                        {
                            var table = excel.ExcelToDataTable(null, true);
                            if (table != null)
                                this.ResolveOrders_NANJING(table, file, ordersList);
                            else
                            {
                                OnUIMessageEventHandle($"{NameDesc}导入文件：{file.FileName}解析完毕,当前订单数{ordersList.Count}");
                                continue;
                            }

                        }
                        break;
                    
                }
                //using (var excel = new NPOIExcel(file.FullName))
                //{
                //    var table = excel.ExcelToDataTable(null, true);
                //    if (table != null)
                //        this.ResolveOrders(table, file, ordersList);
                //    else
                //    {
                //        OnUIMessageEventHandle($"{NameDesc}导入文件：{file.FileName}解析完毕,当前订单数{ordersList.Count}");
                //        continue;
                //    }
                   
                //}
             //   file.FileName = file.
            }

            return ordersList;
        }

        private List<OrderEntity> ResolveOrders_NANJING(DataTable excelTable, DataFileInfo_jinwen file, List<OrderEntity> items)
        {
            OrderDTO orderDTO = new OrderDTO();
            orderDTO.orderStatus = OrderStatus.Confirmed;
            orderDTO.fileName = file.FullName;
            orderDTO.orderType = 0;
            orderDTO.orderStatus = OrderStatus.Confirmed;


            for (int i = 0; i < excelTable.Rows.Count; i++)
            {
                var row = excelTable.Rows[i];

                //var orderDateStr = Convert.ToString(row["支付时间"]); //订单创建时间
                //orderDTO.createdDate = DateTime.Parse(orderDateStr);
                orderDTO.createdDate = file.FileDate;

                orderDTO.source = this.Name;
                orderDTO.sourceDesc = Util.Helpers.Reflection.GetDescription<OrderSource>(this.Name);

                orderDTO.sourceSN = Convert.ToString(row["订单编号"]); //订单号
                if (string.IsNullOrEmpty(orderDTO.sourceSN))
                {
                    InputExceptionOrder(orderDTO, ExceptionType.SourceSnIsNull);
                    continue;
                }


                orderDTO.productName = Convert.ToString(row["商品名称"]); //商品名称
                                                                      // orderDTO.productsku = Convert.ToString(row[2]); //商品编号



                orderDTO.orderSN = string.Format("{0}-{1}_{2}", orderDTO.source, orderDTO.sourceSN, orderDTO.createdDate.ToString("yyyyMMdd"));
                if (CheckOrderInDataBase(orderDTO))//是否是重复订单
                    continue;
                var item = items.Find(o => o.OrderSn == orderDTO.orderSN);
                if (item == null)
                {
                    orderDTO.count = Convert.ToInt32(row["商品数量"]); //数量



                    orderDTO.consigneeName = Convert.ToString(row["收货人名"]); //收件人
                    orderDTO.consigneePhone = Convert.ToString(row["收货人名手机"]); //联系电话
                    orderDTO.consigneePhone2 = string.Empty;


                    orderDTO.consigneeProvince = string.Empty;
                    orderDTO.consigneeCity = string.Empty;
                    orderDTO.consigneeCounty = string.Empty;
                    orderDTO.consigneeAddress = Convert.ToString(row["收货人地址"]); //收货地区+详细地址
                                                                               // orderDTO.consigneeZipCode = Convert.ToString(row[9]); //邮编

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


                    OrderEntity orderItem = OrderEntityService.CreateOrderEntity(orderDTO);
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
                        if (orderItem.OrderRepurchase == null)
                            orderItem.OrderRepurchase = new OrderRepurchase();
                        db.OrderRepurchases.Add(orderItem.OrderRepurchase);
                        db.OrderDateInfos.Add(orderItem.OrderDateInfo);

                        db.SaveChanges();
                    }

                }
                else
                {
                    orderDTO.consigneeName = Convert.ToString(row["收货人名"]); //收件人
                    orderDTO.consigneePhone = Convert.ToString(row["收货人名手机"]); //联系电话

                    orderDTO.count = Convert.ToInt32(row["商品数量"]); //数量

                    using (var db = new OMSContext())
                    {
                        InputProductInfoWithoutSaveChange(db, orderDTO, item);
                    }


                }
            }

            return items;
        }
        /// <summary>
        /// 解析中信银行订单
        /// </summary>
        /// <param name="excelTable"></param>
        /// <param name="file"></param>
        /// <param name="items"></param>
        private List<OrderEntity> ResolveOrders_CITIC(DataTable excelTable, DataFileInfo_jinwen file, List<OrderEntity> items)
        {
            OrderDTO orderDTO = new OrderDTO();
            orderDTO.orderStatus = OrderStatus.Confirmed;
            orderDTO.fileName = file.FullName;
            orderDTO.orderType = 0;
            orderDTO.orderStatus = OrderStatus.Confirmed;


            for (int i = 0; i < excelTable.Rows.Count; i++)
            {
                var row = excelTable.Rows[i];

                //var orderDateStr = Convert.ToString(row["支付时间"]); //订单创建时间 2020-04-08 23:18:19
                //orderDTO.createdDate = DateTime.Parse(orderDateStr);
                orderDTO.createdDate = file.FileDate;

                orderDTO.source = this.Name;
                orderDTO.sourceDesc = Util.Helpers.Reflection.GetDescription<OrderSource>(this.Name);

                orderDTO.sourceSN = Convert.ToString(row[0]); //订单号
                if (string.IsNullOrEmpty(orderDTO.sourceSN))
                {
                    InputExceptionOrder(orderDTO, ExceptionType.SourceSnIsNull);
                    continue;
                }

                try
                {
                    orderDTO.productName = Convert.ToString(row[2]); //商品名称
                }
                catch (Exception)
                {
                    orderDTO.productName = Convert.ToString(row[2]); //商品名称
                   
                }
               
                                                                      // orderDTO.productsku = Convert.ToString(row[2]); //商品编号



                orderDTO.orderSN = string.Format("{0}-{1}_{2}", orderDTO.source, orderDTO.sourceSN, orderDTO.createdDate.ToString("yyyyMMdd"));
                if (CheckOrderInDataBase(orderDTO))//是否是重复订单
                    continue;
                var item = items.Find(o => o.OrderSn == orderDTO.orderSN);
                if (item == null)
                {

                    try
                    {
                        orderDTO.count = Convert.ToInt32(row[3]); //商品名称
                    }
                    catch (Exception)
                    {
                        orderDTO.count = Convert.ToInt32(row["数量"]); //商品名称

                    }

                    //  orderDTO.count = Convert.ToInt32(row["产品数量"]); //数量

                    try
                    {
                        orderDTO.consigneeName = Convert.ToString(row["收货人姓名"]); //商品名称
                    }
                    catch (Exception)
                    {
                        orderDTO.consigneeName = Convert.ToString(row["收货人"]); //商品名称

                    }

                   // orderDTO.consigneeName = Convert.ToString(row["收货人姓名"]); //收件人
                    try
                    {
                        orderDTO.consigneePhone = Convert.ToString(row["收货人电话1"]); //商品名称
                    }
                    catch (Exception)
                    {
                        orderDTO.consigneePhone = Convert.ToString(row["手机"]); //商品名称

                    }
                   // orderDTO.consigneePhone = Convert.ToString(row["收货人电话1"]); //联系电话
                    orderDTO.consigneePhone2 = string.Empty;


                    orderDTO.consigneeProvince = string.Empty;
                    orderDTO.consigneeCity = string.Empty;
                    orderDTO.consigneeCounty = string.Empty;
                    try
                    {
                        orderDTO.consigneeAddress = Convert.ToString(row["收货地址"]); //商品名称
                    }
                    catch (Exception)
                    {
                        orderDTO.consigneeAddress = Convert.ToString(row["地址"]); //商品名称

                    }
                    // orderDTO.consigneeAddress = Convert.ToString(row["收货地址"]); //收货地区+详细地址
                    // orderDTO.consigneeZipCode = Convert.ToString(row[9]); //邮编

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


                    OrderEntity orderItem = OrderEntityService.CreateOrderEntity(orderDTO);
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
                        if (orderItem.OrderRepurchase == null)
                            orderItem.OrderRepurchase = new OrderRepurchase();
                        db.OrderRepurchases.Add(orderItem.OrderRepurchase);
                        db.OrderDateInfos.Add(orderItem.OrderDateInfo);

                        db.SaveChanges();
                    }

                }
                else
                {
                    try
                    {
                        orderDTO.consigneeName = Convert.ToString(row["收货人姓名"]); //商品名称
                    }
                    catch (Exception)
                    {
                        orderDTO.consigneeName = Convert.ToString(row["收货人"]); //商品名称

                    }

                    // orderDTO.consigneeName = Convert.ToString(row["收货人姓名"]); //收件人
                    try
                    {
                        orderDTO.consigneePhone = Convert.ToString(row["收货人电话1"]); //商品名称
                    }
                    catch (Exception)
                    {
                        orderDTO.consigneePhone = Convert.ToString(row["手机"]); //商品名称

                    }

                    try
                    {
                        orderDTO.count = Convert.ToInt32(row[3]); //商品名称
                    }
                    catch (Exception)
                    {
                        orderDTO.count = Convert.ToInt32(row["数量"]); //商品名称

                    }

                    using (var db = new OMSContext())
                    {
                        InputProductInfoWithoutSaveChange(db, orderDTO, item);
                    }


                }
            }

            return items;
        }

        protected List<OrderEntity> ResolveOrders_ABC(DataTable excelTable, DataFileInfo file, List<OrderEntity> items)
        {

            OrderDTO orderDTO = new OrderDTO();
            orderDTO.orderStatus = OrderStatus.Confirmed;
            orderDTO.fileName = file.FullName;
            orderDTO.orderType = 0;
            orderDTO.orderStatus = OrderStatus.Confirmed;


            for (int i = 0; i < excelTable.Rows.Count; i++)
            {
                var row = excelTable.Rows[i];

                var orderDateStr = Convert.ToString(row[1]); //订单创建时间 20200411 23:03:32
                orderDTO.createdDate = DateTime.ParseExact(orderDateStr,"yyyyMMdd HH:mm:ss", CultureInfo.InvariantCulture);
                

                orderDTO.source = this.Name;
                orderDTO.sourceDesc = Util.Helpers.Reflection.GetDescription<OrderSource>(this.Name);

                orderDTO.sourceSN = Convert.ToString(row[0]); //订单号
                if (string.IsNullOrEmpty(orderDTO.sourceSN))
                {
                    InputExceptionOrder(orderDTO, ExceptionType.SourceSnIsNull);
                    continue;
                }


                orderDTO.productName = Convert.ToString(row["商品名称"]); //商品名称
                                                                      // orderDTO.productsku = Convert.ToString(row[2]); //商品编号



                orderDTO.orderSN = string.Format("{0}-{1}_{2}", orderDTO.source, orderDTO.sourceSN, orderDTO.createdDate.ToString("yyyyMMdd"));
                if (CheckOrderInDataBase(orderDTO))//是否是重复订单
                    continue;
                var item = items.Find(o => o.OrderSn == orderDTO.orderSN);
                if (item == null)
                {
                    orderDTO.count = Convert.ToInt32(row["数量"]); //数量



                    orderDTO.consigneeName = Convert.ToString(row["收件人姓名"]); //收件人
                    orderDTO.consigneePhone = Convert.ToString(row["收件人联系方式"]); //联系电话
                    orderDTO.consigneePhone2 = string.Empty;


                    orderDTO.consigneeProvince = string.Empty;
                    orderDTO.consigneeCity = string.Empty;
                    orderDTO.consigneeCounty = string.Empty;
                    orderDTO.consigneeAddress = Convert.ToString(row["收件人地址"]); //收货地区+详细地址
                                                                             // orderDTO.consigneeZipCode = Convert.ToString(row[9]); //邮编

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


                    OrderEntity orderItem = OrderEntityService.CreateOrderEntity(orderDTO);
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
                        if (orderItem.OrderRepurchase == null)
                            orderItem.OrderRepurchase = new OrderRepurchase();
                        db.OrderRepurchases.Add(orderItem.OrderRepurchase);
                        db.OrderDateInfos.Add(orderItem.OrderDateInfo);

                        db.SaveChanges();
                    }

                }
                else
                {
                    orderDTO.consigneeName = Convert.ToString(row["收件人姓名"]); //收件人
                    orderDTO.consigneePhone = Convert.ToString(row["收件人联系方式"]); //联系电话

                    orderDTO.count = Convert.ToInt32(row["数量"]); //数量

                    using (var db = new OMSContext())
                    {
                        InputProductInfoWithoutSaveChange(db, orderDTO, item);
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
                                dr["物流公司编码"] = db.logisticsInfoSet.FirstOrDefault(l => l.FullName == logisticsDetail.Logistics)?.BankLogisticsCode;
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
