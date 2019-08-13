
using OMS.Models;
using PushServer.Configuration;
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
using Util.Files;
using Z.BulkOperations;
using Z.EntityFramework.Plus;
using static PushServer.Commands.OrderOptionBase;

namespace PushServer.Commands
{
    /// <summary>
    /// 积分盘点单录入
    /// </summary>
    [Export(typeof(IOrderOption))]
    
    class CIBJifenPandianOrderOption : IOrderOption
    {
        protected Util.Files.FileScanner FileScanner { get; set; } = new Util.Files.FileScanner();
        public  string Name => OMS.Models.OrderSource.CIBJifenPanDian;

        public  IClientConfig clientConfig => AppServer.Instance.ConfigDictionary[Name];

        protected  List<OrderPandianWithMonth> FetchOrders()
        {
            var ordersList = new List<OrderPandianWithMonth>();

            foreach (var file in this.GetExcelFiles())
            {
                using (var excel = new NPOIExcel(file.FullName))
                {
                    var table = excel.ExcelToDataTable(null, true);
                    var orderItems = this.ResolveOrders(table, file.FullName);
                    if (orderItems != null && orderItems.Any())
                        ordersList.AddRange(orderItems);

                }
            }
            return ordersList;
        }
        protected List<DataFileInfo> GetExcelFiles()
        {
            var excelFileList = new List<DataFileInfo>();

            FileScanner.ScanAllFiles(new DirectoryInfo(clientConfig.ExcelOrderFolder), "*.xls");
            if (FileScanner.ScannedFiles.Any())
            {
                FileScanner.ScannedFiles.ForEach(file =>
                {

                    var fileDate = DateTime.Now;

                    excelFileList.Add(new DataFileInfo(fileDate, file.Name, file.FullName));
                });
            }
            excelFileList = excelFileList.OrderBy(f => f.FileDate).ToList();

            return excelFileList;
        }
        /// <summary>
        /// 上传完毕
        /// </summary>
        /// <param name="postResult">上传成功与否</param>
        protected virtual void OnPostCompleted(bool postResult, List<OrderPandianWithMonth> lst)
        {
            if (postResult)
            {
                try
                {
                    FileScanner.ScannedFiles.ForEach(f =>
                    {
                        var temp = Path.GetExtension(f.Name);
                        File.Move(f.FullName, Path.ChangeExtension(f.FullName, $"{temp}.bak"));

                        // fileInfo.MoveTo($"{fileInfo.FullName}.bak");
                    });
                }
                catch (Exception ex)
                {
                    Util.Logs.Log.GetLog(nameof(Name)).Error($"上传完毕，修改文件名后缀时出错。/r/n{ex.Message}");
                    throw;
                }
               
                

            }
        }

