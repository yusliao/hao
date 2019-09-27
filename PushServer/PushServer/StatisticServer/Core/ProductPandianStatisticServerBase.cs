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
    public abstract class ProductPandianStatisticServerBase : IPandianServer
    {
        public abstract string ServerName { get; }
        public static event Action<string> UIMessageEventHandle;
        protected virtual void OnUIMessageEventHandle(string msg)
        {
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
            

            using (var db = new OMSContext())
            {
               
               

                var lst = db.OrderPandianProductInfoSet.Where(s => s.MonthNum == statisticValue && s.Year == year).ToList();
                var q = lst.GroupBy(p => new { p.ProductPlatName, p.sku, p.weightCode, p.weightCodeDesc, p.MonthNum });
                List<StatisticProduct> smp = new List<StatisticProduct>();
                foreach (var item in q)
                {

                    StatisticProduct statistic = new StatisticProduct()
                    {

                        Source = ServerName,
                        SourceDesc = Util.Helpers.Reflection.GetDescription<OrderSource>(ServerName),
                        Year = year,
                        ProductPlatName = item.Key.ProductPlatName,

                        ProductTotalAmount = item.Sum(o => o.TotalAmount),
                        weightCode = item.Key.weightCode,
                        weightCodeDesc = item.Key.weightCodeDesc,
                        ProductTotalWeight = item.Sum(o => o.ProductWeight) / 1000,
                        ProductCount = item.Sum(o => o.ProductCount),
                        CreateDate = DateTime.Now,
                        StatisticType = (int)StatisticType.Month,
                        StatisticValue = statisticValue

                    };


                    smp.Add(statistic);
                }
                if (Environment.UserInteractive)
                {
                    foreach (var item in smp)
                    {
                        Console.WriteLine($"{item.SourceDesc}\t{item.ProductPlatName}\t{item.weightCodeDesc}\t{item.ProductTotalWeight}\t{item.ProductTotalAmount}\t{item.ProductCount}");
                    }
                }
                var removeobj = db.StatisticProductSet.Where(s => s.Year == year && s.Source == ServerName && s.StatisticType == (int)StatisticType.Month && s.StatisticValue == statisticValue);
                db.Set<StatisticProduct>().RemoveRange(removeobj);
                db.SaveChanges();
                db.Set<StatisticProduct>().AddRange(smp);
                db.SaveChanges();
            }
            return true;

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

                    //dt.Columns.Add("总人数");
                    //dt.Columns.Add("总复购人数");
                    //dt.Columns.Add("总订单数");
                    //dt.Columns.Add("总订单复购人数");
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

                        //dr["总人数"] = item.TotalCustomers;
                        //dr["总复购人数"] = item.TotalOrderRepurchase;
                        //dr["总订单数"] = item.TotalOrders;
                        //dr["总订单复购人数"] = item.TotalOrderRepurchase;
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
