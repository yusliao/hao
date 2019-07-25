using FusionStone.WeiXin;
using M2.OrderManagement.Sync;
using OMS.Models;
using PushServer.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PushServer.Service
{
    class StatisticServer
    {
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
                Statistic foo = null;
                string title = string.Empty;
                switch (statisticType)
                {
                    case StatisticType.Day:

                        var daynum = dateTime.DayOfYear;
                        foo = db.StatisticSet.Where(s => s.StatisticType == (int)StatisticType.Day && s.StatisticValue == daynum).FirstOrDefault();

                        if (foo == null || foo.TotalOrderCount <= 0)
                            title = string.Format("{0} #{1}#{2}(今日无单）", dateTime.ToString("yyyy年MM月dd日"), OrderSource, Environment.NewLine);
                        else
                            title = string.Format("{0} #{1}#", foo.CreateDate.ToString("yyyy年MM月dd日"), OrderSource);
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
                            title = string.Format("{0} #{1}#{2}(今日无单）", foo.CreateDate.ToString("yyyy年MM月dd日"), OrderSource, Environment.NewLine);
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

                if (foo.TotalOrderCount > 0)
                    wxArticles.AddRange(new List<WxArticle>()
                {
                    new WxArticle(string.Format("总计单数：{0}", foo.TotalOrderCount),url,string.Empty,string.Empty),
                    new WxArticle(string.Format("总计盒数：{0}", foo.TotalProductCount),url,string.Empty,string.Empty),
                    new WxArticle(string.Format("总计人数：{0}", foo.TotalCustomer),url,string.Empty,string.Empty),
                    new WxArticle(string.Format("总计重量(kg)：{0}", foo.TotalWeight/1000),url,string.Empty,string.Empty),
                    new WxArticle(string.Format("总计促销单数：{0}", foo.PromotionalOrderCount),url,string.Empty,string.Empty),
                    new WxArticle(string.Format("总计复购人数：{0}", foo.TotalReturningCustomer),url,string.Empty,string.Empty),
                   
                    new WxArticle(string.Format("总计复购率：{0}%", (foo.TotalReturningCustomer/foo.TotalCustomer)*100),url,string.Empty,string.Empty),

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
        public static void CreateDailyReport(DateTime dateTime)
        {
            /*生成昨日报表
             */
            using (var db = new OMSContext())
            {
                //根据OrderExtendInfo信息生成记录
                DateTime start = dateTime.Date;
                DateTime end = start.AddDays(1);
                var lst = db.OrderSet.Include(o => o.Consignee).Include(o => o.OrderExtendInfo).Include(o => o.OrderRepurchase).Where(o => o.CreatedDate> start && o.CreatedDate< end&&o.CreatedDate.Year==dateTime.Year).ToList();
                if (lst != null && lst.Any())
                {
                    Statistic statistic = new Statistic()
                    {
                        CreateDate = dateTime.Date,
                        PromotionalOrderCount = lst.Where(o => o.OrderExtendInfo.IsPromotional == true).Count(),
                        Source = OrderSource.ALL,
                        SourceDesc = Util.Helpers.Reflection.GetDescription<OrderSource>(OrderSource.ALL),
                        StatisticType = (int)StatisticType.Day,
                        StatisticValue = dateTime.DayOfYear,
                        TotalAmount = lst.Sum(o => o.OrderExtendInfo.TotalAmount),
                        TotalCustomer = lst.GroupBy(o => o.Consignee).Count(),
                        TotalOrderCount = lst.Count,
                        TotalProductCount = lst.Sum(o => o.OrderExtendInfo.TotalProductCount),
                        TotalReturningCustomer = lst.Where(o => o.OrderRepurchase.DailyRepurchase == true).Count(),
                        TotalWeight = lst.Sum(o => o.OrderExtendInfo.TotalWeight)
                    };
                    var foo = db.StatisticSet.FirstOrDefault(s => s.CreateDate == statistic.CreateDate && s.Source == statistic.Source && s.StatisticType == statistic.StatisticType && s.StatisticValue == statistic.StatisticValue);
                    if (foo == null)
                        db.StatisticSet.Add(statistic);
                    else
                    {
                        db.StatisticSet.Remove(foo);
                        db.StatisticSet.Add(statistic);
                    }
                    db.SaveChanges();
                }
                else
                {
                    Util.Logs.Log.GetLog(nameof(StatisticServer)).Info("今日无统计结果");
                    return;
                }
                
            }
        }
        public static void CreateWeekReport(int weeknum)
        {
            using (var db = new OMSContext())
            {
                
                //根据OrderExtendInfo信息生成记录
                var lst =db.OrderSet.Include(o=>o.Consignee).Include(o=>o.OrderExtendInfo).Include(o=>o.OrderRepurchase).Where(o => o.OrderDateInfo.WeekNum== weeknum&&o.CreatedDate.Year==DateTime.Now.Year).ToList();
                
                Statistic statistic = new Statistic()
                {
                    CreateDate = DateTime.Now.Date,
                    PromotionalOrderCount = lst.Where(o => o.OrderExtendInfo.IsPromotional == true).Count(),
                    Source = OrderSource.ALL,
                    SourceDesc = Util.Helpers.Reflection.GetDescription<OrderSource>(OrderSource.ALL),
                    StatisticType = (int)StatisticType.Week,
                    StatisticValue = weeknum,
                    TotalAmount = lst.Sum(o => o.OrderExtendInfo.TotalAmount),
                    TotalCustomer = lst.GroupBy(o=>o.Consignee).Count(),
                    TotalOrderCount = lst.Count,
                    TotalProductCount = lst.Sum(o => o.OrderExtendInfo.TotalProductCount),
                    TotalReturningCustomer = lst.Where(o => o.OrderRepurchase.WeekRepurchase == true).Count(),
                    TotalWeight = lst.Sum(o => o.OrderExtendInfo.TotalWeight)
                };
                var foo = db.StatisticSet.FirstOrDefault(s => s.Source == statistic.Source && s.StatisticType == statistic.StatisticType && s.StatisticValue == statistic.StatisticValue);
                if (foo == null)
                    db.StatisticSet.Add(statistic);
                else
                {
                    db.StatisticSet.Remove(foo);
                    db.StatisticSet.Add(statistic);
                }
                db.SaveChanges();

            }
        }
        /// <summary>
        /// 创建月订单报表
        /// </summary>
        /// <param name="monthnum"></param>
        public static void CreateMonthReport(int monthnum)
        {
            /*基于天为单位的统计数据做的月份订单统计报表
             * 
             * 
             */ 
            DateTime start = new DateTime(DateTime.Now.Year, monthnum, 1);
            DateTime end = new DateTime(DateTime.Now.Year, monthnum + 1, 1).AddDays(-1);

           
            using (var db = new OMSContext())
            {

                //根据OrderExtendInfo信息生成记录
                var lst = db.StatisticSet.Where(s => s.StatisticType==(int)StatisticType.Day&&(s.StatisticValue>=start.DayOfYear||s.StatisticValue<=end.DayOfYear)).ToList();
                Statistic statistic = new Statistic()
                {
                    CreateDate = DateTime.Now.Date,
                    PromotionalOrderCount = lst.Sum(s=>s.PromotionalOrderCount),
                    Source = OrderSource.ALL,
                    SourceDesc = Util.Helpers.Reflection.GetDescription<OrderSource>(OrderSource.ALL),
                    StatisticType = (int)StatisticType.Month,
                    StatisticValue = monthnum,
                    TotalAmount = lst.Sum(o => o.TotalAmount),
                   // TotalCustomer = lst.GroupBy(o => o.Consignee).Count(),
                    TotalOrderCount = lst.Sum(o => o.TotalOrderCount),
                    TotalProductCount = lst.Sum(o => o.TotalProductCount),
                  //  TotalReturningCustomer = lst.Sum(o => o.TotalReturningCustomer),
                    TotalWeight = lst.Sum(o => o.TotalWeight)
                };
                var seclst= db.OrderSet.Include(o => o.Consignee).Include(o => o.OrderExtendInfo).Include(o => o.OrderRepurchase).Where(o => o.CreatedDate >= start || o.CreatedDate <= end).ToList();
                int totalCustomer = seclst.GroupBy(o => o.Consignee).Count();
                int totalReturningCustomer = seclst.Where(o => o.OrderRepurchase.MonthRepurchase == true).Count();
                statistic.TotalCustomer = totalCustomer;
                statistic.TotalReturningCustomer = totalReturningCustomer;
                var foo = db.StatisticSet.FirstOrDefault(s => s.Source == statistic.Source && s.StatisticType == statistic.StatisticType && s.StatisticValue == statistic.StatisticValue);
                if (foo == null)
                    db.StatisticSet.Add(statistic);
                else
                {
                    db.StatisticSet.Remove(foo);
                    db.StatisticSet.Add(statistic);
                }
                db.SaveChanges();
            }
        }
        /// <summary>
        /// 创建报表 
        /// </summary>
        /// <param name="dt"></param>
        /// <param name="isAll">true:重新生成当月所有的报表（维度：天，周，月）</param>
        /// <returns></returns>
        public static bool CreateReport(DateTime dt,bool isAll=false)
        {
            try
            {
                if (isAll)
                {
                    int end = new DateTime(dt.Year, dt.Month+1, 1).AddDays(-1).Day;
                    for (int i = 0; i < end; i++)
                    {
                        var foo = new DateTime(dt.Year, dt.Month, i+1);
                        StatisticServer.CreateDailyReport(foo);
                    }
                }
                else
                {
                    StatisticServer.CreateDailyReport(dt);
                   
                }
                Util.Logs.Log.GetLog(nameof(StatisticServer)).Info($"{dt.ToString("yyyyMMdd HH:mm:ss")}日报表统计完毕");

                StatisticServer.CreateWeekReport(Util.Helpers.Time.GetWeekNum(dt));
                Util.Logs.Log.GetLog(nameof(StatisticServer)).Info($"{Util.Helpers.Time.GetWeekNum(dt)}周报表统计完毕");
                var temp = dt.AddDays(1).Day;

                if (temp == 1)//明天是下月第一天
                {
                    StatisticServer.CreateMonthReport(dt.Month);//生成当前月报表
                    Util.Logs.Log.GetLog(nameof(StatisticServer)).Info($"{dt.Month}月报表统计完毕");
                }
                return true;
            }
            catch (Exception ex)
            {
                Util.Logs.Log.GetLog(nameof(Statistic)).Error($"报表生成失败。/r/n{ex.Message}");
                return false;
            }
           
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
