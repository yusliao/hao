using FusionStone.WeiXin;
using M2.OrderManagement.Sync;
using OMS.Models;

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Data.Entity;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
namespace PushServer.Service
{
    public class StatisticServer
    {
        [ImportMany(typeof(IProductStatisticServer))]
        private IEnumerable<IProductStatisticServer> ProductStatisticServerOptSet { get; set; }
        [ImportMany(typeof(IDistrictStatisticServer))]
        private IEnumerable<IDistrictStatisticServer> DistrictStatisticServerOptSet { get; set; }
        [ImportMany(typeof(IOrderStatisticServer))]
        private IEnumerable<IOrderStatisticServer> OrderStatisticServerOptSet { get; set; }
        [ImportMany(typeof(IPandianServer))]
        private IEnumerable<IPandianServer> PandianStatisticServerOptSet { get; set; }
        private static readonly StatisticServer statisticServer = new StatisticServer();
        public static StatisticServer Instance { get { return statisticServer; } }
      
        private StatisticServer()
        {
            #region MEF配置
            MyComposePart();
            #endregion
        }
        void MyComposePart()
        {
            var catalog = new AssemblyCatalog(Assembly.GetExecutingAssembly());
            var container = new CompositionContainer(catalog);
            //将部件（part）和宿主程序添加到组合容器
            container.ComposeParts(this);
        }
        /// <summary>
        /// 发送报表消息
        /// </summary>
        /// <param name="statisticType"></param>
        /// <param name="OrderSource"></param>
        public static void SendStatisticMessage(StatisticType statisticType, DateTime dateTime, string OrderSource = "所有订单")
        {
            var wxTargets = System.Configuration.ConfigurationManager.AppSettings["WxNewsTargets"].Split(new char[] { ',' }).ToList();
            var WxNewsUrl = System.Configuration.ConfigurationManager.AppSettings["WxNewsUrl"];
            var WxNewsPicUrl = System.Configuration.ConfigurationManager.AppSettings["WxNewsPicUrl"];

            var redirectUri = string.Format("{0}?date={1}&mode=day&source={2}", WxNewsUrl, DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd"), "ALL");

            redirectUri = System.Web.HttpUtility.UrlEncode(redirectUri);

            var url = WxPushNews.CreateWxNewsOAuthUrl(redirectUri);
            var picUrl = WxNewsPicUrl;
            using (var db = new OMSContext())
            {
                Statistic foo = new Statistic();
                string title = string.Empty;
                switch (statisticType)
                {
                    case StatisticType.Day:

                        var daynum = dateTime.DayOfYear;
                        foo = db.StatisticSet.Where(s => s.StatisticType == (int)StatisticType.Day && s.StatisticValue == daynum&&s.Year==dateTime.Year).FirstOrDefault();

                        if (foo == null || foo.TotalOrderCount <= 0)
                            title = string.Format("{0} #{1}#{2}(今日无单）", dateTime.ToString("yyyy年MM月dd日"), OrderSource, Environment.NewLine);
                        else
                            title = string.Format("{0} #{1}#", dateTime.ToString("yyyy年MM月dd日"), OrderSource);
                        break;
                    case StatisticType.Week:
                        int week = Util.Helpers.Time.GetWeekNum(DateTime.Now);
                        if (week > 1)
                            week -= 1;//发送上周的报表
                        foo = db.StatisticSet.Where(s => s.StatisticType == (int)StatisticType.Week && s.StatisticValue == week).FirstOrDefault();
                        if (foo == null || foo.TotalOrderCount <= 0)
                            title = string.Format("{0} #{1}#{2}(今日无单）", dateTime.ToString("yyyy年MM月dd日"), OrderSource, Environment.NewLine);
                        else
                            title = $"{DateTime.Now.Year}年#{week}周#{OrderSource}";
                        break;
                    case StatisticType.Month:
                        int month = DateTime.Now.Month;
                        if (month > 1)
                            month -= 1;//发送上个月的报表
                        foo = db.StatisticSet.Where(s => s.StatisticType == (int)StatisticType.Month && s.StatisticValue == month).FirstOrDefault();
                        if (foo == null || foo.TotalOrderCount <= 0)
                            title = string.Format("{0} #{1}#{2}(今日无单）", dateTime.ToString("yyyy年MM月dd日"), OrderSource, Environment.NewLine);
                        else
                            title = $"{DateTime.Now.Year}年#{month}月份#{OrderSource}";
                        break;
                    case StatisticType.Quarter:
                        int quarter = Util.Helpers.Time.GetSeasonNum(DateTime.Now);
                        foo = db.StatisticSet.Where(s => s.StatisticType == (int)StatisticType.Quarter && s.StatisticValue == quarter).FirstOrDefault();
                        if (foo == null || foo.TotalOrderCount <= 0)
                            title = string.Format("{0} #{1}#{2}(今日无单）", dateTime.ToString("yyyy年MM月dd日"), OrderSource, Environment.NewLine);
                        else
                            title = $"{DateTime.Now.Year}年#{quarter}季度#{OrderSource}";
                        break;
                    case StatisticType.Year:
                        
                        foo = db.StatisticSet.Where(s => s.StatisticType == (int)StatisticType.Month && s.StatisticValue == DateTime.Now.Year).FirstOrDefault();
                        if (foo == null || foo.TotalOrderCount <= 0)
                            title = string.Format("{0} #{1}#{2}(今日无单）", dateTime.ToString("yyyy年MM月dd日"), OrderSource, Environment.NewLine);
                        else
                            title = $"{DateTime.Now.Year}年#{OrderSource}";
                        break;
                    default:
                        break;
                }

               
                var wxArticles = new List<WxArticle>()
                {
                    new WxArticle( title,url,picUrl,string.Empty)
                };

                if (foo!=null&&foo.TotalOrderCount > 0)
                    wxArticles.AddRange(new List<WxArticle>()//上限为七
                {
                    new WxArticle(string.Format("总计单数：{0}", foo.TotalOrderCount),url,string.Empty,string.Empty),
                    new WxArticle(string.Format("总计盒数：{0}", foo.TotalProductCount),url,string.Empty,string.Empty),
                    new WxArticle(string.Format("总计人数：{0}", foo.TotalCustomer),url,string.Empty,string.Empty),
                    new WxArticle(string.Format("总计重量(kg)：{0}", foo.TotalWeight/1000),url,string.Empty,string.Empty),
                   // new WxArticle(string.Format("总计促销单数：{0}", foo.PromotionalOrderCount),url,string.Empty,string.Empty),
                    new WxArticle(string.Format("总计复购人数：{0}", foo.TotalCustomerRepurchase),url,string.Empty,string.Empty),
                    new WxArticle(string.Format("总计人数复购率：{0}%", Math.Round((double)foo.TotalCustomerRepurchase*100/foo.TotalCustomer,2)),url,string.Empty,string.Empty),
                    new WxArticle(string.Format("总计单数复购率：{0}%",Math.Round((double)foo.TotalOrderRepurchase*100/foo.TotalOrderCount,2)),url,string.Empty,string.Empty),

                });
                try
                {
                    WxPushNews.OrderStatistic(wxArticles);
                    Util.Logs.Log.GetLog(nameof(WxPushNews)).Info($"消息推送成功：{wxArticles[0].Title}");
                }
                catch (Exception ex)
                {
                    Util.Logs.Log.GetLog(nameof(WxPushNews)).Error($"消息推送失败：{wxArticles[0].Title}，error:{ex.Message},stackTrace：{ex.StackTrace}");
                    
                }
               
            }
        }
        /// <summary>
        /// 生成日报表
        /// </summary>
        public  void CreateDailyReport(DateTime dateTime)
        {
            /*生成昨日报表
             */
            ThreadPool.QueueUserWorkItem((o) =>
            {
                foreach (var item in ProductStatisticServerOptSet)
                {
                    var result = item.CreateDailyReport(dateTime);
                }
            });
            ThreadPool.QueueUserWorkItem((o) =>
            {
                foreach (var item in OrderStatisticServerOptSet)
                {
                    var result = item.CreateDailyReport(dateTime);
                }
            });
            ThreadPool.QueueUserWorkItem((o) =>
            {
                foreach (var item in DistrictStatisticServerOptSet)
                {
                    var result = item.CreateDistrictDailyReport(dateTime);
                }
            });
        }
        public  void CreateWeekReport(int weeknum,int year)
        {
            ThreadPool.QueueUserWorkItem((o) =>
            {
                foreach (var item in ProductStatisticServerOptSet)
                {
                    var result = item.CreateWeekReport(weeknum,year);
                }
            });
            ThreadPool.QueueUserWorkItem((o) =>
            {
                foreach (var item in OrderStatisticServerOptSet)
                {
                    var result = item.CreateWeekReport(weeknum, year);
                }
            });
            ThreadPool.QueueUserWorkItem((o) =>
            {
                foreach (var item in DistrictStatisticServerOptSet)
                {
                    var result = item.CreateDistrictWeekReport(weeknum, year);
                }
            });
        }
        /// <summary>
        /// 创建月订单报表
        /// </summary>
        /// <param name="monthnum"></param>
        public void CreateMonthReport(int monthnum,int year )
        {
            ThreadPool.QueueUserWorkItem((o) =>
            {
                foreach (var item in ProductStatisticServerOptSet)
                {
                    var result = item.CreateMonthReport(monthnum, year);
                }
            });
            ThreadPool.QueueUserWorkItem((o) =>
            {
                foreach (var item in OrderStatisticServerOptSet)
                {
                    var result = item.CreateMonthReport(monthnum, year);
                }
            });
            ThreadPool.QueueUserWorkItem((o) =>
            {
                foreach (var item in DistrictStatisticServerOptSet)
                {
                    var result = item.CreateDistrictMonthReport(monthnum, year);
                }
            });


        }
        /// <summary>
        /// 创建报表 
        /// </summary>
        /// <param name="dt"></param>
        /// <param name="isAll">true:重新生成当月所有的报表（维度：天，周，月）</param>
        /// <returns></returns>
        public  bool CreateReport(DateTime dt)
        {
            /*报表生成规则：
             * 今天生成昨天的日报表
             * 生成指定日期对应的周报表
             * 指定时间是当月月末的日期，则生成当月月报表
             */ 
            try
            {
                
                CreateDailyReport(dt);

                Util.Logs.Log.GetLog(nameof(StatisticServer)).Info($"{dt.ToString("yyyyMMdd HH:mm:ss")}日报表创建完毕");

                CreateWeekReport(Util.Helpers.Time.GetWeekNum(dt), dt.Year);
                Util.Logs.Log.GetLog(nameof(StatisticServer)).Info($"{Util.Helpers.Time.GetWeekNum(dt)}周报表创建完毕");
                
                var temp = dt.AddDays(1).Day;

                if (temp == 1)//明天是下月第一天
                {
                    CreateMonthReport(dt.Month, dt.Year);//生成当前月报表
                    Util.Logs.Log.GetLog(nameof(StatisticServer)).Info($"{dt.Month}月报表创建完毕");
                }
                
                return true;
            }
            catch (Exception ex)
            {
                Util.Logs.Log.GetLog(nameof(Statistic)).Error($"报表生成失败。/r/n{ex.Message}");
                return false;
            }
           
        }
        public bool CreateHistoryReport(int month,int year)
        {
            /*报表生成规则：
             * 今天生成昨天的日报表
             * 生成指定日期对应的周报表
             * 指定时间是当月月末的日期，则生成当月月报表
             */
            try
            {
                int end = new DateTime(year, month+1, 1).AddDays(-1).Day;
                for (int i = 0; i < end; i++)
                {
                    var foo = new DateTime(year, month, i + 1);
                    CreateDailyReport(foo);
                    if (foo.DayOfWeek == DayOfWeek.Sunday)
                    {
                        CreateWeekReport(Util.Helpers.Time.GetWeekNum(foo), foo.Year);
                        Util.Logs.Log.GetLog(nameof(StatisticServer)).Info($"{year}-{Util.Helpers.Time.GetWeekNum(foo)}周报表创建完毕");
                    }

                }
               
                CreateMonthReport(month, year);//生成当前月报表
                Util.Logs.Log.GetLog(nameof(StatisticServer)).Info($"{year}-{month}月报表创建完毕");
                

                return true;
            }
            catch (Exception ex)
            {
                Util.Logs.Log.GetLog(nameof(Statistic)).Error($"历史报表{year}年{month}月生成失败。/r/n{ex.Message}");
                return false;
            }

        }

        public  bool PushPandianReport(int monthNum,string pandianFolder)
        {
            var lst = Instance.PandianStatisticServerOptSet.ToList();
            foreach (var item in lst)
            {

                System.Threading.ThreadPool.QueueUserWorkItem(o =>
                {
                    var dt = item.PushPandianReport(monthNum, DateTime.Now.Year);
                    if (dt != null && dt.Rows.Count > 0)
                    {
                        var filename = System.IO.Path.Combine(pandianFolder, "pandian", $"ERP-{item.ServerName}-{monthNum}月份盘点订单{DateTime.Now.ToString("yyyyMMddHHmmss")}.xlsx");
                        NPOIExcel.Export(dt, filename);
                        if (Environment.UserInteractive)
                        {
                            Console.WriteLine($"ERP-{item.ServerName}-{monthNum}月份盘点订单生成成功。文件名:{filename}");
                        }
                    }
                });
            }
                //按渠道生成对账单
            var prolst = Instance.ProductStatisticServerOptSet.ToList();
            foreach (var pro in prolst)
            {

                System.Threading.ThreadPool.QueueUserWorkItem(o =>
                {
                    var dt = pro.PushMonthReport(monthNum, DateTime.Now.Year);
                    if (dt != null && dt.Rows.Count > 0)
                    {
                        var filename = System.IO.Path.Combine(pandianFolder, "pandian", $"ERP-{pro.ServerName}-{monthNum}月份盘点订单{DateTime.Now.ToString("yyyyMMddHHmmss")}.xlsx");
                        NPOIExcel.Export(dt, filename);
                        if (Environment.UserInteractive)
                        {
                            Console.WriteLine($"ERP-{pro.ServerName}-{monthNum}月份盘点订单生成成功。文件名:{filename}");
                        }
                    }
                });

            }


            return true;
        }
        /// <summary>
        /// 报表推送
        /// </summary>
        /// <param name="serverNames">可推送的条目列表</param>
        /// <returns></returns>
        public bool PushReport(string[] serverNames)
        {
            /*推送报表规则
             * 推送OrderSource对象指定的条目的推送报表
             * 推送昨天的日报表
             * 如果今天是周一，推送上周的周报表
             * 如果今天是月初，推送上月月报表
             */ 
            foreach (var item in serverNames)
            {
                if (item == OrderSource.CIB)//CIB与CIBAPP合并发送
                    continue;
                var dt = DateTime.Now.AddDays(-1);
                OrderStatisticServerOptSet.FirstOrDefault(i=>i.ServerName==item)?.PushDailyReport(dt);
                if (DateTime.Now.DayOfWeek == DayOfWeek.Monday)
                    OrderStatisticServerOptSet.FirstOrDefault(i => i.ServerName == item)?.PushWeekReport(Util.Helpers.Time.GetWeekNum(dt), dt.Year);
                if (DateTime.Now.Day == 1)
                    OrderStatisticServerOptSet.FirstOrDefault(i => i.ServerName == item)?.PushMonthReport(dt.Month, dt.Year);

            }
            return true;
        }
        public  bool CreatePandianReport(int monthNum)
        {
            var lst = Instance.PandianStatisticServerOptSet.ToList();
            foreach (var item in lst)
            {
                System.Threading.ThreadPool.QueueUserWorkItem(o =>
                {
                    try
                    {
                        item.CreateMonthPandianReport(monthNum, DateTime.Now.Year);
                    }
                    catch (Exception ex)
                    {
                        Util.Logs.Log.GetLog($"生成盘点报表失败，来源:{item.ServerName},message:{ex.Message},StackTrace:{ex.StackTrace}");

                    }

                });
            }

            return true;


        }
    }
    public enum StatisticType

    {
        Day=1,
        Week,
        Month,
        Quarter,
        Year
    }
}
