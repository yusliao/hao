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

        internal static bool CreateYearReport(int year)
        {
            return StatisticServer.Instance.CreateYearReport(year);
        }

        private static readonly AppServer instance =  new AppServer();
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
        /// 导出报表
        /// </summary>
        /// <param name="obj"></param>
        private void OrderOptionBase_OnPostCompletedEventHandle(List<OMS.Models.OrderEntity> obj,OptionType optionType)
        {
           
            if (obj != null && obj.Any())
            {
                try
                {
                    ERP.ExportExcel<OrderEntity>(optionType, obj);
                }
                catch (Exception ex)
                {
                    Util.Logs.Log.GetLog(nameof(AppServer)).Error($"{obj.First().SourceDesc}-{Util.Helpers.Enum.GetDescription<OptionType>(optionType)}生成失败.\r\n{ex.Message}\r\n{ex.StackTrace}");

                }


            }
            else
            {
                //if (Environment.UserInteractive)
                //{
                //    var commcolor = Console.ForegroundColor;

                //    Console.ForegroundColor = ConsoleColor.Green;
                 //   Console.WriteLine($"没有检测到新订单数据。ERP-{obj[0].Source}导入订单生成失败");
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
                        item.Dispose();
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

            return Instance.ERP.ImportErpToOMS();
        }
        public static bool ImportOMSToERP()
        {

            return Instance.ERP.ImportOMSToERP();
        }

        /// <summary>
        /// 日，周，月同时发，用于CMD模式
        /// </summary>
        /// <param name="dateTime"></param>
        /// <returns></returns>
        public  static bool PushReport(DateTime dateTime,int reportType)
        {
            var serverNames = Instance.ConfigDictionary.Values.Where(i => i.Enabled == true).Select(i=>i.Name).ToArray();
            return StatisticServer.Instance.PushReport(serverNames, dateTime,Util.Helpers.Enum.Parse<StatisticType>(reportType));

        }
        public static bool PushDailyReport(DateTime dateTime)
        {
            var serverNames = Instance.ConfigDictionary.Values.Where(i => i.Enabled == true).Select(i => i.Name).ToArray();
            return StatisticServer.Instance.PushDailyReport(serverNames, dateTime);

        }
        public static bool PushWeeklyReport(DateTime dateTime)
        {
            var serverNames = Instance.ConfigDictionary.Values.Where(i => i.Enabled == true).Select(i => i.Name).ToArray();
            return StatisticServer.Instance.PushWeeklyReport(serverNames, dateTime);

        }
        public static bool PushMonthlyReport(DateTime dateTime)
        {
            var serverNames = Instance.ConfigDictionary.Values.Where(i => i.Enabled == true).Select(i => i.Name).ToArray();
            return StatisticServer.Instance.PushMonthlyReport(serverNames, dateTime);

        }
        /// <summary>
        /// 发布盘点报告
        /// </summary>
        /// <param name="monthNum">月份</param>
        /// <param name="ordersource">订单来源</param>
        /// <returns></returns>
        public  bool PushPandianReport(int monthNum)
        {
            return StatisticServer.Instance.PushPandianReport(monthNum, Instance.ERP.clientConfig.ExcelOrderFolder);
         
           
            
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
