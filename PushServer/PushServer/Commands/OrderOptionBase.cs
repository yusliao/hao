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
using M2.OrderManagement.Sync;
using PushServer.Service;
using System.Data.Entity;
using OMS.Models.DTO;

namespace PushServer.Commands
{
    public abstract class OrderOptionBase : IOrderOption
    {
        
        protected Util.Files.FileScanner FileScanner { get; set; } = new Util.Files.FileScanner();
        public abstract string Name { get; }
        public bool IsImporting { get; set; } = false;

        public abstract IClientConfig clientConfig { get; }
        public static event Action<List<OrderEntity>,OptionType> OnPostCompletedEventHandle;
        public static event Action<string> UIMessageEventHandle;
        protected abstract List<OrderEntity> FetchOrders();
        protected virtual void OnUIMessageEventHandle(string msg)
        {
            var handle = UIMessageEventHandle;
            if (handle != null)
                handle(msg);
        }
        /// <summary>
        /// 插入商品记录
        /// </summary>
        /// <param name="db"></param>
        /// <param name="orderDTO"></param>
        /// <param name="item"></param>
        protected void InsertOrUpdateProductInfo(OMSContext db, OrderDTO orderDTO,OrderEntity item)
        {
          
            var bar = db.ProductDictionarySet.FirstOrDefault(p => (p.ProductNameInPlatform == orderDTO.productName || p.ProductId == orderDTO.productsku) && p.ProductCode != null);
            if (bar == null)
            {
                OnUIMessageEventHandle($"订单文件：{orderDTO.fileName}中平台单号：{orderDTO.sourceSN}（{orderDTO.productsku}）录入失败");
                Util.Logs.Log.GetLog(nameof(CIBAPPExcelOrderOption)).Error($"订单文件：{orderDTO.fileName}中平台商品：{orderDTO.productName}未找到");
             

                if (bar == null)
                {
                    if (db.ProductDictionarySet.FirstOrDefault(p => p.ProductId == orderDTO.productsku) == null)
                    {
                        ProductDictionary productDictionary = new ProductDictionary()
                        {
                            ProductId = orderDTO.productsku,
                            ProductNameInPlatform = orderDTO.productName
                        };
                        db.ProductDictionarySet.Add(productDictionary);
                        db.SaveChanges();
                    }
                }
                return;
            }
            var foo = db.ProductsSet.Include(p => p.weightModel).FirstOrDefault(p => p.sku == bar.ProductCode);
            if (foo == null)
            {
                OnUIMessageEventHandle($"订单文件：{orderDTO.fileName}中平台单号：{orderDTO.sourceSN}（{orderDTO.productsku}）对应ERP商品记录未找到");
                Util.Logs.Log.GetLog(nameof(CIBAPPExcelOrderOption)).Error($"订单文件：{orderDTO.fileName}中平台商品名称：{orderDTO.productName}对应系统商品未找到");

                return;
            }

            decimal weight = foo == null ? 0 : foo.QuantityPerUnit * orderDTO.count;
            OrderProductInfo orderProductInfo = new OrderProductInfo()
            {
                ProductPlatId = orderDTO.productsku,
                ProductPlatName = orderDTO.productName,
                //   Warehouse = item.OrderLogistics.Logistics,
                MonthNum = orderDTO.createdDate.Month,
                weightCode = foo.weightModel == null ? 0 : foo.weightModel.Code,
                weightCodeDesc = foo.weightModel == null ? string.Empty : $"{foo.weightModel.Value}g",
                OrderSn = orderDTO.orderSN,
              //  TotalAmount = totalAmount,
                ProductCount = orderDTO.count,
                ProductWeight = weight,
                Source = orderDTO.source,
                sku = foo.sku
            };
            if (item.Products.FirstOrDefault(p => p.sku == foo.sku) == null)
            {
                item.Products.Add(orderProductInfo);
              
            }
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
                try
                {
                    FileScanner.ScannedFiles.ForEach(f =>
                    {
                        var temp = Path.GetExtension(f.Name);
                        File.Move(f.FullName, Path.ChangeExtension(f.FullName, $"{temp}.bak"));

                        // fileInfo.MoveTo($"{fileInfo.FullName}.bak");
                    });
                    FileScanner.ScannedFiles.Clear();
                }
                catch (Exception ex)
                {
                    Util.Logs.Log.GetLog(nameof(Name)).Error($"上传完毕，修改文件名后缀时出错。/r/n{ex.Message}");
                    
                }
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
                    item.OrderExtendInfo = new OrderExtendInfo()
                    {
                        DiscountFee = item.Products.Sum(p => p.DiscountFee),
                        IsPromotional = item.Products.Any(p => p.DiscountFee > 0) ? true : false,

                        OrderSn = item.OrderSn,
                        TotalAmount = item.Products.Sum(p => p.TotalAmount),
                        TotalProductCount = item.Products.Sum(p => p.ProductCount),
                        CreatedDate = item.CreatedDate.Date,
                       
                        TotalWeight = item.Products.Sum(p => p.ProductWeight)
                    };
                }
                
