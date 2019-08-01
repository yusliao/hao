using CsvHelper;
using OMS.Models;
using PushServer.Commands;
using PushServer.Configuration;
using PushServer.Service;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Data;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Util;
using Util.Files;

namespace PushServer
{
    public class AppServer
    {
        [ImportMany(typeof(IOrderOption))]
        private IEnumerable<IOrderOption> OrderOptSet { get; set; }
       


        /// <summary>
        /// 平台信息配置字典
        /// </summary>
        public readonly Dictionary<string, Configuration.IClientConfig> ConfigDictionary = new Dictionary<string, Configuration.IClientConfig>();
        private static readonly AppServer instance = new AppServer();
        protected Util.Files.FileScanner FileScanner { get; set; } = new Util.Files.FileScanner();
        public static AppServer Instance
        {
            get { return instance; }
        }

        public  string Name => OrderSource.ERP;

        public  IClientConfig clientConfig => ConfigDictionary[Name];

        private AppServer()
        {
            var section = System.Configuration.ConfigurationManager.GetSection("OrderSource") as Configuration.ClientListSection;
            foreach (var item in section.Clients)
            {
                ConfigDictionary.Add(item.Name, item);
            }
            OrderOptionBase.OnPostCompletedEventHandle += OrderOptionBase_OnPostCompletedEventHandle;
            OrderOptionBase.UIMessageEventHandle += OrderOptionBase_OnUIMessageEventHandle;
            #region MEF配置
            MyComposePart();
            #endregion
        }

        private void OrderOptionBase_OnUIMessageEventHandle(string obj)
        {
            if (Environment.UserInteractive)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(obj);
            }
        }

        private void OrderOptionBase_OnPostCompletedEventHandle(List<OMS.Models.OrderEntity> obj)
        {
            if (obj != null&&obj.Any())
            {
                DataTable dt = new DataTable();
                #region create columns


                dt.Columns.Add("店铺");
                dt.Columns.Add("订单编号");
                dt.Columns.Add("买家会员");
                dt.Columns.Add("支付金额");
                dt.Columns.Add("商品名称");
                dt.Columns.Add("商品代码");
                dt.Columns.Add("规格代码");
                dt.Columns.Add("规格名称");
                dt.Columns.Add("是否赠品");
                dt.Columns.Add("数量");
                dt.Columns.Add("价格");
                dt.Columns.Add("商品备注");
                dt.Columns.Add("运费");
                dt.Columns.Add("买家留言");
                dt.Columns.Add("收货人");
                dt.Columns.Add("联系电话");
                dt.Columns.Add("联系手机");
                dt.Columns.Add("收货地址");
                dt.Columns.Add("省");
                dt.Columns.Add("市");
                dt.Columns.Add("区");
                dt.Columns.Add("邮编");
                dt.Columns.Add("订单创建时间");
                dt.Columns.Add("订单付款时间");
                dt.Columns.Add("发货时间");
                dt.Columns.Add("物流单号");
                dt.Columns.Add("物流公司");
                dt.Columns.Add("卖家备注");
                dt.Columns.Add("发票种类");
                dt.Columns.Add("发票类型");
                dt.Columns.Add("发票抬头");
                dt.Columns.Add("纳税人识别号");
                dt.Columns.Add("开户行");
                dt.Columns.Add("账号");

                dt.Columns.Add("地址");
                dt.Columns.Add("电话");
                dt.Columns.Add("是否手机订单");
                dt.Columns.Add("是否货到付款");
                dt.Columns.Add("支付方式");
                dt.Columns.Add("支付交易号");
                dt.Columns.Add("真实姓名");
                dt.Columns.Add("身份证号");
                dt.Columns.Add("仓库名称");
                dt.Columns.Add("预计发货时间");
                dt.Columns.Add("预计送达时间");
                dt.Columns.Add("订单类型");
                dt.Columns.Add("是否分销商订单");
                #endregion

                using (var db = new OMSContext())
                {
                    foreach (var item in obj)
                    {
                        foreach (var productInfo in item.Products)
                        {
                            var dr = dt.NewRow();
                            dr["店铺"] = item.SourceDesc;
                            dr["订单编号"] = item.SourceSn;
                            dr["买家会员"] = item.Customer == null ? item.Consignee.Name : item.Customer.Name;
                            dr["商品名称"] = productInfo.ProductPlatName;
                            dr["商品代码"] = productInfo.sku;
                            dr["规格代码"] = productInfo.weightCode==0?string.Empty:productInfo.weightCode.ToString();
                            dr["数量"] = productInfo.ProductCount.ToString();
                            dr["价格"] = productInfo.TotalAmount.ToString();
                            dr["商品备注"] = item.Remarks;
                            dr["收货人"] = item.Consignee.Name;
                            dr["联系手机"] = item.Consignee.Phone ?? item.Consignee.Phone2;
                            dr["收货地址"] = item.ConsigneeAddress.Address;
                            dr["省"] = item.ConsigneeAddress.Province;
                            dr["市"] = item.ConsigneeAddress.City;
                            dr["区"] = item.ConsigneeAddress.County;
                            dr["邮编"] = item.ConsigneeAddress.ZipCode;
                          //  dr["物流公司"] = item.OrderLogistics.Logistics;
                            //dr["仓库名称"] = productInfo.Warehouse;
                            dt.Rows.Add(dr);

                        }

                    }

                }
                var filename = System.IO.Path.Combine(clientConfig.ExcelOrderFolder,"import",$"ERP-{obj[0].Source}导入订单{DateTime.Now.ToString("yyyyMMddHHmmss")}.xlsx");
                NPOIExcel.Export(dt, filename);
                if(Environment.UserInteractive)
                {
                    Console.WriteLine($"ERP-{obj[0].Source}导入订单生成成功。文件名:{filename}");
                }

            }
            else
            {
                //if (Environment.UserInteractive)
                //{
                //    var commcolor = Console.ForegroundColor;

                //    Console.ForegroundColor = ConsoleColor.Green;
                //    Console.WriteLine($"没有检测到新订单数据。ERP-{obj[0].Source}导入订单生成失败");
                //    Console.ForegroundColor = commcolor;
                //}
            }

        }

