﻿using System;
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
    /// 电商-团爆品
    /// 特点：EXCEL中提供订单号和订单商品编号，商品名称
    /// </summary>
    [Export(typeof(IOrderOption))]
    class TUANBAOPINExcelOrderOption : OrderOptionBase
    {
        public override string Name => OMS.Models.OrderSource.TUANBAOPIN;

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

                    var table = excel.ExcelToDataTable(null, true);
                    if (table != null)
                        this.ResolveOrders(table, file.FullName, ordersList);
                    else
                    {
                        OnUIMessageEventHandle($"电商-团爆品导入文件：{file.FileName}解析完毕,当前订单数{ordersList.Count}");
                        continue;
                    }
                    
                }
                OnUIMessageEventHandle($"电商-团爆品导入文件：{file.FileName}解析完毕,当前订单数{ordersList.Count}");
            }
            return ordersList;
        }
        protected  List<DataFileInfo> GetExcelFiles()
        {
            var excelFileList = new List<DataFileInfo>();

            FileScanner.ScanAllExcelFiles(new DirectoryInfo(clientConfig.ExcelOrderFolder));
            if (FileScanner.ScannedFiles.Any())
            {
                FileScanner.ScannedFiles.ForEach(file =>
                {
                    excelFileList.Add(new DataFileInfo(file.CreationTime, file.Name, file.FullName));
                });
            }
            excelFileList = excelFileList.OrderBy(f => f.FileDate).ToList();
            
            return excelFileList;
        }


        protected  List<OrderEntity> ResolveOrders(DataTable excelTable,string file, List<OrderEntity> items)
        {

            OrderDTO orderDTO = new OrderDTO();
            orderDTO.orderStatus = OrderStatus.Predelivery;
            orderDTO.fileName = file;
            orderDTO.orderType = 0;
            
           

            for (int i = 0; i < excelTable.Rows.Count; i++)
            {
                var row = excelTable.Rows[i];

               

                orderDTO.source = Name;
                orderDTO.sourceDesc = Util.Helpers.Reflection.GetDescription<OrderSource>(Name);

                orderDTO.sourceSN = Convert.ToString(row["平台单号"]); //订单号
                if (string.IsNullOrEmpty(orderDTO.sourceSN))
                {
                    InputExceptionOrder(orderDTO, ExceptionType.SourceSnIsNull);
                    continue;
                }

                var orderDateStr = Convert.ToString(row["下单时间"]); //订单创建时间
                orderDTO.createdDate = DateTime.Parse(orderDateStr);

                orderDTO.productName = Convert.ToString(row["商品名称"]); //商品名称
               // orderDTO.productsku = Convert.ToString(row[2]); //商品编号
                orderDTO.count = Convert.ToInt32(row["数量"]); //数量


                orderDTO.orderSN = string.Format("{0}-{1}_{2}", orderDTO.source, orderDTO.sourceSN, orderDTO.createdDate.ToString("yyyyMMdd"));
                if (CheckOrderInDataBase(orderDTO))//是否是重复订单
                    continue;
                var item = items.Find(o => o.OrderSn == orderDTO.orderSN);
                if (item == null)
                {
                    
                   // var productProps = Convert.ToString(row[5]); //商品属性


                    orderDTO.consigneeName = Convert.ToString(row["收件人姓名"]); //收件人
                    orderDTO.consigneePhone = Convert.ToString(row["手机号码"]); //联系电话
                    orderDTO.consigneePhone2 = string.Empty;


                    orderDTO.consigneeProvince = Convert.ToString(row["省"]);
                    orderDTO.consigneeCity = Convert.ToString(row["市"]);
                    orderDTO.consigneeCounty = Convert.ToString(row["县"]);
                    orderDTO.consigneeAddress = Convert.ToString(row["详细地址"]); //收货地区+详细地址
                  //  orderDTO.consigneeZipCode = Convert.ToString(row["邮编"]); //邮编

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
                   

                   
                   
                    orderDTO.PayType = PayType.None;
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

                    orderDTO.productName = Convert.ToString(row["商品名称"]); //商品名称
                                                                          
                    orderDTO.count = Convert.ToInt32(row["数量"]); //数量

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
