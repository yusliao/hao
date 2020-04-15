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
    /// 银行-金文（工行代发）
    /// </summary>
    [Export(typeof(IOrderOption))]
    public class ICBC_JINWENENExcelOrderOption : OrderOptionBase
    {
        public override string Name => OMS.Models.OrderSource.ICBC_JINWEN;
        private string NameDesc = Util.Helpers.Reflection.GetDescription<OrderSource>(OrderSource.ICBC_JINWEN);


        public override IClientConfig clientConfig => AppServer.Instance.ConfigDictionary[Name];

       

        protected  List<DataFileInfo> GetExcelFiles()
        {
            var excelFileList = new List<DataFileInfo>();


            // FileScanner.ScanAllFiles(new DirectoryInfo(clientConfig.ExcelOrderFolder), "(*.xlsx|*.xls)");
            FileScanner.ScanAllExcelFiles(new DirectoryInfo(clientConfig.ExcelOrderFolder));
            if (FileScanner.ScannedFiles.Any())
            {
                FileScanner.ScannedFiles.ForEach(file =>
                {
                    //var dateStr = $"{DateTime.Now.Year}{file.Name.Split('-').First().Substring(4, 4)}";
                    //var fileDate = DateTime.ParseExact(dateStr, "yyyyMMdd", CultureInfo.InvariantCulture);
                    

                    excelFileList.Add(new DataFileInfo(file.CreationTime, file.Name, file.FullName));
                });

            }

            excelFileList = excelFileList.OrderBy(f => f.FileDate).ToList();

            return excelFileList;
            
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
                        this.ResolveOrders(table, file, ordersList);
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

       
        protected  List<OrderEntity> ResolveOrders(DataTable excelTable,DataFileInfo file, List<OrderEntity> items)
        {

            OrderDTO orderDTO = new OrderDTO();
            orderDTO.orderStatus = OrderStatus.Confirmed;
            orderDTO.fileName = file.FullName;
            orderDTO.orderType = 0;
            orderDTO.orderStatus = OrderStatus.Confirmed;


            for (int i = 0; i < excelTable.Rows.Count; i++)
            {
                var row = excelTable.Rows[i];

                
                orderDTO.createdDate = file.FileDate;

                orderDTO.source = this.Name;
                orderDTO.sourceDesc = Util.Helpers.Reflection.GetDescription<OrderSource>(this.Name);

                orderDTO.sourceSN = Convert.ToString(row["交易号"]); //订单号
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
                   


                    orderDTO.consigneeName = Convert.ToString(row["收货人"]); //收件人
                    orderDTO.consigneePhone = Convert.ToString(row["手机"]); //联系电话
                    orderDTO.consigneePhone2 = string.Empty;


                    orderDTO.consigneeProvince = string.Empty;
                    orderDTO.consigneeCity = string.Empty;
                    orderDTO.consigneeCounty = string.Empty;
                    orderDTO.consigneeAddress = Convert.ToString(row["地址"]); //收货地区+详细地址
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
                    orderDTO.consigneeName = Convert.ToString(row["收货人"]); //收件人
                    orderDTO.consigneePhone = Convert.ToString(row["手机"]); //联系电话

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
