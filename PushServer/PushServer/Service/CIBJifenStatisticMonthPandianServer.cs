using OMS.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.Entity;
using System.Data;
using Util;
using System.ComponentModel.Composition;

namespace PushServer.Service
{
    [Export(typeof(IPandianServer))]
    public class CIBJifenStatisticMonthPandianServer : IPandianServer
    {
        public string ServerName => OrderSource.CIBJifenPanDian;

        public  bool CreateMonthPandianReport(int monthnum, int year)
        {

            using (var db = new OMSContext())
            {

                
                var lst = db.OrderPandianProductInfoSet.Where(s => s.MonthNum==monthnum&&s.Year==year).ToList();
                var q = lst.GroupBy(p => new { p.ProductPlatName, p.sku, p.weightCode,p.weightCodeDesc, p.MonthNum });
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
                        StatisticValue = monthnum
                       
                    };


                    smp.Add(statistic);
                }
                if(Environment.UserInteractive)
                {
                    foreach (var item in smp)
                    {
                        Console.WriteLine($"{item.SourceDesc}\t{item.ProductPlatName}\t{item.weightCodeDesc}\t{item.ProductTotalWeight}\t{item.ProductTotalAmount}\t{item.ProductCount}");
                    }
                }
                var removeobj = db.StatisticProductSet.Where(s => s.Year == year && s.Source == ServerName && s.StatisticType == (int)StatisticType.Month && s.StatisticValue == monthnum);
                db.Set<StatisticProduct>().RemoveRange(removeobj);
                db.SaveChanges();
                db.Set<StatisticProduct>().AddRange(smp);
                db.SaveChanges();
            }
            return true;
        }
        public  DataTable PushPandianReport(int monthNum,int year)
        {
            DataTable dt = new DataTable();
            using (var db = new OMSContext())
            {
                var lst = db.StatisticProductSet.Where(s => s.StatisticValue == monthNum && s.Source == ServerName&&s.Year==year);
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
    }
}
