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
    class CIBEVTExcelOrderOption : OrderOptionBase
    {
        public override string Name => OMS.Models.OrderSource.CIBEVT;

        public override IClientConfig clientConfig => AppServer.Instance.ConfigDictionary[Name];

        protected override List<OrderEntity> FetchOrders()
        {
            var ordersList = new List<OrderEntity>();

            foreach (var file in this.GetExcelFiles())
            {
                using (var excel = new NPOIExcel(file.FullName))
                {
                    var table = excel.ExcelToDataTable(null, true);
                    var orderItems = this.ResolveOrders(table, file);

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

        

        private List<OrderEntity> ResolveOrders(DataTable excelTable, DataFileInfo file)
        {
            var items = new List<OrderEntity>();

            var source = OrderSource.CIBEVT;
            var createdDate = file.FileDate;
            var orderStatus = OrderStatus.Confirmed;
            var sourceStatus = string.Empty;
            var sourceAccount = string.Empty;

            var nameParts = file.FileName.Split('_');
            var productParts = nameParts[1].Split('+');
            var productName = productParts.First();//商品名称
            var quantity = Convert.ToInt32(productParts.Last().Replace("盒", "")); //数量

            var totalAmount = 0;//?
            var totalQuantity = quantity;
            var totalPayment = 0;

            //是否需要发票
            var invoiceType = string.Empty;
            var invoiceName = string.Empty;

            var remarks = nameParts.First();

            for (int i = 0; i < excelTable.Rows.Count; i++)
            {
                var row = excelTable.Rows[i];

                //订单号,虚拟订单号
                var sourceSN = string.Format("{0}-{1}{2}", source, createdDate.ToString("yyyyMMdd"), i.ToString("D6"));

                var consigneeName = Convert.ToString(row[0]); //收件人
                var consigneePhone = Convert.ToString(row[1]); //联系电话
                var consigneePhone2 = string.Empty;

               
              
                var consigneeProvince = string.Empty;
                var consigneeCity = string.Empty;
                var consigneeCounty = string.Empty;
                var consigneeAddress = Convert.ToString(row[2]); //收货地区+详细地址
                var consigneeZipCode = string.Empty;

                if (consigneeAddress.Contains("|"))
                    consigneeAddress = consigneeAddress.Replace("|", "");

                if (string.IsNullOrEmpty(consigneeProvince)
                    && string.IsNullOrEmpty(consigneeCity) && !string.IsNullOrEmpty(consigneeAddress))
                {
                    var addrInfo = DistrictService.ResolveAddress(consigneeAddress);
                    consigneeProvince = addrInfo.Province;
                    consigneeCity = addrInfo.City;
                    consigneeCounty = addrInfo.County;
                    consigneeAddress = addrInfo.Address;
                }


              
                var orderItem = new OrderEntity()
                {
                    SourceSn = sourceSN,
                    Source = source,
                    SourceDesc = Util.Helpers.Reflection.GetDescription<OrderSource>(OrderSource.CIBEVT),
                    CreatedDate = createdDate,
                    OrderSn = sourceSN,
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
                    var bar = db.ProductDictionarySet.FirstOrDefault(p=>p.ProductNameInPlatform==productName);
                    if(bar == null||string.IsNullOrEmpty(bar.ProductCode))
                    {
                        Util.Logs.Log.GetLog(nameof(CIBEVTExcelOrderOption)).Error($"订单文件：{file.FullName}中平台商品：{productName}未找到");
                        return null;
                    }
                    var foo = db.ProductsSet.Find(bar.ProductCode);
                    if (foo == null)
                    {
                        Util.Logs.Log.GetLog(nameof(CIBEVTExcelOrderOption)).Error($"订单文件：{file.FullName}中平台商品名称：{productName}对应系统商品未找到");
                        return null;
                    }
                    decimal weight = foo == null ? 0 : foo.QuantityPerUnit * quantity;
                    OrderProductInfo orderProductInfo = new OrderProductInfo()
                    {

                        TotalAmount = totalAmount,
                        ProductCount = quantity,
                        ProductWeight = weight,
                        Source = source,
                        sku = foo.sku
                    };
                    orderItem.Products.Add(orderProductInfo);
                    items.Add(orderItem);
                }

                items.Add(orderItem);
            }

            return items;
        }
    }
}
