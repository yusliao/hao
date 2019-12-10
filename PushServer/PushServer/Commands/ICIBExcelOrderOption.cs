using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using OMS.Models;
using OMS.Models.DTO;
using PushServer.Configuration;
using PushServer.ModelServer;

namespace PushServer.Commands
{
    [Export(typeof(IOrderOption))]
    class ICIBExcelOrderOption : OrderOptionBase
    {
        public override string Name => OMS.Models.OrderSource.ICIB;

        public override IClientConfig clientConfig => AppServer.Instance.ConfigDictionary[Name];

        public override DataTable ExportExcel(List<OrderEntity> orders)
        {
            throw new NotImplementedException();
        }

        protected override List<OrderEntity> FetchOrders()
        {
            var ordersList = new List<OrderEntity>();
            string sourceDesc = Util.Helpers.Reflection.GetDescription<OrderSource>(Name);
            foreach (var file in this.GetExcelFiles())
            {
                using (var excel = new NPOIExcel(file.FullName))
                {
                    var table = excel.ExcelToDataTable(null, true);
                    if (table != null)
                        this.ResolveOrders(table, file.FullName, ordersList);
                    else
                    {
                        OnUIMessageEventHandle($"{sourceDesc}导入文件：{file.FileName}解析完毕,当前订单数{ordersList.Count}");
                        continue;
                    }
                }
                OnUIMessageEventHandle($"{sourceDesc}导入文件：{file.FileName}解析完毕,当前订单数{ordersList.Count}");
            }
            return ordersList;
        }
        protected  List<DataFileInfo> GetExcelFiles()
        {
            var excelFileList = new List<DataFileInfo>();

            FileScanner.ScanAllFiles(new DirectoryInfo(clientConfig.ExcelOrderFolder), "*.xlsx");
            if (FileScanner.ScannedFiles.Any())
            {
                FileScanner.ScannedFiles.ForEach(file =>
                {
                    var dateStr = file.Name.Split('.').First();
                    var fileDate = DateTime.ParseExact(dateStr, "yyyyMMddHHmmss",null);

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
            foreach (DataRow row in excelTable.Rows)
            {
                orderDTO.sourceSN = Convert.ToString(row["订单号"]).Trim();
                if (row["产品"] == DBNull.Value)

                {
                    InputExceptionOrder(orderDTO, ExceptionType.ProductNameUnKnown);
                    continue;
                }

                var sOrderDate = Convert.ToString(row["订单时间"]);
                orderDTO.createdDate = DateTime.Parse(sOrderDate);

               

                orderDTO.productName = Convert.ToString(row["产品"]);
                
                orderDTO.count = 2;

                var customerName = string.Empty;
                var customerPhone = string.Empty;
                var customerPhone2 = string.Empty;

                orderDTO.consigneeName = Convert.ToString(row["姓名"]);

                orderDTO.consigneeAddress = Convert.ToString(row["地址"]);

              

                //特殊处理：

                //合并详细地址和单位地址，并且将单位地址设置为空（解决地址被分开，识别度降低问题）
               
                customerPhone = orderDTO.consigneePhone = Convert.ToString(row["电话"]);
               
                
              
               
                orderDTO.orderSN = string.Format("{0}-{1}-{2}", orderDTO.source, orderDTO.sourceSN,orderDTO.createdDate); //订单SN=来源+原来的SN

              

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
