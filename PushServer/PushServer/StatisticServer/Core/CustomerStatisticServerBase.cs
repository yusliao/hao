using FusionStone.WeiXin;

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
    /// <summary>
    /// 订单统计报表
    /// </summary>
    public abstract class CustomerStatisticServerBase : ICustomerStatisticServer
    {
        public  abstract string ServerName { get; }
        public static event Action<string> UIMessageEventHandle;
        protected virtual void OnUIMessageEventHandle(string msg)
        {
            Util.Logs.Log.GetLog(nameof(CustomerStatisticServerBase)).Debug(msg);
            var handle = UIMessageEventHandle;
            if (handle != null)
                handle(msg);
        }
        private bool CreateReport(StatisticType statisticType, int statisticValue, int year)
        {
            OnUIMessageEventHandle($"{ServerName}-{statisticType.ToString()}-{statisticValue}-客户报表开始统计");
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
                          
                            orderEntities = db.OrderSet.Include(o => o.OrderDateInfo).Include(o => o.OrderRepurchase).Include(o => o.OrderExtendInfo).Include(o => o.Consignee).Where(o => (o.OrderType & 1) == 0&&(o.OrderType&4)==0&& o.OrderDateInfo.CreateTime>=start && o.OrderDateInfo.CreateTime <= end && o.CreatedDate.Year == year).ToList();
                            break;
                        case StatisticType.Week:
                            orderEntities = db.OrderSet.Include(o => o.OrderDateInfo).Include(o => o.OrderRepurchase).Include(o => o.OrderExtendInfo).Include(o => o.Consignee).Where(s => (s.OrderType & 1) == 0 && (s.OrderType & 4) == 0 && s.OrderDateInfo.WeekNum == statisticValue && s.CreatedDate.Year == year).ToList();
                            break;
                        case StatisticType.Month:
                            orderEntities = db.OrderSet.Include(o => o.OrderDateInfo).Include(o => o.OrderRepurchase).Include(o => o.OrderExtendInfo).Include(o => o.Consignee).Where(s => (s.OrderType & 1) == 0 && (s.OrderType & 4) == 0 && s.OrderDateInfo.MonthNum == statisticValue && s.CreatedDate.Year == year).ToList();
                            break;
                        case StatisticType.Quarter:
                            orderEntities = db.OrderSet.Include(o => o.OrderDateInfo).Include(o => o.OrderRepurchase).Include(o => o.OrderExtendInfo).Include(o => o.Consignee).Where(s => (s.OrderType & 1) == 0 && (s.OrderType & 4) == 0 && s.OrderDateInfo.SeasonNum == statisticValue && s.CreatedDate.Year == year).ToList();
                            break;
                        case StatisticType.Year:
                            orderEntities = db.OrderSet.Include(o => o.OrderDateInfo).Include(o => o.OrderRepurchase).Include(o => o.OrderExtendInfo).Include(o => o.Consignee).Where(s => (s.OrderType & 1) == 0 && (s.OrderType & 4) == 0 && s.OrderDateInfo.Year == statisticValue).ToList();
                            break;
                        default:
                            break;
                    }
                    if (ServerName != OrderSource.ALL)
                        orderEntities = orderEntities.Where(o => o.Source == ServerName).ToList();
                    var glst = orderEntities.GroupBy(o => o.Consignee).OrderByDescending(g=>g.Count()).Take(20);
                    if (glst != null && glst.Any())
                    {
                        List<StatisticCustomer> lst = new List<StatisticCustomer>();
                        foreach (var item in glst)
                        {
                            StatisticCustomer statistic = new StatisticCustomer()
                            {
                                CreateDate = DateTime.Now,
                                Year = year,

                                Source = ServerName,
                                SourceDesc = Util.Helpers.Reflection.GetDescription<OrderSource>(ServerName),
                                StatisticType = (int)statisticType,
                                StatisticValue = statisticValue,
                                OrderCount = item.Count(),
                                ProductCount = item.Sum(o=>o.OrderExtendInfo.TotalProductCount),
                                Customer = item.Key,
                                Name = item.Key.Name,
                                Phone = item.Key.Phone

                            };
                            lst.Add(statistic);
                        }

                        var foo = db.StatisticCustomers.Where(s => s.Source == ServerName && s.StatisticType == (int)statisticType && s.StatisticValue == statisticValue&&s.Year==year);
                        if (foo == null)
                            db.Set<StatisticCustomer>().AddRange(lst);
                        else
                        {
                            db.Set<StatisticCustomer>().RemoveRange(foo);
                            db.Set<StatisticCustomer>().AddRange(lst);
                        }
                        db.SaveChanges();
                        OnUIMessageEventHandle($"{ServerName}-{statisticType.ToString()}-{statisticValue}-客户统计完毕");
                    }
                    else
                    {
                        OnUIMessageEventHandle($"{ServerName}-{statisticType.ToString()}-{statisticValue}-客户方面无统计结果");
                        
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

        private DataTable PushReport(StatisticType statisticType, int statisticValue, int year)
        {
            DataTable dt = new DataTable();
            using (var db = new OMSContext())
            {

                var lst = db.StatisticCustomers.Where(s => s.StatisticType == (int)statisticType && s.StatisticValue == statisticValue && s.Year == year && s.Source == ServerName);
                if (lst != null && lst.Any())
                {

                    #region create columns


                    dt.Columns.Add("店铺");
                    dt.Columns.Add("姓名");
                    dt.Columns.Add("手机号");
                    dt.Columns.Add("单数");
                    dt.Columns.Add("盒数");
                    dt.Columns.Add("报表时间");
                    #endregion
                    foreach (var item in lst)
                    {
                        var dr = dt.NewRow();
                        dr["店铺"] = item.Source;
                        dr["姓名"] = item.Name;

                        dr["手机号"] = item.Phone;
                        dr["单数"] = item.OrderCount;
                        dr["盒数"] = item.ProductCount;
                        dr["报表时间"] = item.CreateDate;
                       
                        dt.Rows.Add(dr);

                    }

                }
            }
            return dt;
        }
        public virtual DataTable PushDailyReport(DateTime value)
        {
            return PushReport(StatisticType.Day, value.DayOfYear, value.Year);
        }


        public virtual DataTable PushMonthReport(int monthNum, int year)
        {
            return PushReport(StatisticType.Month, monthNum, year);
        }

        public virtual DataTable PushSeasonReport(int seasonNum, int year)
        {
            return PushReport(StatisticType.Quarter, seasonNum, year);
        }

        public virtual DataTable PushWeekReport(int weekNum, int year)
        {
            return PushReport(StatisticType.Week, weekNum, year);
        }

        public virtual DataTable PushYearReport(int year)
        {
            return PushReport(StatisticType.Year, year, year);
        }
    }
    public class CustomerStatisticServerCommon : CustomerStatisticServerBase
    {


        public override string ServerName => Name;
        private String Name { get; set; }
        public CustomerStatisticServerCommon(string name)
        {
            Name = name;
        }

    }
}
