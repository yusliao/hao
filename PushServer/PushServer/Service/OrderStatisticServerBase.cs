using FusionStone.WeiXin;
using M2.OrderManagement.Sync;
using OMS.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PushServer.Service
{
    /// <summary>
    /// 订单统计报表
    /// </summary>
    public abstract class OrderStatisticServerBase : IOrderStatisticServer
    {
        public  abstract string ServerName { get; }
        private bool CreateReport(StatisticType statisticType, int statisticValue, int year)
        {
            try
            {
                using (var db = new OMSContext())
                {
                    List<OrderEntity> orderEntities = new List<OrderEntity>();

                    switch (statisticType)
                    {
                        case StatisticType.Day:
                            DateTime start = new DateTime(year, 1, 1).AddDays(statisticValue-1);
                            DateTime end = start.AddDays(1);
                            
                            orderEntities = db.OrderSet.Include(o => o.OrderDateInfo).Include(o => o.OrderRepurchase).Include(o => o.OrderExtendInfo).Include(o => o.Consignee).Where(o =>o.OrderDateInfo.CreateTime>=start && o.OrderDateInfo.CreateTime < end && o.CreatedDate.Year == year).ToList();
                            break;
                        case StatisticType.Week:
                            orderEntities = db.OrderSet.Include(o => o.OrderDateInfo).Include(o => o.OrderRepurchase).Include(o => o.OrderExtendInfo).Include(o => o.Consignee).Where(s => s.OrderDateInfo.WeekNum == statisticValue && s.CreatedDate.Year == year).ToList();
                            break;
                        case StatisticType.Month:
                            orderEntities = db.OrderSet.Include(o => o.OrderDateInfo).Include(o => o.OrderRepurchase).Include(o => o.OrderExtendInfo).Include(o => o.Consignee).Where(s => s.OrderDateInfo.MonthNum == statisticValue && s.CreatedDate.Year == year).ToList();
                            break;
                        case StatisticType.Quarter:
                            orderEntities = db.OrderSet.Include(o => o.OrderDateInfo).Include(o => o.OrderRepurchase).Include(o => o.OrderExtendInfo).Include(o => o.Consignee).Where(s => s.OrderDateInfo.SeasonNum == statisticValue && s.CreatedDate.Year == year).ToList();
                            break;
                        case StatisticType.Year:
                            orderEntities = db.OrderSet.Include(o => o.OrderDateInfo).Include(o => o.OrderRepurchase).Include(o => o.OrderExtendInfo).Include(o => o.Consignee).Where(s => s.OrderDateInfo.Year == statisticValue).ToList();
                            break;
                        default:
                            break;
                    }
                    if (ServerName != OrderSource.ALL)
                        orderEntities = orderEntities.Where(o => o.Source == ServerName).ToList();

                    if (orderEntities != null && orderEntities.Any())
                    {
                        Statistic statistic = new Statistic()
                        {
                            CreateDate = DateTime.Now,
                            PromotionalOrderCount = orderEntities.Where(o => o.OrderExtendInfo.IsPromotional == true).Count(),//总计优惠订单数
                            Source = ServerName,
                            SourceDesc = Util.Helpers.Reflection.GetDescription<OrderSource>(ServerName),
                            StatisticType = (int)statisticType,
                            StatisticValue = statisticValue,
                            TotalAmount = orderEntities.Sum(o => o.OrderExtendInfo.TotalAmount),//总计金额
                            TotalCustomer = orderEntities.GroupBy(o => o.Consignee).Count(),//总计客户数量
                            TotalOrderCount = orderEntities.Count,//总计订单数量
                            TotalProductCount = orderEntities.Sum(o => o.OrderExtendInfo.TotalProductCount),//总计盒数
                            
                            TotalWeight = orderEntities.Sum(o => o.OrderExtendInfo.TotalWeight)
                        };

                        switch (statisticType)
                        {
                            case StatisticType.Day:
                                statistic.TotalOrderRepurchase = orderEntities.Where(o => o.OrderRepurchase.DailyRepurchase == true).Count();
                                statistic.TotalCustomerRepurchase = orderEntities.Where(o => o.OrderRepurchase.DailyRepurchase == true).GroupBy(o => o.Consignee).Count();
                                statistic.TotalProductRepurchase = orderEntities.Where(o => o.OrderRepurchase.DailyRepurchase == true).Sum(o => o.OrderExtendInfo.TotalProductCount);
                                break;
                            case StatisticType.Week:
                                statistic.TotalOrderRepurchase = orderEntities.Where(o => o.OrderRepurchase.WeekRepurchase == true).Count();
                                statistic.TotalCustomerRepurchase = orderEntities.Where(o => o.OrderRepurchase.WeekRepurchase == true).GroupBy(o => o.Consignee).Count();
                                statistic.TotalProductRepurchase = orderEntities.Where(o => o.OrderRepurchase.WeekRepurchase == true).Sum(o => o.OrderExtendInfo.TotalProductCount);
                                break;
                            case StatisticType.Month:
                                statistic.TotalOrderRepurchase = orderEntities.Where(o => o.OrderRepurchase.MonthRepurchase == true).Count();
                                statistic.TotalCustomerRepurchase = orderEntities.Where(o => o.OrderRepurchase.MonthRepurchase == true).GroupBy(o => o.Consignee).Count();
                                statistic.TotalProductRepurchase = orderEntities.Where(o => o.OrderRepurchase.MonthRepurchase == true).Sum(o => o.OrderExtendInfo.TotalProductCount);
                                break;
                            case StatisticType.Quarter:
                                statistic.TotalOrderRepurchase = orderEntities.Where(o => o.OrderRepurchase.SeasonRepurchase == true).Count();
                                statistic.TotalCustomerRepurchase = orderEntities.Where(o => o.OrderRepurchase.SeasonRepurchase == true).GroupBy(o => o.Consignee).Count();
                                statistic.TotalProductRepurchase = orderEntities.Where(o => o.OrderRepurchase.SeasonRepurchase == true).Sum(o => o.OrderExtendInfo.TotalProductCount);
                                break;
                            case StatisticType.Year:
                                statistic.TotalOrderRepurchase = orderEntities.Where(o => o.OrderRepurchase.YearRepurchase == true).Count();
                                statistic.TotalCustomerRepurchase = orderEntities.Where(o => o.OrderRepurchase.YearRepurchase == true).GroupBy(o => o.Consignee).Count();
                                statistic.TotalProductRepurchase = orderEntities.Where(o => o.OrderRepurchase.YearRepurchase == true).Sum(o => o.OrderExtendInfo.TotalProductCount);
                                break;
                            default:
                                break;
                        }

                        var foo = db.StatisticSet.FirstOrDefault(s => s.Source == statistic.Source && s.StatisticType == statistic.StatisticType && s.StatisticValue == statistic.StatisticValue&&s.Year==year);
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
                        
                    }
                    return true;

                }

            }
            catch (Exception ex)
            {
                Util.Logs.Log.GetLog(nameof(DistrictStatisticServerBase)).Error($":error:{ex.Message},statck:{ex.StackTrace}");
                return false;
            }
        }

        public virtual bool CreateDailyReport(DateTime value)
        {
            return CreateReport(StatisticType.Day, value.DayOfYear, value.Year);
        }

        public virtual bool CreateMonthReport(int monthnum, int year)
        {
            return CreateReport(StatisticType.Month, monthnum, year);
        }

        public virtual bool CreateSeasonReport(int seasonnum, int year)
        {
            return CreateReport(StatisticType.Month, seasonnum, year);
        }

        public virtual bool CreateWeekReport(int weeknum, int year)
        {
            return CreateReport(StatisticType.Week, weeknum, year);
        }

        public virtual bool CreateYearReport(int year)
        {
            return CreateReport(StatisticType.Year, year, year);
        }
        private  void PushReport(StatisticType statisticType, int statisticValue,int year)
        {
            var wxTargets = System.Configuration.ConfigurationManager.AppSettings["WxNewsTargets"].Split(new char[] { ',' }).ToList();
            var WxNewsUrl = System.Configuration.ConfigurationManager.AppSettings["WxNewsUrl"];
            var WxNewsPicUrl = System.Configuration.ConfigurationManager.AppSettings["WxNewsPicUrl"];

            var redirectUri = string.Format("{0}?date={1}&mode=day&source={2}", WxNewsUrl, DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd"), ServerName);

            redirectUri = System.Web.HttpUtility.UrlEncode(redirectUri);

            var url = WxPushNews.CreateWxNewsOAuthUrl(redirectUri);
            var picUrl = WxNewsPicUrl;
            using (var db = new OMSContext())
            {
                Statistic foo = null;
                string title = string.Empty;
                DateTime dateTime = new DateTime(year, 1, 1).AddDays(statisticValue - 1);
                switch (statisticType)
                {
                    case StatisticType.Day:

                       
                        foo = db.StatisticSet.Where(s => s.StatisticType == (int)statisticType && s.StatisticValue == statisticValue&&s.Year==year).FirstOrDefault();

                        if (foo == null || foo.TotalOrderCount <= 0)
                            title = string.Format("{0} #{1}#{2}(今日无单）", dateTime.ToString("yyyy年MM月dd日"), ServerName, Environment.NewLine);
                        else
                            title = string.Format("{0} #{1}#", foo.CreateDate.ToString("yyyy年MM月dd日"), ServerName);
                        break;
                    case StatisticType.Week:
                        int week = Util.Helpers.Time.GetWeekNum(DateTime.Now);
                        if (week > 1)
                            week -= 1;//发送上周的报表
                        foo = db.StatisticSet.Where(s => s.StatisticType == (int)StatisticType.Week && s.StatisticValue == week).FirstOrDefault();
                        if (foo == null || foo.TotalOrderCount <= 0)
                            title = string.Format("{0} #{1}#{2}(今日无单）", dateTime.ToString("yyyy年MM月dd日"), ServerName, Environment.NewLine);
                        else
                            title = $"{DateTime.Now.Year}年#{week}周#{ServerName}";
                        break;
                    case StatisticType.Month:
                        int month = DateTime.Now.Month;
                        if (month > 1)
                            month -= 1;//发送上个月的报表
                        foo = db.StatisticSet.Where(s => s.StatisticType == (int)StatisticType.Month && s.StatisticValue == month).FirstOrDefault();
                        if (foo == null || foo.TotalOrderCount <= 0)
                            title = string.Format("{0} #{1}#{2}(今日无单）", dateTime.ToString("yyyy年MM月dd日"), ServerName, Environment.NewLine);
                        else
                            title = $"{DateTime.Now.Year}年#{month}月份#{ServerName}";
                        break;
                    case StatisticType.Quarter:
                        int quarter = Util.Helpers.Time.GetSeasonNum(DateTime.Now);
                        foo = db.StatisticSet.Where(s => s.StatisticType == (int)StatisticType.Quarter && s.StatisticValue == quarter).FirstOrDefault();
                        if (foo == null || foo.TotalOrderCount <= 0)
                            title = string.Format("{0} #{1}#{2}(今日无单）", foo.CreateDate.ToString("yyyy年MM月dd日"), ServerName, Environment.NewLine);
                        else
                            title = $"{DateTime.Now.Year}年#{quarter}季度#{ServerName}";
                        break;
                    case StatisticType.Year:

                        foo = db.StatisticSet.Where(s => s.StatisticType == (int)StatisticType.Month && s.StatisticValue == DateTime.Now.Year).FirstOrDefault();
                        if (foo == null || foo.TotalOrderCount <= 0)
                            title = string.Format("{0} #{1}#{2}(今日无单）", dateTime.ToString("yyyy年MM月dd日"), ServerName, Environment.NewLine);
                        else
                            title = $"{DateTime.Now.Year}年#{ServerName}";
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
                    new WxArticle(string.Format("总计复购人数：{0}", foo.TotalCustomerRepurchase),url,string.Empty,string.Empty),
                     new WxArticle(string.Format("总计复购单数：{0}", foo.TotalOrderRepurchase),url,string.Empty,string.Empty),
                    new WxArticle(string.Format("总计人数复购率：{0}%", (foo.TotalCustomerRepurchase/foo.TotalCustomer)*100),url,string.Empty,string.Empty),
                    new WxArticle(string.Format("总计单数复购率：{0}%", (foo.TotalOrderRepurchase/foo.TotalOrderCount)*100),url,string.Empty,string.Empty),
                    new WxArticle(string.Format("总计盒数复购率：{0}%", (foo.TotalProductRepurchase/foo.TotalProductCount)*100),url,string.Empty,string.Empty),

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
        public virtual void PushDailyReport(DateTime value)
        {
            PushReport(StatisticType.Day, value.DayOfYear, value.Year);
        }

        public virtual void PushMonthReport(int monthnum, int year)
        {
            PushReport(StatisticType.Month, monthnum, year);
        }

        public virtual void PushSeasonReport(int seasonnum, int year)
        {

            PushReport(StatisticType.Quarter, seasonnum, year);
        }

        public virtual void PushWeekReport(int weeknum, int year)
        {

            PushReport(StatisticType.Week,weeknum, year);
        }

        public virtual void PushYearReport(int year)
        {
            PushReport(StatisticType.Year, year, year);
        }
    }
}
