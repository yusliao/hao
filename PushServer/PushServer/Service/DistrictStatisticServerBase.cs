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
    public class DistrictStatisticServerBase : IDistrictStatisticServer
    {
        private bool CreateReport(StatisticType statisticType,int statisticValue,int year)
        {
            try
            {
                using (var db = new OMSContext())
                {
                    List<OrderEntity> orderEntities = new List<OrderEntity>();

                    switch (statisticType)
                    {
                        case StatisticType.Day:
                            orderEntities = db.OrderSet.Include(o => o.OrderDateInfo).Include(o => o.OrderExtendInfo).Include(o => o.ConsigneeAddress).Where(s => s.CreatedDate.DayOfYear == statisticValue && s.CreatedDate.Year == year).ToList();
                            break;
                        case StatisticType.Week:
                            orderEntities = db.OrderSet.Include(o => o.OrderDateInfo).Include(o => o.OrderExtendInfo).Include(o => o.ConsigneeAddress).Where(s => s.OrderDateInfo.WeekNum == statisticValue && s.CreatedDate.Year == year ).ToList();
                            break;
                        case StatisticType.Month:
                            orderEntities = db.OrderSet.Include(o => o.OrderDateInfo).Include(o => o.OrderExtendInfo).Include(o => o.ConsigneeAddress).Where(s => s.OrderDateInfo.MonthNum == statisticValue && s.CreatedDate.Year == year ).ToList();
                            break;
                        case StatisticType.Quarter:
                            orderEntities = db.OrderSet.Include(o => o.OrderDateInfo).Include(o => o.OrderExtendInfo).Include(o => o.ConsigneeAddress).Where(s => s.OrderDateInfo.SeasonNum == statisticValue && s.CreatedDate.Year == year ).ToList();
                            break;
                        case StatisticType.Year:
                            orderEntities = db.OrderSet.Include(o => o.OrderDateInfo).Include(o => o.OrderExtendInfo).Include(o => o.ConsigneeAddress).Where(s => s.OrderDateInfo.Year == statisticValue).ToList();
                            break;
                        default:
                            break;
                    }
                    if (orderEntities.Any())
                    {
                        var plst = orderEntities.Select(o => new { o.ConsigneeAddress,o.OrderExtendInfo});

                        var q = plst.GroupBy(p => new { p.ConsigneeAddress.Province });
                        List<StatisticDistrictItem> smp = new List<StatisticDistrictItem>();
                        foreach (var item in q)
                        {
                            StatisticDistrictItem statistic = new StatisticDistrictItem()
                            {
                              

                            };
                            smp.Add(statistic);
                        }

                        //foreach (var item in smp)
                        //{
                        //    OnUIMessageEventHandle($"{item.SourceDesc}\t{item.ProductPlatName}\t{item.weightCodeDesc}\t{item.ProductTotalWeight}\t{item.ProductTotalAmount}\t{item.ProductCount}");
                        //}

                        //var removeobj = db.StatisticProductSet.Where(s => s.Year == year && s.Source == ServerName && s.StatisticType == (int)statisticType && s.StatisticValue == statisticValue);
                        //db.Set<StatisticProduct>().RemoveRange(removeobj);
                        //db.SaveChanges();
                        //db.Set<StatisticProduct>().AddRange(smp);
                        //db.SaveChanges();
                    }
                    else
                    {
                        return false;
                    }
                    return true;

                }

            }
            catch (Exception ex)
            {
                Util.Logs.Log.GetLog(nameof(ProductStatisticServerBase)).Error($":error:{ex.Message},statck:{ex.StackTrace}");
                return false;
            }
        }
        public bool CreateDailyReport(DateTime value)
        {
            throw new NotImplementedException();
        }

        public bool CreateMonthReport(int monthnum, int year)
        {
            throw new NotImplementedException();
        }

        public bool CreateSeasonReport(int seasonnum, int year)
        {
            throw new NotImplementedException();
        }

        public bool CreateWeekReport(int weeknum, int year)
        {
            throw new NotImplementedException();
        }

        public bool CreateYearReport(int year)
        {
            throw new NotImplementedException();
        }

        public DataTable PushDailyReport(DateTime value)
        {
            throw new NotImplementedException();
        }

        public DataTable PushMonthReport(int monthnum, int year)
        {
            throw new NotImplementedException();
        }

        public DataTable PushSeasonReport(int seasonnum, int year)
        {
            throw new NotImplementedException();
        }

        public DataTable PushWeekReport(int weeknum, int year)
        {
            throw new NotImplementedException();
        }

        public DataTable PushYearReport(int year)
        {
            throw new NotImplementedException();
        }
    }
}
