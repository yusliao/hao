using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PushServer.Configuration;
using OMS.Models;
using System.Data;

using System.IO;
using FusionStone.WeiXin;

using PushServer.Service;
using System.Data.Entity;
using OMS.Models.DTO;
using System.ComponentModel;
namespace PushServer.Commands
{
    public abstract class OrderOptionBase : IOrderOption
    {
        
        protected Util.Files.FileScanner FileScanner { get; set; } = new Util.Files.FileScanner();
        protected List<ExceptionOrder> exceptionOrders = new List<ExceptionOrder>();
        public abstract string Name { get; }
        public bool IsImporting { get; set; } = false;

        public abstract IClientConfig clientConfig { get; }
        public static event Action<ICollection<ExceptionOrder>> ExceptionMessageEventHandle;
        public static event Action<List<OrderEntity>,OptionType> OnPostCompletedEventHandle;
    
        public static event Action<string> UIMessageEventHandle;
        protected abstract List<OrderEntity> FetchOrders();
        protected virtual void OnUIMessageEventHandle(string msg)
        {
            Util.Logs.Log.GetLog(nameof(OrderOptionBase)).Debug(msg);
            var handle = UIMessageEventHandle;
            if (handle != null)
                handle.BeginInvoke(msg,null,null);
        }
        protected virtual void OnExceptionMessageEventHandle(List<ExceptionOrder> msgs)
        {
            if (msgs.Any())
            {
                
                ExceptionOrder[] exceptions = msgs.ToArray();
                var handle = ExceptionMessageEventHandle;
                if (handle != null)
                    handle.BeginInvoke(exceptions, null, null);
                msgs.Clear();
            }
        }
        /// <summary>
        /// 插入商品记录
        /// </summary>
        /// <param name="db"></param>
        /// <param name="orderDTO"></param>
        /// <param name="item"></param>
        protected virtual bool InputProductInfoWithoutSaveChange(OMSContext db, OrderDTO orderDTO,OrderEntity item)
        {
            ProductDictionary pd = null;
            switch (item.Source)
            {
                case OrderSource.CIB:
                case OrderSource.CIBAPP:
                case OrderSource.CIBEVT:
                case OrderSource.CIBSTM:
                    pd = db.ProductDictionarySet.FirstOrDefault(p => p.ProductId.Trim() == orderDTO.productsku.Trim() && orderDTO.productsku != null&& p.ProductCode != null);
                    if (pd == null)
                    {
                        OnUIMessageEventHandle($"订单文件：{orderDTO.fileName}中平台单号：{orderDTO.sourceSN}（{orderDTO.productsku}）录入失败");
                        InputExceptionOrder(orderDTO,ExceptionType.ProductIdUnKnown);
                        if (db.ProductDictionarySet.FirstOrDefault(p => p.ProductId == orderDTO.productsku) == null)
                        {
                            ProductDictionary productDictionary = new ProductDictionary()
                            {
                                ProductId = orderDTO.productsku,
                                Source = orderDTO.source,
                                ProductNameInPlatform = orderDTO.productName.Trim()
                            };
                            db.ProductDictionarySet.Add(productDictionary);
                            
                            db.SaveChanges();
                        }
                        return false;
                    }
                    break;
                case OrderSource.CMBC:
                case OrderSource.CIBVIP:
                case OrderSource.ICBC_JINWEN:
                case OrderSource.BANK_JINWEN:
                case OrderSource.XUNXIAO:
                case OrderSource.JINGDONG:
                case OrderSource.ICIB:
                    pd = db.ProductDictionarySet.FirstOrDefault(p => p.ProductNameInPlatform.Trim() == orderDTO.productName.Trim() && orderDTO.productName != null  && p.ProductCode != null);
                    if (pd == null)
                    {
                        OnUIMessageEventHandle($"订单文件：{orderDTO.fileName}中平台单号：{orderDTO.sourceSN}（{orderDTO.productName}）录入失败");
                        InputExceptionOrder(orderDTO,ExceptionType.ProductNameUnKnown);
                        if (db.ProductDictionarySet.FirstOrDefault(p => p.ProductNameInPlatform == orderDTO.productName) == null)
                        {
                            ProductDictionary productDictionary = new ProductDictionary()
                            {
                                ProductId = orderDTO.productsku,
                                Source = orderDTO.source,
                                ProductNameInPlatform = orderDTO.productName.Trim()
                            };
                            db.ProductDictionarySet.Add(productDictionary);
                            db.SaveChanges();
                        }
                        return false;
                    }
                    break;
    
                default:
                    pd = db.ProductDictionarySet.FirstOrDefault(p => p.ProductId.Trim() == orderDTO.productsku.Trim() && orderDTO.productsku != null && p.ProductCode != null);
                    if (pd == null)
                    {
                        OnUIMessageEventHandle($"订单文件：{orderDTO.fileName}中平台单号：{orderDTO.sourceSN}（{orderDTO.productsku}）录入失败");
                        InputExceptionOrder(orderDTO, ExceptionType.ProductIdUnKnown);
                        if (db.ProductDictionarySet.FirstOrDefault(p => p.ProductId == orderDTO.productsku) == null)
                        {
                            ProductDictionary productDictionary = new ProductDictionary()
                            {
                                ProductId = orderDTO.productsku,
                                Source = orderDTO.source,
                                ProductNameInPlatform = orderDTO.productName.Trim()
                            };
                            db.ProductDictionarySet.Add(productDictionary);
                            int i = db.SaveChanges();
                        }
                        return false;
                    }
                    break;
            }


            // var foo = db.ProductsSet.Include(p => p.weightModel).FirstOrDefault(p => p.sku.Trim() == "S0010040003\t".Trim());
            string temp = pd.ProductCode.Trim();//"S0010040003\t"
            var foo = db.ProductsSet.Include(p => p.weightModel).FirstOrDefault(p => p.sku.Trim() == temp);
            if (foo == null)
            {
                OnUIMessageEventHandle($"订单文件：{orderDTO.fileName}中平台单号：{orderDTO.sourceSN}（{orderDTO.productsku}）对应ERP商品记录未找到");
                InputExceptionOrder(orderDTO, ExceptionType.ProductCodeUnKnown);
                return false;
            }
            if (item.Products.FirstOrDefault(p => p.sku == foo.sku) == null)
            {
                decimal weight = foo == null ? 0 : foo.QuantityPerUnit * orderDTO.count;
                decimal totalAmount = pd.PayPrice * orderDTO.count;
                decimal totalCostPrice = foo.CostPrice * orderDTO.count;
                decimal totalFlatAmount = foo.FlatPrice * orderDTO.count;
                OrderProductInfo orderProductInfo = new OrderProductInfo()
                {
                    ProductPlatId = orderDTO.productsku,
                    ProductPlatName = orderDTO.productName.Trim(),
                    //   Warehouse = item.OrderLogistics.Logistics,
                    MonthNum = orderDTO.createdDate.Month,
                    weightCode = foo.weightModel == null ? 0 : foo.weightModel.Code,
                    weightCodeDesc = foo.weightModel == null ? string.Empty : $"{foo.weightModel.Value}g",
                    OrderSn = orderDTO.orderSN,
                    TotalAmount = totalAmount,
                    TotalCostPrice = totalCostPrice,
                    TotalFlatAmount = totalFlatAmount,
                    ProductCount = orderDTO.count,
                    ProductWeight = weight,
                    Source = orderDTO.source,
                    sku = foo.sku
                };
                item.Products.Add(orderProductInfo);
            }
           
           

            OnUIMessageEventHandle($"订单文件：{orderDTO.fileName}中平台单号：{orderDTO.sourceSN}（{orderDTO.productsku}）解析完毕");
            return true;
        }
        protected virtual bool InputProductInfoWithSaveChange(OMSContext db,OrderDTO orderDTO, OrderEntity item,string oldordersn=null)
        {
            ProductDictionary pd = null;
            // 验证该商品是否在ERP系统中有存根。没有存根就停止录入
            switch (item.Source)
            {
                case OrderSource.CIB:
                case OrderSource.CIBAPP:
                case OrderSource.CIBEVT:
                case OrderSource.CIBSTM:
                    pd = db.ProductDictionarySet.FirstOrDefault(p => p.ProductId.Trim() == orderDTO.productsku.Trim() && orderDTO.productsku != null && p.ProductCode != null);
                    if (pd == null)
                    {
                        OnUIMessageEventHandle($"订单文件：{orderDTO.fileName}中平台单号：{orderDTO.sourceSN}（{orderDTO.productsku}）录入失败");
                        InputExceptionOrder(orderDTO, ExceptionType.ProductIdUnKnown);
                        if (db.ProductDictionarySet.FirstOrDefault(p => p.ProductId == orderDTO.productsku) == null)
                        {
                            ProductDictionary productDictionary = new ProductDictionary()
                            {
                                ProductId = orderDTO.productsku,
                                Source = orderDTO.source,
                                ProductNameInPlatform = orderDTO.productName.Trim()
                            };
                            db.ProductDictionarySet.Add(productDictionary);
                            db.SaveChanges();
                        }
                        return false;
                    }
                    break;
                case OrderSource.CMBC:
                case OrderSource.ICBC_JINWEN:
                case OrderSource.BANK_JINWEN:
                case OrderSource.XUNXIAO:
                case OrderSource.JINGDONG:
                case OrderSource.CIBVIP: //根据商品名称查找对应关系
                    pd = db.ProductDictionarySet.FirstOrDefault(p => p.ProductNameInPlatform.Trim() == orderDTO.productName.Trim() && orderDTO.productName != null && p.ProductCode != null);
                    if (pd == null)
                    {
                        OnUIMessageEventHandle($"订单文件：{orderDTO.fileName}中平台单号：{orderDTO.sourceSN}（{orderDTO.productName}）录入失败");
                        InputExceptionOrder(orderDTO, ExceptionType.ProductNameUnKnown);
                        if (db.ProductDictionarySet.FirstOrDefault(p => p.ProductNameInPlatform == orderDTO.productName) == null)
                        {
                            ProductDictionary productDictionary = new ProductDictionary()
                            {
                                ProductId = orderDTO.productsku,
                                Source = orderDTO.source,
                                ProductNameInPlatform = orderDTO.productName.Trim()
                            };
                            db.ProductDictionarySet.Add(productDictionary);
                            db.SaveChanges();
                        }
                        return false;
                    }
                    break;

                default:
                    pd = db.ProductDictionarySet.FirstOrDefault(p => p.ProductId.Trim() == orderDTO.productsku.Trim() && orderDTO.productsku != null && p.ProductCode != null);
                    if (pd == null)
                    {
                        OnUIMessageEventHandle($"订单文件：{orderDTO.fileName}中平台单号：{orderDTO.sourceSN}（{orderDTO.productsku}）录入失败");
                        InputExceptionOrder(orderDTO, ExceptionType.ProductIdUnKnown);
                        if (db.ProductDictionarySet.FirstOrDefault(p => p.ProductId == orderDTO.productsku) == null)
                        {
                            ProductDictionary productDictionary = new ProductDictionary()
                            {
                                ProductId = orderDTO.productsku,
                                Source = orderDTO.source,
                                ProductNameInPlatform = orderDTO.productName.Trim()
                            };
                            db.ProductDictionarySet.Add(productDictionary);
                            db.SaveChanges();
                        }
                        return false;
                    }
                    break;
            }


            //判断该商品的ERP编号是否存在，不存在则停止录入
            string temp = pd.ProductCode.Trim();//"S0010040003\t"
            var foo = db.ProductsSet.Include(p => p.weightModel).FirstOrDefault(p => p.sku.Trim() == temp);
            if (foo == null)
            {
                OnUIMessageEventHandle($"订单文件：{orderDTO.fileName}中平台单号：{orderDTO.sourceSN}（{orderDTO.productsku}）对应ERP商品记录未找到");
                InputExceptionOrder(orderDTO, ExceptionType.ProductCodeUnKnown);
                return false;
            }
            
            if (item.Products.FirstOrDefault(p => p.sku == foo.sku) == null)
            {
                decimal weight = foo == null ? 0 : foo.QuantityPerUnit * orderDTO.count;
                decimal totalAmount = pd.PayPrice * orderDTO.count;
                decimal totalCostPrice = foo.CostPrice * orderDTO.count;
                decimal totalFlatAmount = foo.FlatPrice * orderDTO.count;
                OrderProductInfo orderProductInfo = new OrderProductInfo()
                {
                    ProductPlatId = orderDTO.productsku,
                    ProductPlatName = orderDTO.productName,
                    //   Warehouse = item.OrderLogistics.Logistics,
                    MonthNum = orderDTO.createdDate.Month,
                    weightCode = foo.weightModel == null ? 0 : foo.weightModel.Code,
                    weightCodeDesc = foo.weightModel == null ? string.Empty : $"{foo.weightModel.Value}g",
                    OrderSn = oldordersn?? orderDTO.orderSN,
                    TotalAmount = totalAmount,
                    TotalCostPrice = totalCostPrice,
                    TotalFlatAmount = totalFlatAmount,
                    ProductCount = orderDTO.count,
                    ProductWeight = weight,
                    Source = orderDTO.source,
                    sku = foo.sku
                };
                item.Products.Add(orderProductInfo);
                OnUIMessageEventHandle($"订单文件：{orderDTO.fileName}中平台单号：{orderDTO.sourceSN}（{orderDTO.productsku}）解析完毕");
            }
            else
            {
                OnUIMessageEventHandle($"订单文件：{orderDTO.fileName}中平台单号：{orderDTO.sourceSN}（{orderDTO.productsku}）重复录入");
            }
            db.SaveChanges();

          
            return true;
            
           
        }
        /// <summary>
        /// 检查订单是否已经存在于数据库中，如果存在了就更新商品信息。
        /// </summary>
        /// <param name="orderDTO"></param>
        /// <returns></returns>
        protected virtual bool CheckOrderInDataBase(OrderDTO orderDTO)
        {
            /*检查订单是否已经存在在数据库中
             * 如果订单不存在于数据库中，返回false,结束
             * 如果订单存在于数据库中：
             * 检查订单是否取消订单，如果是取消订单，则减去相应商品的数量和重量信息；
             * 如果不是取消清单，则录入该订单商品
             */ 
            using (var db = new OMSContext())
            {
                #region 订单编号已经过渡完毕，该业务算法废弃
                //var foo = db.OrderSet.Include(o => o.Products).FirstOrDefault(o => o.OrderSn == orderDTO.orderSN_old);//订单在数据库中
                //if (foo != null)//系统中已经存在该订单
                //{
                //    //取消订单只存在于兴业积点渠道，这个渠道的商品没有商品编号
                //    if (orderDTO.orderStatus == OrderStatus.Cancelled)//是否取消订单
                //    {

                //        var bar = db.ProductDictionarySet.FirstOrDefault(x => x.ProductNameInPlatform.Trim() == orderDTO.productName.Trim());
                //        if (bar != null && !string.IsNullOrEmpty(bar.ProductCode))
                //        {
                //            var p1 = db.ProductsSet.Include(x => x.weightModel).FirstOrDefault(x => x.sku == bar.ProductCode);
                //            if (p1 != null)
                //            {
                //                decimal weight = foo == null ? 0 : p1.QuantityPerUnit * orderDTO.count;
                //                var p = foo.Products.FirstOrDefault(o => o.sku == p1.sku);
                //                if (p != null)
                //                {

                //                    p.ProductCount -= orderDTO.count;
                //                    p.ProductWeight -= weight;

                //                    db.SaveChanges();
                //                    return true;
                //                }
                //            }
                //        }

                //    }
                //    else
                //    {
                //        InputProductInfoWithSaveChange(db, orderDTO, foo, orderDTO.orderSN_old);

                //    }
                //    return true;
                //}
                //else
                //{
                //    var foo1 = db.OrderSet.Include(o => o.Products).FirstOrDefault(o => o.OrderSn == orderDTO.orderSN);//订单在数据库中
                //    if (foo1 != null)//系统中已经存在该订单
                //    {
                //        //取消订单只存在于兴业积点渠道，这个渠道的商品没有商品编号
                //        if (orderDTO.orderStatus == OrderStatus.Cancelled)//是否取消订单
                //        {

                //            var bar = db.ProductDictionarySet.FirstOrDefault(x => x.ProductNameInPlatform.Trim() == orderDTO.productName.Trim());
                //            if (bar != null && !string.IsNullOrEmpty(bar.ProductCode))
                //            {
                //                var p1 = db.ProductsSet.Include(x => x.weightModel).FirstOrDefault(x => x.sku == bar.ProductCode);
                //                if (p1 != null)
                //                {
                //                    decimal weight = foo1 == null ? 0 : p1.QuantityPerUnit * orderDTO.count;
                //                    var p = foo1.Products.FirstOrDefault(o => o.sku == p1.sku);
                //                    if (p != null)
                //                    {

                //                        p.ProductCount -= orderDTO.count;
                //                        p.ProductWeight -= weight;

                //                        db.SaveChanges();

                //                    }
                //                }
                //            }
                //        }
                //        else
                //        {
                //            InputProductInfoWithSaveChange(db, orderDTO, foo1);

                //        }
                //        return true;

                //    }
                //    else
                //        return false;
                //}
                #endregion
                var foo1 = db.OrderSet.Include(o => o.Products).FirstOrDefault(o => o.OrderSn == orderDTO.orderSN);//订单在数据库中
                if (foo1 != null)//系统中已经存在该订单
                {
                    //取消订单只存在于兴业积点渠道，这个渠道的商品没有商品编号
                    if (orderDTO.orderStatus == OrderStatus.Cancelled)//是否取消订单
                    {

                        var bar = db.ProductDictionarySet.FirstOrDefault(x => x.ProductNameInPlatform.Trim() == orderDTO.productName.Trim());
                        if (bar != null && !string.IsNullOrEmpty(bar.ProductCode))
                        {
                            var p1 = db.ProductsSet.Include(x => x.weightModel).FirstOrDefault(x => x.sku == bar.ProductCode);
                            if (p1 != null)
                            {
                                decimal weight = foo1 == null ? 0 : p1.QuantityPerUnit * orderDTO.count;
                                var p = foo1.Products.FirstOrDefault(o => o.sku == p1.sku);
                                if (p != null)
                                {

                                    p.ProductCount -= orderDTO.count;
                                    p.ProductWeight -= weight;

                                    db.SaveChanges();

                                }
                            }
                        }
                    }
                    else
                    {
                        InputProductInfoWithSaveChange(db, orderDTO, foo1);

                    }
                    return true;

                }
                else
                    return false;
            }
        }
        protected void InputExceptionOrder(OrderDTO orderItem,ExceptionType exceptionType)
        {
            ExceptionOrder exceptionOrder = new ExceptionOrder()
            {
                OrderFileName = orderItem.fileName,
                OrderInfo = Util.Helpers.Json.ToJson(orderItem),
                Source = this.Name,
                SourceSn = orderItem.sourceSN,
                CreateTime = DateTime.Now,
                ErrorCode = exceptionType,
                ErrorMessage = Util.Helpers.Enum.GetDescription<ExceptionType>(exceptionType)

            };
            exceptionOrders.Add(exceptionOrder);
        }
        /// <summary>
        /// 入库完毕
        /// </summary>
        /// <param name="postResult">入库结果</param>
        /// <param name="lst">订单集合</param>
        /// <param name="optionType" cref="OptionType">生成操作</param>
        protected virtual void OnPostCompleted(bool postResult,List<OrderEntity> lst,OptionType optionType= OptionType.ErpExcel)
        {
            if (postResult)
            {
              
                    FileScanner.ScannedFiles.ForEach(f =>
                    {
                        try
                        {
                            /*
                            var temp = Path.GetExtension(f.Name);
                            File.Move(f.FullName, Path.ChangeExtension($"{f.FullName}", $"{temp}_{DateTime.Now.ToString("yyyyMMdd")}.bak"));
                            */
                            string newfilename = $"{f.FullName}.bak";
                            Util.Files.Paths.IndexPathGenerator indexPathGenerator = new Util.Files.Paths.IndexPathGenerator(new FileInfo(f.FullName).DirectoryName);
                            newfilename = indexPathGenerator.GetNewFileName(newfilename, 1);
                            File.Move(f.FullName, newfilename);

                        }
                        catch (Exception ex)
                        {
                            Util.Logs.Log.GetLog(nameof(Name)).Error($"上传完毕，修改文件名后缀时出错,文件名：{f.Name}。/r/n{ex.Message}");

                        }
                        // fileInfo.MoveTo($"{fileInfo.FullName}.bak");
                    });
                FileScanner.ScannedFiles.Clear();
                FileScanner.ScannedFolders?.Clear();
               
                if(OnPostCompletedEventHandle!=null)
                {
                    var handle = OnPostCompletedEventHandle;
                    handle.BeginInvoke(lst, optionType, null,null);
                    
                }
               


            }
        }
       


