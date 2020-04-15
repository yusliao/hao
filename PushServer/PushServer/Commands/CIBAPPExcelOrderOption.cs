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
using PushServer.ModelServer;

namespace PushServer.Commands
{
    /// <summary>
    /// 兴业银行积分
    /// 特点：EXCEL中提供订单号和订单商品编号，商品名称
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

                                dr["物流编号"] = db.logisticsInfoSet.FirstOrDefault(l => l.FullName == logisticsDetail.Logistics)?.BankLogisticsCode;

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
            orderDTO.orderType = 0;
            orderDTO.orderStatus = OrderStatus.Confirmed;
           

            for (int i = 0; i < excelTable.Rows.Count; i++)
            {
                var row = excelTable.Rows[i];

                var orderDateStr = Convert.ToString(row[0]); //订单创建时间
                orderDTO.createdDate = DateTime.Parse(orderDateStr);

                orderDTO.source = OrderSource.CIBAPP;
                orderDTO.sourceDesc = Util.Helpers.Reflection.GetDescription<OrderSource>(OrderSource.CIBAPP);

                orderDTO.sourceSN = Convert.ToString(row[1]); //订单号
                if (string.IsNullOrEmpty(orderDTO.sourceSN))
                {
                    InputExceptionOrder(orderDTO, ExceptionType.SourceSnIsNull);
                    continue;
                }


                orderDTO.productName = Convert.ToString(row[3]); //商品名称
                orderDTO.productsku = Convert.ToString(row[2]); //商品编号
              


                orderDTO.orderSN = string.Format("{0}-{1}_{2}", orderDTO.source, orderDTO.sourceSN, orderDTO.createdDate.ToString("yyyyMMdd"));
                if (CheckOrderInDataBase(orderDTO))//是否是重复订单
                    continue;
                var item = items.Find(o => o.OrderSn == orderDTO.orderSN);
                if (item == null)
                {
                    orderDTO.count = Convert.ToInt32(row[4]); //数量
                   // var productProps = Convert.ToString(row[5]); //商品属性


                    orderDTO.consigneeName = Convert.ToString(row[6]); //收件人
                    orderDTO.consigneePhone = Convert.ToString(row[7]); //联系电话
                    orderDTO.consigneePhone2 = string.Empty;


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

                   
                    var totalQuantity = orderDTO.count;
                   

                    //是否需要发票
                    var invoiceFlag = Convert.ToString(row["是否需要发票"]); //是否需要发票
                    var invoiceType = string.Empty;
                    var invoiceName = Convert.ToString(row["发票抬头"]); //发票抬头
                    if (invoiceFlag.Equals("否"))
                        invoiceType = invoiceName = string.Empty;
                    string paystr = Convert.ToString(row["支付方式"]);
                    switch (paystr)
                    {
                        case "积分支付":
                            orderDTO.PayType = PayType.Integral;
                            
                            break;
                        case "积分+自付金支付":
                            orderDTO.PayType = PayType.IntegralAndMoney;
                            orderDTO.source = OrderSource.CIBEVT;
                            orderDTO.sourceDesc = Util.Helpers.Reflection.GetDescription<OrderSource>(OrderSource.CIBEVT);

                            break;
                        case "分期支付":
                            orderDTO.PayType = PayType.installments;
                            orderDTO.source = OrderSource.CIBSTM;
                            orderDTO.sourceDesc = Util.Helpers.Reflection.GetDescription<OrderSource>(OrderSource.CIBSTM);

                            break;
                        default:
                            orderDTO.PayType = PayType.None;
                            break;
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
                            InputExceptionOrder( orderDTO, ExceptionType.PhoneNumOrPersonNameIsNull);
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
                    orderDTO.productName = Convert.ToString(row[3]); //商品名称
                    orderDTO.productsku = Convert.ToString(row[2]); //商品编号

                    orderDTO.count = Convert.ToInt32(row[4]); //数量

                    using (var db = new OMSContext())
                    {
                        InputProductInfoWithoutSaveChange(db, orderDTO, item);
                    }

                    
                }
            }

            return items;
        }
    }
}
