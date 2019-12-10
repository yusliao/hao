using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Data;
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
    [Export(typeof(IOrderOption))]
    class CMBCExcelOrderOption : OrderOptionBase
    {
        public override string Name => OMS.Models.OrderSource.CMBC;

        public override IClientConfig clientConfig => AppServer.Instance.ConfigDictionary[Name];

        public override DataTable ExportExcel(List<OrderEntity> orders)
        {
            throw new NotImplementedException();
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
                        OnUIMessageEventHandle($"民生银行导入文件：{file.FileName}解析完毕,当前订单数{ordersList.Count}");
                        continue;
                    }
                }
                OnUIMessageEventHandle($"民生银行导入文件：{file.FileName}解析完毕,当前订单数{ordersList.Count}");
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
                    var dateStr = file.Name.Split('.').First();
                    if (dateStr.Contains("供货商发货"))
                        dateStr = dateStr.Substring(5, 8);
                    else
                        dateStr = dateStr.Substring(6, 8);

                    var fileDate = DateTime.ParseExact(dateStr, "yyyyMMdd", CultureInfo.InvariantCulture);

                    excelFileList.Add(new DataFileInfo(fileDate, file.Name, file.FullName));
                });
            }

            excelFileList = excelFileList.OrderBy(f => f.FileDate).ToList();

            return excelFileList;


           

        
            

        }


        protected List<OrderEntity> ResolveOrders(DataTable excelTable, string file, List<OrderEntity> items)
        {
            OrderDTO orderDTO = new OrderDTO();
            orderDTO.orderStatus = OrderStatus.Confirmed;
            orderDTO.fileName = file;
            orderDTO.source = Name;
            orderDTO.orderType = 0;
            orderDTO.sourceDesc = Util.Helpers.Reflection.GetDescription<OrderSource>(Name);
            for (int i = 0; i < excelTable.Rows.Count; i++)
            {
                var row = excelTable.Rows[i];
                if (row["姓名"] == DBNull.Value
                    || row["产品名称"] == DBNull.Value
                    || row["联系电话"] == DBNull.Value
                    || row["兑奖申请日期"] == DBNull.Value
                    || string.IsNullOrEmpty(row["兑奖申请日期"].ToString().Trim()))
                {
                    InputExceptionOrder(orderDTO, ExceptionType.PhoneNumOrPersonNameIsNull);
                    
                    continue;
                }

                var id = orderDTO.sourceSN = Convert.ToString(row["交易流水号"]).Trim();
                if (row["产品名称"] == DBNull.Value
                   || row["产品数量"] == DBNull.Value)

                {
                    InputExceptionOrder(orderDTO, ExceptionType.ProductNameUnKnown);
                    continue;
                }
                else
                {
                    orderDTO.productName = Convert.ToString(row["产品名称"]);

                    orderDTO.count = Convert.ToInt32(row["产品数量"]);
                }

                var sOrderDate = Convert.ToString(row["兑奖申请日期"]);
                if (string.IsNullOrEmpty(sOrderDate))
                    continue;
                orderDTO.createdDate = DateTime.Parse(sOrderDate);
                

                var customerName = string.Empty;
                var customerPhone = string.Empty;
                var customerPhone2 = string.Empty;

                orderDTO.consigneeName = Convert.ToString(row["姓名"]);

                orderDTO.consigneeAddress = Convert.ToString(row["收件人地址"]).Replace("中国", "").Trim();
                
                orderDTO.consigneeZipCode = Convert.ToString(row["邮编"]);

                customerName = Convert.ToString(row["姓名"]);
              



               
                customerPhone = orderDTO.consigneePhone = Convert.ToString(row["联系电话"]);
                
               
                orderDTO.orderSN = string.Format("{0}-{1}_{2}", orderDTO.source, orderDTO.sourceSN, orderDTO.createdDate.ToString("yyyyMMdd"));

                

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

               
              

                if (CheckOrderInDataBase(orderDTO))
                    continue;
                var item = items.Find(o => o.OrderSn == orderDTO.orderSN);
                if (item == null)
                {
                    OrderEntity orderItem = OrderEntityService.CreateOrderEntity(orderDTO);
                    using (var db = new OMSContext())
                    {
                        //处理收货人相关的业务逻辑
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
                        else
                        {

                            items.Add(orderItem);
                            db.OrderRepurchases.Add(orderItem.OrderRepurchase);
                            db.OrderDateInfos.Add(orderItem.OrderDateInfo);


                            db.SaveChanges();
                        }
                    }

                }
                else
                {

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