                db.BulkInsert<OrderExtendInfo>(lst.Select(o => o.OrderExtendInfo));
                stopwatch.Stop();
                Util.Logs.Log.GetLog(nameof(OrderOptionBase)).Info($"订单数量:{lst.Count} 批量插入订单扩展表耗时ms:{stopwatch.ElapsedMilliseconds}");
                try
                {
                   
                    stopwatch.Start();
                    db.BulkInsert<OrderEntity>(lst);
                    stopwatch.Stop();
                    Util.Logs.Log.GetLog(nameof(OrderOptionBase)).Info($"订单数量:{lst.Count} 批量插入订单表耗时ms:{stopwatch.ElapsedMilliseconds}");
                    stopwatch.Start();
                    db.Set<OrderProductInfo>().AddRange(lst.SelectMany<OrderEntity, OrderProductInfo>(o => o.Products));
                    
                    db.BulkInsert<OrderProductInfo>(lst.SelectMany(o => o.Products));
                    stopwatch.Stop();
                    Util.Logs.Log.GetLog(nameof(OrderOptionBase)).Info($"订单数量:{lst.Count} 批量插入订单商品表耗时ms:{stopwatch.ElapsedMilliseconds}");
                    stopwatch.Start();
                    db.Set<OrderLogisticsDetail>().AddRange(lst.SelectMany<OrderEntity, OrderLogisticsDetail>(o => o.OrderLogistics));

                    db.BulkInsert<OrderLogisticsDetail>(lst.SelectMany(o => o.OrderLogistics));
                    stopwatch.Stop();
                    Util.Logs.Log.GetLog(nameof(OrderOptionBase)).Info($"订单数量:{lst.Count} 批量插入订单物流表耗时ms:{stopwatch.ElapsedMilliseconds}");

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

        /// <summary>
        /// 推送报表
        /// </summary>
        /// <returns></returns>
        public bool PushReport()
        {
            /* 发送昨天的日报表
             * 如果昨天是周日，发送周报表
             * 如果昨天是当月最后一天，发送月报表 
             * 
             */

            StatisticServer.SendStatisticMessage(StatisticType.Day,DateTime.Now.AddDays(-1),clientConfig.Tag);
            if (DateTime.Now.DayOfWeek == DayOfWeek.Monday)
                StatisticServer.SendStatisticMessage(StatisticType.Week, DateTime.Now, clientConfig.Tag);
            if (DateTime.Now.Day == 1)
                StatisticServer.SendStatisticMessage(StatisticType.Month, DateTime.Now, clientConfig.Tag);
            return true;
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
        ErpExcel,//ERP导出单
        LogisticsExcel //银行回传单（物流单）
    }
}
