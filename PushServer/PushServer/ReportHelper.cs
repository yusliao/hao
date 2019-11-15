using System;

using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OMS.Models;


namespace PushServer
{
    class ReportHelper
    {
        
        public ReportHelper()
        {

        }
        public void CIBCustomerDistribution()
        {
            using (var db = new OMS.Models.OMSContext())
            {
                var total = db.OrderSet.Include(o => o.OrderDateInfo).Include(o => o.Consignee).Include(o=>o.OrderExtendInfo).Include(o=>o.ConsigneeAddress)
                   .Where(o => o.CreatedDate.Year == 2019&&o.OrderType==0);
                var cib = total
                    .Where(o => o.Source == OrderSource.CIB||o.Source==OrderSource.CIBAPP
                    ).Select(o=>o.Consignee.CustomerId).Distinct().ToList();
                var cibvip = total
                    .Where(o => o.Source == OrderSource.CIBVIP
                    ).Select(o=>o.Consignee.CustomerId).Distinct().ToList();
                Console.WriteLine("2019年兴业客户分布".PadRight(20,'*'));
                Console.WriteLine($"积分客户总人数：{cib.Count}");
                Console.WriteLine($"积点客户总人数：{cibvip.Count}");

                var unlst = cib.Union(cibvip);
                Console.WriteLine($"2019年兴业客户总人数：{unlst.Count()}");
                var intersect = cib.Intersect(cibvip);
                Console.WriteLine($"兴业客户积分积点都参与的总人数：{intersect.Count()}");
                Console.WriteLine("=".PadRight(20,'='));
                Console.WriteLine("各个平台销售情况：".PadRight(20,'*'));
                
                var totallst = total.GroupBy(o => o.Source);
                
                Console.WriteLine("平台名称\t\t总单数\t总盒数\t总人数");
                foreach (var item in totallst)
                {
                    var temp = total.Where(o => o.Source == item.Key);

                    Console.WriteLine($"{Util.Helpers.Reflection.GetDescription<OrderSource>(item.Key.ToUpper())}" +
                        $"\t{item.Count()}" +
                        $"\t{temp.Sum(o => o.OrderExtendInfo.TotalProductCount)}" +
                        $"\t{temp.GroupBy(o => o.Consignee).Count()}");
                    //Console.WriteLine($"{Util.Helpers.Reflection.GetDescription<OrderSource>(item.Key.ToUpper())}");
                    //Console.WriteLine($"\t{item.Count()}");
                    //Console.WriteLine($"\t{temp.Sum(o => o.OrderExtendInfo.TotalProductCount)}");
                    //Console.WriteLine($"\t{temp.GroupBy(o => o.Consignee).Count()}");
                }



               


            }
        }
    }
}
