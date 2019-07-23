using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FusionStone.District;
using OMS.Models;
using PushServer.Configuration;

namespace PushServer.Commands
{
    [Export(typeof(IOrderOption))]
    class CIBSTMExcelOrderOption : OrderOptionBase
    {
        public override string Name => OMS.Models.OrderSource.CIBSTM;

        public override IClientConfig clientConfig => AppServer.Instance.ConfigDictionary[Name];

        protected override List<OrderEntity> FetchOrders()
        {
            var ordersList = new List<OrderEntity>();

            foreach (var file in this.GetExcelFiles())
            {
                using (var excel = new NPOIExcel(file.FullName))
                {
                    var table = excel.ExcelToDataTable(null, true);
                    var orderItems = this.ResolveOrders(table);

                    ordersList.AddRange(orderItems);
                   
                }
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


        protected  List<OrderEntity> ResolveOrders(DataTable excelTable)
        {
            var items = new List<OrderEntity>();

            var orderStatus = OrderStatus.Confirmed;

            var sourceStatus = excelTable.Columns[0].ColumnName.Split(',').First().Split('：').Last();
            switch (sourceStatus)
            {
                case "待发货":
                    orderStatus = OrderStatus.Confirmed;
                    break;
                case "待收货":
                    orderStatus = OrderStatus.Delivered;
                    break;
                case "待评价":
                case "已评价":
                    orderStatus = OrderStatus.Finished;
                    break;
            }

            for (int i = 1; i < excelTable.Rows.Count; i++)
            {
                var row = excelTable.Rows[i];

                var orderDateStr = Convert.ToString(row[0]); //订单创建时间
                var createdDate = DateTime.Parse(orderDateStr);

                var source = OrderSource.CIBAPP;
                var sourceDesc = Util.Helpers.Reflection.GetDescription<OrderSource>(OrderSource.CIBAPP);
                var sourceAccount = string.Empty;
                var sourceSN = Convert.ToString(row[1]); //订单号
                if (string.IsNullOrEmpty(sourceSN))
                    continue;

                //Added By: BingYi 20180728
                //CIBAPP目前包含两种支付方式: 本金支付/积分支付 
                //如果为 本金支付,该订单 应该改属于 [兴业分期商城]
                var paymentType = Convert.ToString(row[16]).Trim();
                if (paymentType.Equals("本金支付") || paymentType.Equals("分期支付"))
                {
                    source = OrderSource.CIBSTM;
                    sourceDesc = Util.Helpers.Reflection.GetDescription<OrderSource>(OrderSource.CIBSTM);
                }

                //订单SN=来源+原来的SN
                var orderSN = string.Format("{0}-{1}", source, sourceSN);
                using (var db = new OMSContext())
                {
                    var foo = db.OrderSet.Find(orderSN);
                    if (foo != null)
                    {
                        Util.Logs.Log.GetLog(nameof(CIBAPPExcelOrderOption)).Error($"订单{foo.OrderSn}已经存在");
                        continue;

                    }
                }
                var item = items.Find(o => o.OrderSn == orderSN);
                if (item == null)
                {
                    decimal unitPrice = 0M;
                    var productName = Convert.ToString(row[3]); //商品名称
                    var productsku = Convert.ToString(row[2]); //商品编号
                    if (productName.Equals("水清清特供稻花香精装礼盒2.5KG"))
                        unitPrice = 56.00M;
                    else if (productName.Equals("水清清冠军优选四季经典4KG*1提"))
                        unitPrice = 108.00M;

                    var quantity = Convert.ToInt32(row[4]); //数量
                    var productProps = Convert.ToString(row[5]); //商品属性
                    productName = string.Format("{0}-{1}", productName, productProps);

                    var consigneeName = Convert.ToString(row[6]); //收件人
                    var consigneePhone = Convert.ToString(row[7]); //联系电话
                    var consigneePhone2 = string.Empty;

                   
                    var consigneeProvince = string.Empty;
                    var consigneeCity = string.Empty;
                    var consigneeCounty = string.Empty;
                    var consigneeAddress = Convert.ToString(row[8]); //收货地区+详细地址
                    var consigneeZipCode = Convert.ToString(row[9]); //邮编

                    //
                    if (string.IsNullOrEmpty(consigneeProvince)
                        && string.IsNullOrEmpty(consigneeCity) && !string.IsNullOrEmpty(consigneeAddress))
                    {
                        var addrInfo = DistrictService.ResolveAddress(consigneeAddress);
                        consigneeProvince = addrInfo.Province;
                        consigneeCity = addrInfo.City;
                        consigneeCounty = addrInfo.County;
                        consigneeAddress = addrInfo.Address;
                    }

                    var totalAmount = 0;//?
                    var totalQuantity = quantity;
                    var totalPayment = 0;

                    //是否需要发票
                    var invoiceFlag = Convert.ToString(row[10]); //是否需要发票
                    var invoiceType = string.Empty;
                    var invoiceName = Convert.ToString(row[13]); //发票抬头
                    if (invoiceFlag.Equals("否"))
                        invoiceType = invoiceName = string.Empty;

                    var orderItem = new OrderEntity()
                    {
                        SourceSn = sourceSN,
                        Source = source,
                        SourceDesc = sourceDesc,
                        CreatedDate = createdDate,
                        OrderSn = orderSN,
                        Consignee = new CustomerEntity()
                        {
                            Name = consigneeName,
                            Phone = consigneePhone,
                            Phone2 = consigneePhone2
                        },
                        ConsigneeAddress = new AddressEntity()
                        {
                            Address = consigneeAddress,
                            City = consigneeCity,
                            County = consigneeCounty,
                            Province = consigneeProvince,
                            ZipCode = consigneeZipCode
                        },
                       
                     
                        Logistics = string.Empty,
                        LogisticsNo = string.Empty,
                        LogisticsPrice = 0M,

                        OrderStatus = (int)orderStatus,
                        OrderStatusDesc = Util.Helpers.Enum.GetDescription(typeof(OrderStatus), orderStatus),


                        Remarks = string.Empty
                    };
                    if (orderItem.Products == null)
                        orderItem.Products = new List<OrderProductInfo>();
                    using (var db = new OMSContext())
                    {
                        var foo = db.ProductsSet.Find(db.ProductDictionarySet.Find(productsku)?.ProductCode);
                        
                        decimal weight = foo==null?0:foo.QuantityPerUnit * quantity;
                        OrderProductInfo orderProductInfo = new OrderProductInfo()
                        {
                           
                            TotalAmount = totalAmount,
                            ProductCount = quantity,
                            ProductWeight = weight,
                            Source = source,
                            sku = productsku
                        };
                        orderItem.Products.Add(orderProductInfo);
                        items.Add(orderItem);
                    }
                    
                }
                else
                {
                    var productName = Convert.ToString(row[3]); //商品名称
                    var productsku = Convert.ToString(row[2]); //商品编号
                   

                    var quantity = Convert.ToInt32(row[4]); //数量

                    var totalAmount = 0 * quantity;

                    using (var db = new OMSContext())
                    {
                        var foo = db.ProductsSet.Find(db.ProductDictionarySet.Find(productsku)?.ProductCode);

                        decimal weight = foo == null ? 0 : foo.QuantityPerUnit * quantity;
                        OrderProductInfo orderProductInfo = new OrderProductInfo()
                        {
                            TotalAmount = totalAmount,
                            ProductCount = quantity,
                            ProductWeight = weight,
                            Source = source,
                            sku = productsku
                        };
                        item.Products.Add(orderProductInfo);
                    }

                    
                }
            }

            return items;
        }
    }
}
