using OMS.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
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
            OnUIMessageEventHandle($"{ServerName}-{statisticType.ToString()}-{statisticValue}-商品报表开始统计");
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
                                SKU = item.Key.sku,
                                ProductTotalAmount = item.Sum(o => o.TotalAmount),
                                ProductTotalCostAmount = item.Sum(o=>o.TotalCostPrice),
                                ProductTotalFlatAmount = item.Sum(o=>o.TotalFlatAmount),
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
                            //复购统计
                            switch (statisticType)
                            {
                                case StatisticType.Day:
                                    DateTime start = new DateTime(year, 1, 1).AddDays(statisticValue - 1);
                                    statistic.CreateDate = start;
                                    statistic.TotalOrderRepurchase = lst.Where(o => o.OrderRepurchase.DailyRepurchase == true).Count();
                                    statistic.TotalCustomerRepurchase = lst.Where(o => o.OrderRepurchase.DailyRepurchase == true).GroupBy(o => o.Consignee).ToList().Count();
                                    statistic.TotalProductRepurchase = lst.Where(o => o.OrderRepurchase.DailyRepurchase == true).SelectMany(o=>o.Products.Where(P=>P.sku==item.Key.sku)).Sum(p => p.ProductCount);
                                    break;
                                case StatisticType.Week:
                                    statistic.TotalOrderRepurchase = lst.Where(o => o.OrderRepurchase.WeekRepurchase == true).Count();
                                    statistic.TotalCustomerRepurchase = lst.Where(o => o.OrderRepurchase.WeekRepurchase == true).GroupBy(o => o.Consignee).ToList().Count();
                                    statistic.TotalProductRepurchase = lst.Where(o => o.OrderRepurchase.WeekRepurchase == true).SelectMany(o => o.Products.Where(P => P.sku == item.Key.sku)).Sum(p => p.ProductCount);
                                    break;
                                case StatisticType.Month:
                                    statistic.TotalOrderRepurchase = lst.Where(o => o.OrderRepurchase.MonthRepurchase == true).Count();
                                    statistic.TotalCustomerRepurchase = lst.Where(o => o.OrderRepurchase.MonthRepurchase == true).GroupBy(o => o.Consignee).ToList().Count();
                                    statistic.TotalProductRepurchase = lst.Where(o => o.OrderRepurchase.MonthRepurchase == true).SelectMany(o => o.Products.Where(P => P.sku == item.Key.sku)).Sum(p => p.ProductCount);
                                    break;
                                case StatisticType.Quarter:
                                    statistic.TotalOrderRepurchase = lst.Where(o => o.OrderRepurchase.SeasonRepurchase == true).Count();
                                    statistic.TotalCustomerRepurchase = lst.Where(o => o.OrderRepurchase.SeasonRepurchase == true).GroupBy(o => o.Consignee).ToList().Count();
                                    statistic.TotalProductRepurchase = lst.Where(o => o.OrderRepurchase.SeasonRepurchase == true).ToList().SelectMany(o => o.Products.Where(P => P.sku == item.Key.sku)).Sum(p => p.ProductCount);
                                    break;
                                case StatisticType.Year:
                                    statistic.TotalOrderRepurchase = lst.Where(o => o.OrderRepurchase.YearRepurchase == true).Count();
                                    statistic.TotalCustomerRepurchase = lst.Where(o => o.OrderRepurchase.YearRepurchase == true).GroupBy(o => o.Consignee).ToList().Count();
                                    statistic.TotalProductRepurchase = lst.Where(o => o.OrderRepurchase.YearRepurchase == true).SelectMany(o => o.Products.Where(P => P.sku == item.Key.sku)).Sum(p => p.ProductCount);
                                    break;
                                default:
                                    break;
                            }
                            
                            smp.Add(statistic);
                        }


                        //foreach (var item in smp)
                        //{
                        //    OnUIMessageEventHandle($"{item.SourceDesc}\t{item.ProductPlatName}\t{item.weightCodeDesc}\t{item.ProductTotalWeight}\t{item.ProductTotalAmount}\t{item.ProductCount}");
                        //}
                       
                        var removeobj = db.StatisticProductSet.Where(s => s.Year == year && s.Source == ServerName && s.StatisticType == (int)statisticType && s.StatisticValue==statisticValue);
                        
                        if (removeobj.Any())
                        {
                            
                            //bool saveFailed;
                            //do
                            //{
                            //    saveFailed = false;
                            //    try
                            //    {
                            //        db.SaveChanges();
                            //    }
                            //    catch (Db ex)
                            //    {

                            //        saveFailed = true;

                            //        // Update original values from the database
                            //        var entry = ex.Entries.Single();
                            //        entry.OriginalValues.SetValues(entry.GetDatabaseValues());
                            //    }

                            //} while (saveFailed);


                            db.Set<StatisticProduct>().RemoveRange(removeobj);
                            db.SaveChanges();

                        }
                      
                        db.Set<StatisticProduct>().AddRange(smp);
                        db.SaveChanges();
                        
                        OnUIMessageEventHandle($"{ServerName}-{statisticType.ToString()}-{statisticValue}-统计完毕");
                    }
                    else
                    {
                        OnUIMessageEventHandle($"{ServerName}-{statisticType.ToString()}-{statisticValue}-产品方面无统计结果");
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
