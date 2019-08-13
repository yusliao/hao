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
        private readonly List<CustomerEntity> EmptyPackagePersonList = new List<CustomerEntity>();
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
        /// <summary>
        /// 生成ERP导出单
        /// </summary>
        /// <param name="obj"></param>
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
                            var foo = db.CustomStrategies.Include(c => c.Customer).FirstOrDefault(c => c.Customer.CustomerId == item.Consignee.CustomerId);
                            if(foo!=null&&Util.Helpers.Enum.Parse<CustomStrategyEnum>(foo.StrategyValue).HasFlag(CustomStrategyEnum.EmptyPackage))
                                dr["卖家备注"] = Util.Helpers.Enum.GetDescription<CustomStrategyEnum>(CustomStrategyEnum.EmptyPackage);
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
                        string pcode = csv.GetField<string>("商品代码").Trim();//ERP标识的商品编码
                        decimal weight = csv.GetField<string>("总重量").ToInt();
                        int count = csv.GetField<string>("数量").ToInt();

                        var p1 = order.Products.FirstOrDefault(p => p.sku == pcode);
                        if(p1!=null)//修改已有商品汇总信息
                        {
                            p1.Warehouse = warehouse;
                            p1.ProductWeight = weight;
                            p1.ProductCount = count;
                            
                        }
                        else//新增订单商品
                        {
                            OrderProductInfo orderProductInfo = new OrderProductInfo()
                            {
                                ProductPlatName = csv.GetField<string>("商品名称").Trim(),
                                ProductPlatId = pcode,
                                MonthNum = order.CreatedDate.Month,
                                Warehouse = warehouse,
                                
                                weightCode = csv.GetField<string>("规格代码").ToInt(),
                                weightCodeDesc = csv.GetField<string>("规格名称").Trim(),
                                OrderSn = order.OrderSn,
                                //  TotalAmount = csv.GetField<string>("规格名称"),
                                ProductCount = count,
                                ProductWeight = weight,
                                Source = order.Source,

                            };
                            db.OrderProductSet.Add(orderProductInfo);
                            order.Products.Add(orderProductInfo);
                            
                        }
                       
                        OrderLogisticsDetail orderLogisticsDetail = new OrderLogisticsDetail();
                        orderLogisticsDetail.Logistics = csv.GetField<string>("物流公司");
                        orderLogisticsDetail.LogisticsNo = csv.GetField<string>("物流单号").Trim();
                        orderLogisticsDetail.LogisticsPrice = csv.GetField<string>("物流费用").ToDecimalOrNull();
                        orderLogisticsDetail.PickingTime = DateTime.Parse(csv.GetField<string>("配货时间"));
                        //发货时间为空时，从配货时间中取日期作为发货时间
                        orderLogisticsDetail.SendingTime = csv.GetField<string>("发货时间") =="" ? orderLogisticsDetail.PickingTime.Value.Date : DateTime.Parse(csv.GetField<string>("发货时间"));
                        var templ = order.OrderLogistics.FirstOrDefault(o => o.LogisticsNo == orderLogisticsDetail.LogisticsNo);//是否已经存在该物流信息，防止重复添加
                        if (templ == null)
                        {
                            if (!string.IsNullOrEmpty(orderLogisticsDetail.LogisticsNo))//物流单号为空，不保存此信息
                            {
                                db.OrderLogisticsDetailSet.Add(orderLogisticsDetail);
                                order.OrderLogistics.Add(orderLogisticsDetail);
                            }
                            
                        }
                        db.SaveChanges();
                        items.Add(order);
                    }
                    else
                    {
                        Util.Logs.Log.GetLog(nameof(AppServer)).Error($"ERP导出单：{file}。该文件中订单编号：{sn}在OMS系统中不存在");
                        

                    }
                    if (Environment.UserInteractive)
                    {
                        Console.WriteLine($"ERP导出单：{file}。该文件中订单编号：{sn}解析完毕");
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
            var serverNames = instance.ConfigDictionary.Values.Where(i => i.Enabled == true).Select(i=>i.Name).ToArray();
            return StatisticServer.Instance.PushReport(serverNames);

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
        public static bool CreateReport(DateTime dateTime)
        {
            return StatisticServer.Instance.CreateReport(dateTime);
            
        }
        public static bool CreateHistoryReport(int month,int year)
        {
            return StatisticServer.Instance.CreateHistoryReport(month, year);

        }
        public static bool CreatePandianReport(int monthNum)
        {
           return StatisticServer.Instance.CreatePandianReport(monthNum);
            
        }


    }
}
