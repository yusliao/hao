using OMS.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PushServer.Service
{
    public abstract class DistrictStatisticServerBase : IDistrictStatisticServer
    {
        public abstract string ServerName { get; }
        public abstract string ServerDesc { get; }
        public static event Action<string> UIMessageEventHandle;
        protected virtual void OnUIMessageEventHandle(string msg)
        {
            Util.Logs.Log.GetLog(nameof(ProductStatisticServerBase)).Debug(msg);
            var handle = UIMessageEventHandle;
            if (handle != null)
                handle(msg);
        }

        private bool CreateReport(StatisticType statisticType,int statisticValue,int year)
        {
            OnUIMessageEventHandle($"{ServerName}-{statisticType.ToString()}-{statisticValue}-地域报表开始统计");
            try
            {
                using (var db = new OMSContext())
                {
                    List<OrderEntity> orderEntities = new List<OrderEntity>();
                    
                    switch (statisticType)
                    {
                        case StatisticType.Day:
                            DateTime start = new DateTime(year, 1, 1).AddDays(statisticValue - 1);
                            DateTime end = start.AddDays(1);

                            orderEntities = db.OrderSet.Include(o => o.OrderDateInfo).Include(o=>o.OrderRepurchase).Include(o => o.OrderExtendInfo).Include(o => o.ConsigneeAddress).Where(o => (o.OrderType & 1) == 0 && (o.OrderType & 4) == 0 && o.OrderDateInfo.CreateTime >= start && o.OrderDateInfo.CreateTime < end && o.CreatedDate.Year == year).ToList();
                            break;
                        case StatisticType.Week:
                            orderEntities = db.OrderSet.Include(o => o.OrderDateInfo).Include(o => o.OrderRepurchase).Include(o => o.OrderExtendInfo).Include(o => o.ConsigneeAddress).Where(s => (s.OrderType & 1) == 0 && (s.OrderType & 4) == 0 && s.OrderDateInfo.WeekNum == statisticValue && s.CreatedDate.Year == year).ToList();
                            break;
                        case StatisticType.Month:
                            orderEntities = db.OrderSet.Include(o => o.OrderDateInfo).Include(o => o.OrderRepurchase).Include(o => o.OrderExtendInfo).Include(o => o.ConsigneeAddress).Where(s => (s.OrderType & 1) == 0 && (s.OrderType & 4) == 0 && s.OrderDateInfo.MonthNum == statisticValue && s.CreatedDate.Year == year).ToList();
                            break;
                        case StatisticType.Quarter:
                            orderEntities = db.OrderSet.Include(o => o.OrderDateInfo).Include(o => o.OrderRepurchase).Include(o => o.OrderExtendInfo).Include(o => o.ConsigneeAddress).Where(s => (s.OrderType & 1) == 0 && (s.OrderType & 4) == 0 && s.OrderDateInfo.SeasonNum == statisticValue && s.CreatedDate.Year == year).ToList();
                            break;
                        case StatisticType.Year:
                            orderEntities = db.OrderSet.Include(o => o.OrderDateInfo).Include(o => o.OrderRepurchase).Include(o => o.OrderExtendInfo).Include(o => o.ConsigneeAddress).Where(s => (s.OrderType & 1) == 0 && (s.OrderType & 4) == 0 && s.OrderDateInfo.Year == statisticValue).ToList();
                            break;
                        default:
                            break;
                    }
                    if (ServerName != OrderSource.ALL)
                        orderEntities = orderEntities.Where(o => o.Source == ServerName).ToList();

                    if (orderEntities.Any())
                    {
                        var plst = orderEntities.Select(o => new { o.ConsigneeAddress,o.OrderExtendInfo,o.OrderRepurchase});
                       
                        List<StatisticDistrictItem> provinceitems = new List<StatisticDistrictItem>();
                        List<StatisticDistrictItem> cityitems = new List<StatisticDistrictItem>();


                        var q1 = plst.GroupBy(p => new { p.ConsigneeAddress.Province });//按省份统计
                        DateTime createDate = DateTime.Now;
                        if (StatisticType.Day == statisticType)
                        {
                            DateTime start = new DateTime(year, 1, 1).AddDays(statisticValue - 1);
                            createDate = start;
                        }

                        foreach (var item in q1)
                        {
                            var cad = db.ChinaAreaDatas.FirstOrDefault(a => a.Name.IndexOf(item.Key.Province) == 0);
                            if (cad == null)
                                cad = db.ChinaAreaDatas.Find(100000);//中国
                            StatisticDistrictItem statistic = new StatisticDistrictItem()
                            {
                                CreateDate = createDate,
                                DiscountFee = item.Sum(a => a.OrderExtendInfo.DiscountFee),
                                TotalOrders = item.Count(),
                                TotalAmount = item.Sum(a => a.OrderExtendInfo.TotalAmount),
                                TotalCostAmount = item.Sum(o => o.OrderExtendInfo.TotalCostPrice),
                                TotalFlatAmount = item.Sum(o => o.OrderExtendInfo.TotalFlatAmount),
                                TotalProductCount = item.Sum(a => a.OrderExtendInfo.TotalProductCount),
                                TotalWeight = item.Sum(a => a.OrderExtendInfo.TotalWeight),
                                AddressID = cad

                            };
                            DateTime start = new DateTime(year, 1, 1).AddDays(statisticValue - 1);
                            statistic.CreateDate = start;
                           
                            provinceitems.Add(statistic);
                        }
                     
                        
                        var q2 = plst.GroupBy(p => new { p.ConsigneeAddress.Province, p.ConsigneeAddress.City });//按省份，地级市统计
                        foreach (var item in q2)
                        {
                            var cad = db.ChinaAreaDatas.FirstOrDefault(a => a.MergerName.Contains(item.Key.City) && a.MergerName.Contains(item.Key.Province));
                            if (cad == null)
                                cad = db.ChinaAreaDatas.Find(100000);//中国
                            StatisticDistrictItem statistic = new StatisticDistrictItem()
                            {
                                CreateDate = createDate,
                                DiscountFee = item.Sum(a => a.OrderExtendInfo.DiscountFee),
                                TotalOrders = item.Count(),
                                TotalAmount = item.Sum(a => a.OrderExtendInfo.TotalAmount),
                                TotalProductCount = item.Sum(a => a.OrderExtendInfo.TotalProductCount),
                                TotalWeight = item.Sum(a => a.OrderExtendInfo.TotalWeight),
                                AddressID = cad

                            };
                            cityitems.Add(statistic);
                        }
                      
                      
                        StatisticDistrict statisticDistrict = new StatisticDistrict()
                        {
                            StatisticDistrictriCityItems = cityitems,
                            StatisticDistrictriProvinceItems = provinceitems,
                            StatisticType = (int)statisticType,
                            Source = ServerName,
                            SourceDesc=Util.Helpers.Reflection.GetDescription<OrderSource>(ServerName),
                            StatisticValue = statisticValue,
                            CreateDate = DateTime.Now,
                            Year = year
                            
                        };
                        if (StatisticType.Day == statisticType)
                        {
                            DateTime start = new DateTime(year, 1, 1).AddDays(statisticValue - 1);
                            statisticDistrict.CreateDate = start;
                        }

                        var removeobj = db.StatisticDistricts.Include(s=>s.StatisticDistrictriProvinceItems).Include(s=>s.StatisticDistrictriCityItems).FirstOrDefault(s => s.Year == year && s.Source == ServerName && s.StatisticType == (int)statisticType && s.StatisticValue == statisticValue);
                        if (removeobj != null)
                        {
                            //var delobj = db.StatisticDistrictItems.Where(s => s. == removeobj.StatisticDistrictID);
                            //if (delobj != null)
                            //    db.Set<StatisticDistrictItem>().RemoveRange(delobj);
                            db.StatisticDistricts.Remove(removeobj);
                           
                           // db.SaveChanges();
                        }
                       // db.StatisticDistricts.Remove(removeobj);
                        db.Set<StatisticDistrictItem>().AddRange(provinceitems);
                        db.Set<StatisticDistrictItem>().AddRange(cityitems);
                        
                        db.StatisticDistricts.Add(statisticDistrict);
                        db.SaveChanges();
                        OnUIMessageEventHandle($"{ServerName}-{statisticType.ToString()}-{statisticValue}-地域统计完毕");
                    }
                    else
                    {
                        OnUIMessageEventHandle($"{ServerName}-{statisticType.ToString()}-{statisticValue}-地域方面无统计结果");
                        return false;
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
        public virtual bool CreateDistrictDailyReport(DateTime value)
        {
            return CreateReport(StatisticType.Day, value.DayOfYear, value.Year);
        }

        public virtual bool CreateDistrictMonthReport(int monthnum, int year)
        {
            return CreateReport(StatisticType.Month, monthnum, year);
        }

        public virtual bool CreateDistrictSeasonReport(int seasonnum, int year)
        {
            return CreateReport(StatisticType.Quarter, seasonnum, year);
        }

        public virtual bool CreateDistrictWeekReport(int weeknum, int year)
        {
            return CreateReport(StatisticType.Week, weeknum, year);
        }

        public virtual bool CreateDistrictYearReport(int year)
        {
            return CreateReport(StatisticType.Year, year, year);
        }
        private DataTable PushReport(StatisticType statisticType, int statisticValue, int year)
        {
            return null;
        }

        public virtual DataTable PushDistrictDailyReport(DateTime value)
        {
            throw new NotImplementedException();
        }

        public virtual DataTable PushDistrictMonthReport(int monthnum, int year)
        {
            throw new NotImplementedException();
        }

        public virtual DataTable PushDistrictSeasonReport(int seasonnum, int yearL)
        {
            throw new NotImplementedException();
        }

        public virtual DataTable PushDistrictWeekReport(int weeknum, int year)
        {
            throw new NotImplementedException();
        }

        public virtual DataTable PushDistrictYearReport(int year)
        {
            throw new NotImplementedException();
        }
    }
    public class DistrictStatisticServerCommon : DistrictStatisticServerBase
    {


        public override string ServerName => Name;
        public override string ServerDesc => Desc;
        private String Name;
        private string Desc;
        public DistrictStatisticServerCommon(string name, string desc)
        {
            Name = name;
            Desc = desc;
        }

    }
}
