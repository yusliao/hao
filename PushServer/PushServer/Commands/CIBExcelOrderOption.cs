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
    /// 兴业银行积分PC
    /// 特点：提供兑换流水编号+付款时间+收货人手机号+商品编号 组成的MD5值作为原始订单编号，提供商品编号
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
            orderDTO.source = Name;
            orderDTO.orderType = 0;
            orderDTO.sourceDesc = Util.Helpers.Reflection.GetDescription<OrderSource>(Name);
            foreach (DataRow row in excelTable.Rows)
            {
                var id= orderDTO.sourceSN = Convert.ToString(row["兑换流水编号"]).Trim();
                if (row["礼品名称"] == DBNull.Value
                   || row["兑换礼品数量"] == DBNull.Value)
                   
                {
                    InputExceptionOrder(orderDTO, ExceptionType.ProductNameUnKnown);
                    continue;
                }

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

             

                if (excelTable.Columns.Contains("分机"))
                    orderDTO.consigneePhone2 = Convert.ToString(row["分机"]);

                if (excelTable.Columns.Contains("手机号码"))
                    customerPhone= orderDTO.consigneePhone = Convert.ToString(row["手机号码"]);
                else
                    customerPhone = orderDTO.consigneePhone;//NOTE: no necessery!
                //兑换流水+下单日期+手机号+商品编号
                var sourceSN = $"{id.Trim()}_{orderDTO.createdDate.ToString("yyyyMMdd")}_{customerPhone}_{orderDTO.productsku}";
                orderDTO.orderSN_old= string.Format("{0}-{1}", orderDTO.source, orderDTO.sourceSN);
                orderDTO.sourceSN = Util.Helpers.Encrypt.Md5By16(sourceSN);
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
                            InputExceptionOrder(orderDTO,ExceptionType.PhoneNumIsNull);
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