        protected List<OrderPandianWithMonth> ResolveOrders(DataTable excelTable, string file)
        {
            var items = new List<OrderPandianWithMonth>();

           
            foreach (DataRow row in excelTable.Rows)
            {
                if (row["订单号"] == DBNull.Value
                   || row["商品名称"] == DBNull.Value
                   || row["数量"] == DBNull.Value)
                    continue;
                var sourceStatus = Convert.ToString(row["类型"]);
                OrderStatus orderStatus;
                switch (sourceStatus)
                {
                    case "购买":
                        orderStatus = OrderStatus.Finished;
                        break;
                    case "取消":
                        orderStatus = OrderStatus.Cancelled;
                        break;
                    default:
                        orderStatus = OrderStatus.Finished;
                        break;
                }

                var source = OrderSource.CIBJifenPanDian;
                var sourceDesc = Util.Helpers.Reflection.GetDescription<OrderSource>(OrderSource.CIBJifenPanDian);
                var sourceSN = Convert.ToString(row["订单号"]);

                var orderType = Convert.ToString(row["订单类型"]);
                var sOrderDate = Convert.ToString(row["交易日期"]);
                var createdDate = DateTime.Parse(sOrderDate);


                var productName = Convert.ToString(row["商品名称"]);
                var productsku = Convert.ToString(row["商品SKU编号"]);
                
                var quantity = Convert.ToInt32(row["数量"]);
               
                var totalAmount = Convert.ToDecimal(row["结算金额"]);
                var temp = row["商品规格"].ToString();
                int weightvalue = 0;
                if (temp.IndexOf("kg")!=0)
                {
                    temp = temp.Replace("kg", "").Replace("[","").Replace("]","").Replace("\"","");
                    decimal result;
                    if (decimal.TryParse(temp, out result))
                        weightvalue = (int)(result * 1000);
                    else
                        weightvalue = 0;
                }
                else if(temp.IndexOf("g")!=0)
                {
                    temp = temp.Replace("g", "").Replace("[", "").Replace("]", ""); 
                    int result;
                    if (int.TryParse(temp, out result))
                        weightvalue = result;
                    else
                        weightvalue = 0;
                }
               

                var item = items.Find(o => o.SourceSn == sourceSN);
                if (item == null)
                {
                    var orderItem = new OrderPandianWithMonth()
                    {
                        SourceSn = sourceSN,
                        Source = source,
                        SourceDesc = sourceDesc,
                        CreatedDate = createdDate,
                        OrderType = orderType,

                        OrderStatus = (int)orderStatus,
                        OrderStatusDesc = Util.Helpers.Enum.GetDescription(typeof(OrderStatus), orderStatus),

                        Remarks = string.Empty
                    };
                    if (orderItem.Products == null)
                        orderItem.Products = new List<OrderPandianProductInfo>();

                    using (var db = new OMSContext())
                    {
                        int weight=0;
                        var bar = db.ProductDictionarySet.FirstOrDefault(p => p.ProductNameInPlatform == productName);
                        if (bar == null || string.IsNullOrEmpty(bar.ProductCode))
                        {
                            Util.Logs.Log.GetLog(nameof(CIBVIPCSVOrderOption)).Error($"订单文件：{file}中平台商品：{productName}未找到");
                            weight = weightvalue * quantity;
                        }
                        else
                        {
                            var foo = db.ProductsSet.Include(p => p.weightModel).FirstOrDefault(p => p.sku == bar.ProductCode);
                            if (foo == null)
                            {
                                Util.Logs.Log.GetLog(nameof(CIBVIPCSVOrderOption)).Error($"订单文件：{file}中平台商品名称：{productName}对应系统商品未找到");
                                weight = weightvalue * quantity;
                            }
                            else
                            {
                                weight = foo.weightModel.Value * quantity;
                            }
                        }
                        if (orderStatus == OrderStatus.Cancelled)
                        {
                            quantity = -quantity;
                            weight = -weight;
                           
                        }
                        OrderPandianProductInfo orderProductInfo = new OrderPandianProductInfo()
                        {
                            
                            ProductPlatName = productName,
                            Year=createdDate.Year,
                            MonthNum = createdDate.Month,
                            TotalAmount = totalAmount,
                            ProductCount = quantity,
                            ProductWeight = weight,
                            Source = source,
                            SourceDesc = sourceDesc,
                            sku = productsku
                        };
                        orderItem.Products.Add(orderProductInfo);
                        items.Add(orderItem);
                    }
                   
                    

                }
                else
                {
                    using (var db = new OMSContext())
                    {
                        int weight = 0;
                        var bar = db.ProductDictionarySet.FirstOrDefault(p => p.ProductNameInPlatform == productName);
                        if (bar == null || string.IsNullOrEmpty(bar.ProductCode))
                        {
                            Util.Logs.Log.GetLog(nameof(CIBVIPCSVOrderOption)).Error($"订单文件：{file}中平台商品：{productName}未找到");
                            weight = weightvalue * quantity;
                        }
                        else
                        {
                            var foo = db.ProductsSet.Include(p => p.weightModel).FirstOrDefault(p => p.sku == bar.ProductCode);
                            if (foo == null)
                            {
                                Util.Logs.Log.GetLog(nameof(CIBVIPCSVOrderOption)).Error($"订单文件：{file}中平台商品名称：{productName}对应系统商品未找到");
                                weight = weightvalue * quantity;
                            }
                            else
                            {
                                weight = foo.weightModel.Value * quantity;
                            }
                        }
                        if (orderStatus == OrderStatus.Cancelled)
                        {
                            quantity = -quantity;
                            weight = -weight;

                        }
                        OrderPandianProductInfo orderProductInfo = new OrderPandianProductInfo()
                        {
                           
                            ProductPlatName = productName,
                           
                            MonthNum = createdDate.Month,
                            TotalAmount = totalAmount,
                            ProductCount = quantity,
                            ProductWeight = weight,
                            Source = source,
                            SourceDesc = sourceDesc,
                            sku = productsku
                        };
                        item.Products.Add(orderProductInfo);
                    }

                }
            }

            return items;
        }
        public  bool ImportToOMS()
        {
            //抽取订单，获取订单数据集
            var lst = FetchOrders();

            if (lst == null || !lst.Any())
            {
                OnPostCompleted(true, lst);
                return true;
            }
            using (var db = new OMSContext())
            {
                bool result = false;
               

                //try
                //{
                //    db.<OrderPandianWithMonth>(lst);
                //    result = true;

                //}
                //catch (Exception ex)
                //{
                //    result = false;
                //    Util.Logs.Log.GetLog(nameof(OrderOptionBase)).Error($"BulkInsert插入订单数据出错，content:{ex.Message}");
                //}
                if (!result)
                {
                    try
                    {

                        db.BulkInsert(lst);
                        db.Set<OrderPandianProductInfo>().AddRange(lst.SelectMany(o => o.Products));
                        db.BulkInsert<OrderPandianProductInfo>(lst.SelectMany(o => o.Products));
                        result = true;
                    }
                    catch (Exception ex)
                    {
                        Util.Logs.Log.GetLog(nameof(OrderOptionBase)).Error($"AddRange插入订单数据出错，content:{ex.Message}");
                        result = false;
                    }

                }

                OnPostCompleted(result, lst);

            }

            // OnPostCompleted(result.success);
            return true;
        }

      

        public bool PushReport()
        {
            throw new NotImplementedException();
        }

        public DataTable ExportExcel(List<OrderEntity> orders)
        {
            throw new NotImplementedException();
        }
    }
}