        public abstract DataTable ExportExcel(List<OrderEntity> orders);
       
        public virtual  bool ImportToOMS()
        {
            if (IsImporting)
                return true;
            IsImporting = true;
            bool result = false; 
            try
            {
                //抽取订单，获取订单数据集
                var lst = FetchOrders();
                OnUIMessageEventHandle($"{this.Name}准备入库：当前订单数{lst.Count}");
                if (lst == null || !lst.Any())
                {
                    OnPostCompleted(true, lst);
                    result = true;
                }
                else
                {
                    result = InsertDB(lst);
                    OnPostCompleted(result, lst);
                }
                
                OnExceptionMessageEventHandle(exceptionOrders);
                return result;
            }
            finally
            {
                IsImporting = false;
            }
           
            
        }

        protected virtual bool InsertDB(List<OrderEntity> lst)
        {
            using (var db = new OMSContext())
            {
                System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
                var adc = db.Configuration.AutoDetectChangesEnabled;
                stopwatch.Start();
                foreach (var item in lst.AsParallel())
                {
                    
                    item.OrderExtendInfo = new OrderExtendInfo();
                    item.OrderExtendInfo.DiscountFee = item.Products.Sum(p => p.DiscountFee);
                    item.OrderExtendInfo.IsPromotional = item.Products.Any(p => p.DiscountFee > 0) ? true : false;
                    item.OrderExtendInfo.TotalAmount = item.Products.Sum(p => p.TotalAmount);
                    item.OrderExtendInfo.TotalCostPrice = item.Products.Sum(p => p.TotalCostPrice);
                    item.OrderExtendInfo.TotalFlatAmount = item.Products.Sum(p => p.TotalFlatAmount);

                    item.OrderExtendInfo.OrderSn = item.OrderSn;
                    item.OrderExtendInfo.TotalAmount = item.Products.Sum(p => p.TotalAmount);
                    item.OrderExtendInfo.TotalProductCount = item.Products.Sum(p => p.ProductCount);
                    item.OrderExtendInfo.CreatedDate = item.CreatedDate.Date;

                    item.OrderExtendInfo.TotalWeight = item.Products.Sum(p => p.ProductWeight);
                    
                }
                
                db.BulkInsert<OrderExtendInfo>(lst.Select(o => o.OrderExtendInfo));
              //  db.SaveChanges();
                stopwatch.Stop();
                OnUIMessageEventHandle($"订单数量:{lst.Count} 批量插入【订单扩展表】耗时ms:{stopwatch.ElapsedMilliseconds}");
                
                try
                {
                   
                    stopwatch.Start();
                   
                    db.BulkInsert<OrderEntity>(lst);
                    stopwatch.Stop();
                    OnUIMessageEventHandle($"订单数量:{lst.Count} 批量插入【订单表】耗时ms:{stopwatch.ElapsedMilliseconds}");

                    if(exceptionOrders.Any())
                        db.BulkInsert<ExceptionOrder>(exceptionOrders);

                    stopwatch.Start();
                    db.Set<OrderProductInfo>().AddRange(lst.SelectMany<OrderEntity, OrderProductInfo>(o => o.Products));
                    
                    db.BulkInsert<OrderProductInfo>(lst.SelectMany(o => o.Products));
                    stopwatch.Stop();
                    OnUIMessageEventHandle($"订单数量:{lst.Count} 批量插入【订单商品表】耗时ms:{stopwatch.ElapsedMilliseconds}");
                 
                    stopwatch.Start();
               
                    var templst = lst.SelectMany(o => o.OrderLogistics??new List<OrderLogisticsDetail>());
                    if (templst!=null&&templst.Any())
                    {
                        db.Set<OrderLogisticsDetail>().AddRange(lst.SelectMany<OrderEntity, OrderLogisticsDetail>(o => o.OrderLogistics));
                        db.BulkInsert<OrderLogisticsDetail>(lst.SelectMany(o => o.OrderLogistics));
                        stopwatch.Stop();
                        OnUIMessageEventHandle($"订单数量:{lst.Count} 批量插入【订单物流表】耗时ms:{stopwatch.ElapsedMilliseconds}");
                       
                    }
                    return true;
                }
                catch (Exception ex)
                {
                   
                    Util.Logs.Log.GetLog(nameof(OrderOptionBase)).Error($"BulkInsert插入订单数据出错，订单数量:{lst.Count} ,content:{ex.Message}");
                    return false;
                }
                finally
                {
                    db.Configuration.AutoDetectChangesEnabled = adc;
                }
               
               
            }
        }
        public void Dispose()
        {
            FileScanner.ScannedFiles.Clear();
        }
        

      

        public class DataFileInfo
        {
            public DataFileInfo(DateTime fileDate, string fileName, string fullName)
            {
                this.FileDate = fileDate;
                this.FileName = fileName;
                this.FullName = fullName;
            }

            public DateTime FileDate { get; set; }
            public string FileName { get; set; }
            public string FullName { get; set; }
        }
    }
    public enum OptionType
    {
        None =0,
        [Description("ERP导入单")]
        ErpExcel,//ERP导入单
        [Description("银行回传单")]
        LogisticsExcel, //银行回传单（物流单）
        [Description("异常订单")]
        ExceptionExcel   //异常订单
    }
}
