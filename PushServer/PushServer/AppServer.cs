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
using System.Globalization;
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
        private ERPExcelOrderOption ERP { get; set; } = new ERPExcelOrderOption();

        public static AppServer Instance
        {
            get { return instance; }
        }

      

        private AppServer()
        {
            var section = System.Configuration.ConfigurationManager.GetSection("OrderSource") as Configuration.ClientListSection;
            foreach (var item in section.Clients)
            {
                ConfigDictionary.Add(item.Name, item);
            }
            OrderOptionBase.OnPostCompletedEventHandle += OrderOptionBase_OnPostCompletedEventHandle;
            OrderOptionBase.UIMessageEventHandle += OrderOptionBase_OnUIMessageEventHandle;
            OrderOptionBase.ExceptionMessageEventHandle += OrderOptionBase_ExceptionMessageEventHandle1; ;

            StatisticServer.Instance.ShowMessageEventHandle += OrderOptionBase_OnUIMessageEventHandle;
            
            #region MEF配置
            MyComposePart();
            #endregion
        }

        private void OrderOptionBase_ExceptionMessageEventHandle1(ICollection<ExceptionOrder> obj)
        {
            if (obj != null && obj.Any())
            {
                ERP.ExportExcel<ExceptionOrder>(OptionType.ExceptionExcel, obj.ToList());
                var glst = obj.GroupBy(e => e.ErrorCode);
                foreach (var item in glst)
                {
                    WxPushNews.SendErrorText($"错误类型：{Util.Helpers.Enum.GetDescription<ExceptionType>(item.Key)},数量：{item.Count()}");
                }
              
            }
            else
            {

            }
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
        private void OrderOptionBase_OnPostCompletedEventHandle(List<OMS.Models.OrderEntity> obj,OptionType optionType)
        {
            if (obj != null&&obj.Any())
            {
                ERP.ExportExcel<OrderEntity>(optionType, obj);
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

            return instance.ERP.ImportErpToOMS();
        }
        public  void ExportLogisticsInfo(List<OrderEntity> lst,string filepath)
        {
            var glst = lst.GroupBy(o => o.Source).ToList();
            DataTable cib_dt = new DataTable();
            cib_dt.Columns.Add("订单号");
            cib_dt.Columns.Add("物流编号");
            cib_dt.Columns.Add("物流单号");
            int taskcount = glst.Count;
            foreach (var item in glst)
            {
                var option = AppServer.Instance.OrderOptSet.FirstOrDefault(i => i.clientConfig.Name == item.Key);
                if (option != null)
                {
                    System.Threading.ThreadPool.QueueUserWorkItem(o =>
                    {
                        var dt = option.ExportExcel(item.ToList());
                        if (option.clientConfig.Name == OrderSource.CIB || option.clientConfig.Name == OrderSource.CIBAPP)
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
                                string filename = Path.Combine(filepath, $"ERP-{item.Key}回传订单{DateTime.Now.ToString("yyyyMMddHHmmss")}.xlsx");
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
            while (taskcount > 0)
            {
                System.Threading.Thread.Sleep(1000);
            }
            if (cib_dt.Rows.Count > 0)
            {
                string filename = Path.Combine(filepath, $"ERP-兴业银行积分回传订单{DateTime.Now.ToString("yyyyMMddHHmmss")}.xlsx");
                NPOIExcel.Export(cib_dt, filename);
                if (Environment.UserInteractive)
                {
                    Console.WriteLine($"ERP-兴业银行积分回传订单生成成功。文件名:{filename}");
                }
            }

            
        }


        public  static bool PushReport(DateTime dateTime)
        {
            var serverNames = instance.ConfigDictionary.Values.Where(i => i.Enabled == true).Select(i=>i.Name).ToArray();
            return StatisticServer.Instance.PushReport(serverNames, dateTime);

        }
        /// <summary>
        /// 发布盘点报告
        /// </summary>
        /// <param name="monthNum">月份</param>
        /// <param name="ordersource">订单来源</param>
        /// <returns></returns>
        public  bool PushPandianReport(int monthNum)
        {
            return StatisticServer.Instance.PushPandianReport(monthNum, instance.ERP.clientConfig.ExcelOrderFolder);
         
           
            
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