        void MyComposePart()
        {
            var catalog = new AssemblyCatalog(Assembly.GetExecutingAssembly());
            var container = new CompositionContainer(catalog);
            //将部件（part）和宿主程序添加到组合容器
            container.ComposeParts(this);
        }
        public  static bool ImportToOMS()
        {
            var lst = AppServer.Instance.OrderOptSet.ToList();
            foreach (var item in lst)
            {
                System.Threading.ThreadPool.QueueUserWorkItem(o =>
                {
                    try
                    {
                        if(Environment.UserInteractive)
                        {
                            Console.ForegroundColor = ConsoleColor.White;
                            Console.WriteLine($"{item.clientConfig.Tag}开始导入".PadRight(30,'>'));
                        }
                        item.ImportToOMS();
                        if (Environment.UserInteractive)
                        {
                            var commcolor = Console.ForegroundColor;
                           
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.WriteLine($"{item.clientConfig.Tag}导入完毕".PadRight(30, '<'));
                            Console.ForegroundColor = commcolor;
                        }
                    }
                    catch (Exception ex)
                    {
                        if (Environment.UserInteractive)
                        {
                            var commcolor = Console.ForegroundColor;

                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine($"{item.clientConfig.Tag}导入失败,msg:{ex.Message}");
                            Console.ForegroundColor = commcolor;
                        }
                        Util.Logs.Log.GetLog(nameof(AppServer)).Error($"{item.clientConfig.Tag}导入订单失败.\r\n{ex.Message}\r\n{ex.StackTrace}");
                        
                    }
                  
                });
                
            }
            return true;
        }
        public static bool ImportErpToOMS()
        {

            var lst = instance.FetchOrders();
            if (lst != null && lst.Any())
            {
                if(Environment.UserInteractive)
                {
                    Console.WriteLine($"正在生成ERP回传物流订单,订单数量：{lst.Count}");
                }
                instance.OnPostCompleted(true, lst);
                return true;
            }
            else
                return false;
        }
        protected  void OnPostCompleted(bool postResult, List<OrderEntity> lst)
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
                    Util.Logs.Log.GetLog(nameof(Name)).Error($"修改文件名后缀时出错。/r/n{ex.Message}");

                }
                var glst = lst.GroupBy(o => o.Source).ToList();
                DataTable cib_dt = new DataTable();
                cib_dt.Columns.Add("订单号");
                cib_dt.Columns.Add("物流编号");
                cib_dt.Columns.Add("物流单号");
                int taskcount = glst.Count;
                foreach (var item in glst)
                {
                    var option = AppServer.Instance.OrderOptSet.FirstOrDefault(i=>i.clientConfig.Name==item.Key);
                    if (option != null)
                    {
                        System.Threading.ThreadPool.QueueUserWorkItem(o =>
                        {
                            var dt = option.ExportExcel(item.ToList());
                            if (option.clientConfig.Name == OrderSource.CIB|| option.clientConfig.Name == OrderSource.CIBAPP)
                            {
                                if (dt != null)
                                {
                                    cib_dt.Merge(dt);
                                }
                            }
                            else
                            {
                                if (dt != null)
                                {
                                    string filename = Path.Combine(clientConfig.ExcelOrderFolder, "logistics", $"ERP-{item.Key}回传订单{DateTime.Now.ToString("yyyyMMddHHmmss")}.xlsx");
                                    NPOIExcel.Export(dt, filename);
                                    if (Environment.UserInteractive)
                                    {
                                        Console.WriteLine($"ERP-{item.Key}回传订单生成成功。文件名:{filename}");
                                    }
                                }
                            }
                            taskcount--;
                        });
                    }
                }
                while (taskcount>0)
                {
                    System.Threading.Thread.Sleep(1000);
                }
                if(cib_dt.Rows.Count>0)
                {
                    string filename = Path.Combine(clientConfig.ExcelOrderFolder, "logistics", $"ERP-兴业银行积分回传订单{DateTime.Now.ToString("yyyyMMddHHmmss")}.xlsx");
                    NPOIExcel.Export(cib_dt, filename);
                    if(Environment.UserInteractive)
                    {
                        Console.WriteLine($"ERP-兴业银行积分回传订单生成成功。文件名:{filename}");
                    }
                }

            }
        }
        protected List<OrderEntity> FetchOrders()
        {
            var ordersList = new List<OrderEntity>();

            foreach (var file in this.GetExcelFiles())
            {
                if(Environment.UserInteractive)
                {
                    Console.WriteLine($"正在解析ERP导出单文件：{file.FullName}");
                }
                using (var csv = new CsvReader(new StreamReader(file.FullName, Encoding.Default)))
                {
                    ResolveOrders(csv, file.FullName, ordersList);
                }
            }
            return ordersList;
        }
        protected List<FileInfo> GetExcelFiles()
        {
           

            FileScanner.ScanAllFiles(new DirectoryInfo(Path.Combine(clientConfig.ExcelOrderFolder, "export")), "*.csv");
            if (FileScanner.ScannedFiles.Any())
            {
                return FileScanner.ScannedFiles;
            }
            else
                return new List<FileInfo>();
        }
        protected List<OrderEntity> ResolveOrders(CsvReader csv, string file, List<OrderEntity> items)
        {
            csv.Read();
            csv.ReadHeader();
            while (csv.Read())
            {
                using (var db = new OMSContext())
                {
                   
                    string sn = csv.GetField<string>("平台单号").Trim();
                    var order = db.OrderSet.Include(o=>o.OrderLogistics).Include(o=>o.Products).FirstOrDefault(o => o.SourceSn == sn);
                    if (order != null)
                    {
                        string warehouse = csv.GetField<string>("仓库名称");
                        string sku = csv.GetField<string>("商品代码");
                        decimal weight = csv.GetField<string>("总重量").ToInt();
                        int count = csv.GetField<string>("数量").ToInt();
                        var p1 = order.Products.FirstOrDefault(p => p.sku == sku);
                        if(p1!=null)
                        {
                            p1.Warehouse = warehouse;
                            p1.ProductWeight = weight;
                            p1.ProductCount = count;
                            
                        }
                        OrderLogisticsDetail orderLogisticsDetail = new OrderLogisticsDetail();
                        orderLogisticsDetail.Logistics = csv.GetField<string>("物流公司");
                        orderLogisticsDetail.LogisticsNo = csv.GetField<string>("物流单号");
                        orderLogisticsDetail.LogisticsPrice = csv.GetField<string>("物流费用").ToDecimalOrNull();
                        orderLogisticsDetail.PickingTime = DateTime.Parse(csv.GetField<string>("配货时间"));
                        //发货时间为空时，从配货时间中取日期作为发货时间
                        orderLogisticsDetail.SendingTime = csv.GetField<string>("发货时间") =="" ? orderLogisticsDetail.PickingTime.Value.Date : DateTime.Parse(csv.GetField<string>("发货时间"));
                        order.OrderLogistics.Add(orderLogisticsDetail);
                        db.OrderLogisticsDetailSet.Add(orderLogisticsDetail);
                        db.SaveChanges();
                        items.Add(order);
                    }
                    else
                    {
                        Util.Logs.Log.GetLog(nameof(AppServer)).Error($"ERP导出单：{file}。该文件中订单编号：{sn}在OMS系统中不存在");
                        if(Environment.UserInteractive)
                        {
                            Console.WriteLine($"ERP导出单：{file}。该文件中订单编号：{sn}在OMS系统中不存在");
                        }
                    }
                }
            }
            return items;

        }
        public  static bool ExportExcel()
        {
            //var lst = AppServer.Instance.OrderOptSet.ToList();
            //foreach (var item in lst)
            //{
            //    System.Threading.ThreadPool.QueueUserWorkItem(o =>
            //    {
            //        item.ExportExcel();
            //    });
            //}
            return true;
        }
        public  static bool PushReport()
        {
            StatisticServer.SendStatisticMessage(StatisticType.Day, DateTime.Now.AddDays(-1));
            if (DateTime.Now.DayOfWeek == DayOfWeek.Monday)
                StatisticServer.SendStatisticMessage(StatisticType.Week, DateTime.Now);
            if (DateTime.Now.Day == 1)
                StatisticServer.SendStatisticMessage(StatisticType.Month, DateTime.Now);
            return true;
        }
        /// <summary>
        /// 发布盘点报告
        /// </summary>
        /// <param name="monthNum">月份</param>
        /// <param name="ordersource">订单来源</param>
        /// <returns></returns>
        public  bool PushPandianReport(int monthNum)
        {
            return StatisticServer.Instance.PushPandianReport(monthNum, clientConfig.ExcelOrderFolder);
         
           
            
        }
        public static bool CreateReport(DateTime dateTime,bool isAll =false)
        {
            return StatisticServer.Instance.CreateReport(dateTime, isAll);
            
        }
        public static bool CreatePandianReport(int monthNum)
        {
           return StatisticServer.Instance.CreatePandianReport(monthNum);
            
        }


    }
}
