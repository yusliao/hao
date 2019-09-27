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
    public abstract class ProductStatisticServerBase : IProductStatisticServer
    {
        public abstract string ServerName { get; }
        public static event Action<string> UIMessageEventHandle;
        protected virtual void OnUIMessageEventHandle(string msg)
        {
            Util.Logs.Log.GetLog(nameof(ProductStatisticServerBase)).Debug(msg);
            var handle = UIMessageEventHandle;
            if (handle != null)
                handle(msg);
        }

        public virtual bool CreateDailyReport(DateTime value)
        {
            return CreateReport(StatisticType.Day, value.DayOfYear, value.Year);
        }

        public virtual bool CreateMonthReport(int monthnum,int year)
        {
            return CreateReport(StatisticType.Month, monthnum, year);
        }
        private bool CreateReport(StatisticType statisticType, int statisticValue,int year)
        {
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

                            orderEntities = db.OrderSet.Include(o=>o.Consignee).Include(o=>o.OrderDateInfo).Include(o => o.OrderRepurchase).Include(o => o.Products).Where(s=> s.OrderType==0 && s.OrderDateInfo.CreateTime >= start && s.OrderDateInfo.CreateTime < end && s.CreatedDate.Year == year ).ToList();
                            break;
                        case StatisticType.Week:
                            orderEntities = db.OrderSet.Include(o => o.Consignee).Include(o => o.OrderDateInfo).Include(o => o.OrderRepurchase).Include(o => o.Products).Where(s => s.OrderType == 0 && s.OrderDateInfo.WeekNum == statisticValue && s.CreatedDate.Year == year ).ToList();
                            break;
                        case StatisticType.Month:
                            orderEntities = db.OrderSet.Include(o => o.Consignee).Include(o => o.OrderDateInfo).Include(o => o.OrderRepurchase).Include(o => o.Products).Where(s => s.OrderType == 0 && s.OrderDateInfo.MonthNum == statisticValue && s.CreatedDate.Year == year ).ToList();
                            break;
                        case StatisticType.Quarter:
                            orderEntities = db.OrderSet.Include(o => o.Consignee).Include(o => o.OrderDateInfo).Include(o => o.OrderRepurchase).Include(o => o.Products).Where(s => s.OrderType == 0  && s.OrderDateInfo.SeasonNum == statisticValue && s.CreatedDate.Year == year ).ToList();
                            break;
                        case StatisticType.Year:
                            orderEntities = db.OrderSet.Include(o => o.Consignee).Include(o => o.OrderDateInfo).Include(o => o.OrderRepurchase).Include(o => o.Products).Where(s => s.OrderType == 0  && s.OrderDateInfo.Year == statisticValue).ToList();
                            break;
                        default:
                            break;
                    }
                    if (ServerName != OrderSource.ALL)
                        orderEntities = orderEntities.Where(o => o.Source == ServerName).ToList();
                    if (orderEntities.Any())
                    {
                      
                        var plst = orderEntities.SelectMany(o =>  o.Products).Where(o=>o.sku!=null);

                        var q = plst.GroupBy(p => new {   p.weightCode ,p.sku });
                        List<StatisticProduct> smp = new List<StatisticProduct>();
                        foreach (var item in q)
                        {
                            var lst = orderEntities.Where(o => o.Products.Any(p => p.sku == item.Key.sku));
                            StatisticProduct statistic = new StatisticProduct()
                            {

                                Source = ServerName,
                                SourceDesc = Util.Helpers.Reflection.GetDescription<OrderSource>(ServerName),
                                Year = year,
                                ProductPlatName = db.ProductsSet.Find(item.Key.sku)?.ShortName??db.ProductsSet.Find(item.Key.sku)?.Name,

                                ProductTotalAmount = item.Sum(o => o.TotalAmount),
                                weightCode = item.Key.weightCode,
                                weightCodeDesc = db.WeightCodeSet.Find(item.Key.weightCode)?.Value.ToString(),
                                ProductTotalWeight = item.Sum(o => o.ProductWeight)/1000,
                                ProductCount = item.Sum(o => o.ProductCount),
                                CreateDate = DateTime.Now,
                                StatisticType = (int)statisticType,
                                StatisticValue = statisticValue,
                                TotalCustomers = lst.GroupBy(o=>o.Consignee).Count(),//总计客户数量
                                TotalOrders = lst.Count()
                            };
                                    
                                    
                            statistic.TotalOrderRepurchase = lst.Where(o => o.OrderRepurchase.DailyRepurchase == true).Count();
                            statistic.TotalCustomerRepurchase = lst.Where(o => o.OrderRepurchase.DailyRepurchase == true).GroupBy(o => o.Consignee).ToList().Count();
                            var RepurchaseCount = lst.Where(o => o.OrderRepurchase.DailyRepurchase == true).Count();
                            if (RepurchaseCount != 0)
                            {
                                statistic.TotalProductRepurchase = RepurchaseCount;
                            }
                            else
                                statistic.TotalProductRepurchase = 0;
                            
                            
                            smp.Add(statistic);
                        }


                        //foreach (var item in smp)
                        //{
                        //    OnUIMessageEventHandle($"{item.SourceDesc}\t{item.ProductPlatName}\t{item.weightCodeDesc}\t{item.ProductTotalWeight}\t{item.ProductTotalAmount}\t{item.ProductCount}");
                        //}
                       
                        var removeobj = db.StatisticProductSet.Where(s => s.Year == year && s.Source == ServerName && s.StatisticType == (int)statisticType && s.StatisticValue==statisticValue);
                        db.Set<StatisticProduct>().RemoveRange(removeobj);
                        db.SaveChanges();
                        db.Set<StatisticProduct>().AddRange(smp);
                        db.SaveChanges();
                        OnUIMessageEventHandle($"{ServerName}渠道-{statisticType}-{statisticValue}报表生成完毕");
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
                Util.Logs.Log.GetLog(nameof(ProductStatisticServerBase)).Error($"{ServerName}:error:{ex.Message},statck:{ex.StackTrace}");
                return false;
            }
            
        }

        public virtual bool CreateSeasonReport(int seasonnum,int year)
        {
            return CreateReport(StatisticType.Quarter, seasonnum, year);
        }

        public virtual bool CreateWeekReport(int weeknum, int year)
        {
            return CreateReport(StatisticType.Week, weeknum, year);
        }

        public virtual bool CreateYearReport(int year)
        {
            return CreateReport(StatisticType.Year,year,year);
        }
        private DataTable PushReport(StatisticType statisticType,int statisticValue,int year)
        {
            DataTable dt = new DataTable();
            using (var db = new OMSContext())
            {
              
                var lst = db.StatisticProductSet.Where(s => s.StatisticType == (int)statisticType && s.StatisticValue == statisticValue && s.Year == year && s.Source == ServerName);
                if (lst != null && lst.Any())
                {

                    #region create columns


                    dt.Columns.Add("店铺");
                    dt.Columns.Add("商品名称");
                    dt.Columns.Add("重量规格代码");
                    dt.Columns.Add("重量规格名称");
                    dt.Columns.Add("数量");
                    dt.Columns.Add("价格");
                    dt.Columns.Add("商品重量");
                    #endregion
                    foreach (var item in lst)
                    {
                        var dr = dt.NewRow();
                        dr["店铺"] = item.SourceDesc;
                        dr["商品名称"] = item.ProductPlatName;

                        dr["重量规格代码"] = item.weightCode;
                        dr["重量规格名称"] = item.weightCodeDesc;
                        dr["数量"] = item.ProductCount;
                        dr["价格"] = item.ProductTotalAmount;
                        dr["商品重量"] = item.ProductTotalWeight;
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
}
