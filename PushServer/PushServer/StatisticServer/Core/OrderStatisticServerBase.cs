using FusionStone.WeiXin;

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
        public static event Action<string> UIMessageEventHandle;
        protected virtual void OnUIMessageEventHandle(string msg)
        {
            Util.Logs.Log.GetLog(nameof(ProductStatisticServerBase)).Debug(msg);
            var handle = UIMessageEventHandle;
            if (handle != null)
                handle(msg);
        }
        private bool CreateReport(StatisticType statisticType, int statisticValue, int year)
        {
            OnUIMessageEventHandle($"{ServerName}-{statisticType.ToString()}-{statisticValue}-订单报表开始统计");
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
                            
                            orderEntities = db.OrderSet.Include(o => o.OrderDateInfo).Include(o => o.OrderRepurchase).Include(o => o.OrderExtendInfo).Include(o => o.Consignee).Where(o =>o.OrderType==0&& o.OrderDateInfo.CreateTime>=start && o.OrderDateInfo.CreateTime <= end && o.CreatedDate.Year == year).ToList();
                            break;
                        case StatisticType.Week:
                            orderEntities = db.OrderSet.Include(o => o.OrderDateInfo).Include(o => o.OrderRepurchase).Include(o => o.OrderExtendInfo).Include(o => o.Consignee).Where(s => s.OrderType == 0 && s.OrderDateInfo.WeekNum == statisticValue && s.CreatedDate.Year == year).ToList();
                            break;
                        case StatisticType.Month:
                            orderEntities = db.OrderSet.Include(o => o.OrderDateInfo).Include(o => o.OrderRepurchase).Include(o => o.OrderExtendInfo).Include(o => o.Consignee).Where(s => s.OrderType == 0 && s.OrderDateInfo.MonthNum == statisticValue && s.CreatedDate.Year == year).ToList();
                            break;
                        case StatisticType.Quarter:
                            orderEntities = db.OrderSet.Include(o => o.OrderDateInfo).Include(o => o.OrderRepurchase).Include(o => o.OrderExtendInfo).Include(o => o.Consignee).Where(s => s.OrderType == 0 && s.OrderDateInfo.SeasonNum == statisticValue && s.CreatedDate.Year == year).ToList();
                            break;
                        case StatisticType.Year:
                            orderEntities = db.OrderSet.Include(o => o.OrderDateInfo).Include(o => o.OrderRepurchase).Include(o => o.OrderExtendInfo).Include(o => o.Consignee).Where(s => s.OrderType == 0 && s.OrderDateInfo.Year == statisticValue).ToList();
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
                            Year = year,
                            PromotionalOrderCount = orderEntities.Where(o => o.OrderExtendInfo.IsPromotional == true).Count(),//总计优惠订单数
                            Source = ServerName,
                            SourceDesc = Util.Helpers.Reflection.GetDescription<OrderSource>(ServerName),
                            StatisticType = (int)statisticType,
                            StatisticValue = statisticValue,
                            TotalAmount = orderEntities.Sum(o => o.OrderExtendInfo.TotalAmount),//总计金额
                            TotalCustomer = orderEntities.GroupBy(o => o.Consignee).ToList().Count(),//总计客户数量
                            TotalOrderCount = orderEntities.Count,//总计订单数量
                            TotalProductCount = orderEntities.Sum(o => o.OrderExtendInfo.TotalProductCount),//总计盒数
                            
                            TotalWeight = orderEntities.Sum(o => o.OrderExtendInfo.TotalWeight),
                            TotalOrderRepurchase = orderEntities.Where(o => o.OrderRepurchase.DailyRepurchase == true).Count(),
                            TotalCustomerRepurchase = orderEntities.Where(o => o.OrderRepurchase.DailyRepurchase == true).GroupBy(o => o.Consignee).ToList().Count(),
                            TotalProductRepurchase = orderEntities.Where(o => o.OrderRepurchase.DailyRepurchase == true).Sum(o => o.OrderExtendInfo.TotalProductCount)

                        };
                      

                        var foo = db.StatisticSet.FirstOrDefault(s => s.Source == statistic.Source && s.StatisticType == statistic.StatisticType && s.StatisticValue == statistic.StatisticValue&&s.Year==year);
                        if (foo == null)
                            db.StatisticSet.Add(statistic);
                        else
                        {
                            db.StatisticSet.Remove(foo);
                            db.StatisticSet.Add(statistic);
                        }
                        db.SaveChanges();
                        OnUIMessageEventHandle($"{ServerName}-{statisticType.ToString()}-{statisticValue}-统计完毕");
                    }
                    else
                    {
                        OnUIMessageEventHandle($"{ServerName}-{statisticType.ToString()}-{statisticValue}-订单方面无统计结果");
                        
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
            return CreateReport(StatisticType.Quarter, seasonnum, year);
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
         
            var WxNewsUrl = System.Configuration.ConfigurationManager.AppSettings["WxNewsUrl"];
            var WxNewsPicUrl = System.Configuration.ConfigurationManager.AppSettings["WxNewsPicUrl"]; 
            var WxNewssmallPicUrl = System.Configuration.ConfigurationManager.AppSettings["WxNewssmallPicUrl"];

            var redirectUri = string.Format("{0}?date={1}&mode=day&source={2}", WxNewsUrl, DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd"), ServerName);

            redirectUri = System.Web.HttpUtility.UrlEncode(redirectUri);

            var url = WxPushNews.CreateWxNewsOAuthUrl(redirectUri);
            var picUrl = WxNewsPicUrl;
            var smallpicUrl = WxNewssmallPicUrl;
            using (var db = new OMSContext())
            {
                Statistic foo = new Statistic();
                string title = string.Empty;
                
                List<Statistic> lst = new List<Statistic>();
                var nameDesc =  Util.Helpers.Reflection.GetDescription<OrderSource>(ServerName);
                switch (statisticType)
                {
                    case StatisticType.Day:
                        
                        DateTime dateTime = new DateTime(year, 1, 1).AddDays(statisticValue - 1);
                        lst = db.StatisticSet.Where(s => s.StatisticType == (int)statisticType && s.StatisticValue == statisticValue && s.Year == year && s.SourceDesc == nameDesc).ToList();
                        
                        foreach (var s in lst)
                        {
                            foo.TotalAmount += s.TotalAmount;
                            foo.TotalCustomer += s.TotalCustomer;
                            foo.TotalCustomerRepurchase += s.TotalCustomerRepurchase;
                            foo.TotalOrderCount += s.TotalOrderCount;
                            foo.TotalOrderRepurchase += s.TotalOrderRepurchase;
                            foo.TotalProductCount += s.TotalProductCount;
                            foo.TotalProductRepurchase += s.TotalProductRepurchase;
                            foo.TotalWeight += s.TotalWeight;
                        }
                        if (foo == null || foo.TotalOrderCount <= 0)
                            title = string.Format("{0} #{1}#{2}(今日无单）", dateTime.ToString("yyyy年MM月dd日"), nameDesc, Environment.NewLine);
                        else
                            title = string.Format("{0} #{1}#", dateTime.ToString("yyyy年MM月dd日"), nameDesc);
                        break;
                    case StatisticType.Week:
                       
                        lst = db.StatisticSet.Where(s => s.StatisticType == (int)StatisticType.Week && s.StatisticValue == statisticValue && s.Year == year && s.SourceDesc == nameDesc).ToList();
                        foreach (var s in lst)
                        {
                            foo.TotalAmount += s.TotalAmount;
                            foo.TotalCustomer += s.TotalCustomer;
                            foo.TotalCustomerRepurchase += s.TotalCustomerRepurchase;
                            foo.TotalOrderCount += s.TotalOrderCount;
                            foo.TotalOrderRepurchase += s.TotalOrderRepurchase;
                            foo.TotalProductCount += s.TotalProductCount;
                            foo.TotalProductRepurchase += s.TotalProductRepurchase;
                            foo.TotalWeight += s.TotalWeight;
                        }
                        if (foo == null || foo.TotalOrderCount <= 0)
                            title = string.Format("{0} #{1}#{2}(本周无单）", $"{year}年{statisticValue}周", nameDesc, Environment.NewLine);
                        else
                            title = $"{year}年#{statisticValue}周#{nameDesc}";
                        break;
                    case StatisticType.Month:

                        lst = db.StatisticSet.Where(s => s.StatisticType == (int)StatisticType.Month && s.StatisticValue == statisticValue && s.Year == year && s.SourceDesc == nameDesc).ToList();
                        foreach (var s in lst)
                        {
                            foo.TotalAmount += s.TotalAmount;
                            foo.TotalCustomer += s.TotalCustomer;
                            foo.TotalCustomerRepurchase += s.TotalCustomerRepurchase;
                            foo.TotalOrderCount += s.TotalOrderCount;
                            foo.TotalOrderRepurchase += s.TotalOrderRepurchase;
                            foo.TotalProductCount += s.TotalProductCount;
                            foo.TotalProductRepurchase += s.TotalProductRepurchase;
                            foo.TotalWeight += s.TotalWeight;
                        }
                        if (foo == null || foo.TotalOrderCount <= 0)
                            title = string.Format("{0} #{1}#{2}(本月无单）", $"{year}年{statisticValue}月", nameDesc, Environment.NewLine);
                        else
                            title = $"{year}年#{statisticValue}月份#{nameDesc}";
                        break;
                    case StatisticType.Quarter:

                        lst = db.StatisticSet.Where(s => s.StatisticType == (int)StatisticType.Quarter && s.StatisticValue == statisticValue && s.Year == year && s.SourceDesc == nameDesc).ToList();
                        foreach (var s in lst)
                        {
                            foo.TotalAmount += s.TotalAmount;
                            foo.TotalCustomer += s.TotalCustomer;
                            foo.TotalCustomerRepurchase += s.TotalCustomerRepurchase;
                            foo.TotalOrderCount += s.TotalOrderCount;
                            foo.TotalOrderRepurchase += s.TotalOrderRepurchase;
                            foo.TotalProductCount += s.TotalProductCount;
                            foo.TotalProductRepurchase += s.TotalProductRepurchase;
                            foo.TotalWeight += s.TotalWeight;
                        }
                        if (foo == null || foo.TotalOrderCount <= 0)
                            title = string.Format("{0} #{1}#{2}(本季无单）", $"{year}年{statisticValue}季", nameDesc, Environment.NewLine);
                        else
                            title = $"{year}年#{statisticValue}季度#{nameDesc}";
                        break;
                    case StatisticType.Year:

                        lst = db.StatisticSet.Where(s => s.StatisticType == (int)StatisticType.Year && s.StatisticValue == year&& s.SourceDesc == nameDesc).ToList();
                        foreach (var s in lst)
                        {
                            foo.TotalAmount += s.TotalAmount;
                            foo.TotalCustomer += s.TotalCustomer;
                            foo.TotalCustomerRepurchase += s.TotalCustomerRepurchase;
                            foo.TotalOrderCount += s.TotalOrderCount;
                            foo.TotalOrderRepurchase += s.TotalOrderRepurchase;
                            foo.TotalProductCount += s.TotalProductCount;
                            foo.TotalProductRepurchase += s.TotalProductRepurchase;
                            foo.TotalWeight += s.TotalWeight;
                        }
                        if (foo == null || foo.TotalOrderCount <= 0)
                            title = string.Format("{0} #{1}#{2}(本年无单）", $"{year}年", nameDesc, Environment.NewLine);
                        else
                            title = $"{year}年#{nameDesc}";
                        break;
                    default:
                        break;
                }


                var wxArticles = new List<WxArticle>()
                {
                    new WxArticle( title,url,picUrl,string.Empty)
                };
                int rangvalue = 7;
                switch (statisticType)
                {
                   
                    case StatisticType.Week:
                        rangvalue = 7;
                        break;
                    case StatisticType.Month:
                        rangvalue = DateTime.DaysInMonth(year, statisticValue);
                        break;
                    case StatisticType.Quarter:
                        rangvalue = Util.Helpers.Time.GetDaysInSeason(year, statisticValue);
                        break;
                    case StatisticType.Year:
                        rangvalue = Util.Helpers.Time.GetDaysInYear(year);
                        break;
                    default:
                        break;
                }
                if (foo != null && foo.TotalOrderCount > 0 && statisticType != StatisticType.Day)
                {
                    wxArticles.AddRange(new List<WxArticle>()
                    {
                          new WxArticle(string.Format("总计单数：{0}", foo.TotalOrderCount),url,smallpicUrl,string.Empty),
                        new WxArticle(string.Format("总计盒数：{0}", foo.TotalProductCount),url,smallpicUrl,string.Empty),
                       

                        new WxArticle(string.Format("总计人数：{0}", foo.TotalCustomer),url,smallpicUrl,string.Empty),
                         new WxArticle(string.Format("日均单数：{0}", Math.Round((double)foo.TotalOrderCount/rangvalue,2)),url,smallpicUrl,string.Empty),
                        new WxArticle(string.Format("日均盒数：{0}",Math.Round((double)foo.TotalProductCount/rangvalue,2)),url,smallpicUrl,string.Empty),
                       // new WxArticle(string.Format("总计重量(kg)：{0}", foo.TotalWeight/1000),url,smallpicUrl,string.Empty),
                       // new WxArticle(string.Format("总计促销单数：{0}", foo.PromotionalOrderCount),url,string.Empty,string.Empty),
                      //  new WxArticle(string.Format("总计复购人数：{0}", foo.TotalCustomerRepurchase),url,smallpicUrl,string.Empty),

                        new WxArticle(string.Format("总计人数复购率：{0}%", Math.Round((double)foo.TotalCustomerRepurchase*100/foo.TotalCustomer,2)),url,smallpicUrl,string.Empty),
                        new WxArticle(string.Format("总计单数复购率：{0}%",Math.Round((double)foo.TotalOrderRepurchase*100/foo.TotalOrderCount,2)),url,smallpicUrl,string.Empty),

                    });
                }
                else if (foo != null && foo.TotalOrderCount > 0 && statisticType == StatisticType.Day)
                {
                    wxArticles.AddRange(new List<WxArticle>()
                    {

                        new WxArticle(string.Format("总计人数：{0}", foo.TotalCustomer),url,smallpicUrl,string.Empty),
                          new WxArticle(string.Format("总计单数：{0}", foo.TotalOrderCount),url,smallpicUrl,string.Empty),
                        new WxArticle(string.Format("总计盒数：{0}", foo.TotalProductCount),url,smallpicUrl,string.Empty),
                        new WxArticle(string.Format("总计重量(kg)：{0}", foo.TotalWeight/1000),url,smallpicUrl,string.Empty),
                       // new WxArticle(string.Format("总计促销单数：{0}", foo.PromotionalOrderCount),url,string.Empty,string.Empty),
                        new WxArticle(string.Format("总计复购人数：{0}", foo.TotalCustomerRepurchase),url,smallpicUrl,string.Empty),

                        new WxArticle(string.Format("总计人数复购率：{0}%", Math.Round((double)foo.TotalCustomerRepurchase*100/foo.TotalCustomer,2)),url,smallpicUrl,string.Empty),
                        new WxArticle(string.Format("总计单数复购率：{0}%",Math.Round((double)foo.TotalOrderRepurchase*100/foo.TotalOrderCount,2)),url,smallpicUrl,string.Empty),

                    });
                }
                try
                {
                    WxPushNews.OrderStatistic(wxArticles,ServerName);
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
