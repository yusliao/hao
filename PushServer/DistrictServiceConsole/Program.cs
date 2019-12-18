using DistrictService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentScheduler;
using System.Data.Entity;
using OMS.Models;

namespace DistrictServiceConsole
{
    class Program
    {
        static void Main(string[] args)
        {

            //while (true)
            //{
            //    Console.WriteLine("请选择操作：1：加密；2：解密");
            //    string str = Console.ReadLine();
            //    switch (str)
            //    {
            //        case "1":
            //            Console.WriteLine("请输入要加密的字符串");
            //            str = Console.ReadLine();
            //            if(!string.IsNullOrEmpty(str))
            //                Console.WriteLine($"加密后的字符串为：\r\n{Util.Helpers.Encrypt.AesEncrypt(str)}");
            //            else
            //                Console.WriteLine("输入不正确请重新输入");
            //            break;
            //        case "2":
            //            Console.WriteLine("请输入要解密的字符串");
            //            str = Console.ReadLine();
            //            if (!string.IsNullOrEmpty(str))
            //                Console.WriteLine($"解密后的字符串为：\r\n{Util.Helpers.Encrypt.AesDecrypt(str)}");
            //            else
            //                Console.WriteLine("输入不正确请重新输入");
            //            break;
            //        default:
            //            Console.WriteLine("输入不正确请重新输入");
            //            break;
            //    }
            //}
            Program.Encrypt();
            //DateTime dt = new DateTime(2019, 9, 11);
            //int i = dt.DayOfYear;
            //dt = new DateTime(2019, 9, 27);
            //i = dt.DayOfYear;
          
            //    string str = "山东济宁太白湖区许庄街道豪庭御都小区";


            //    var d = DistrictService.DistrictService.ResolveAddress(str.Trim());
            //    Console.WriteLine(d.ToString());

       
            Console.ReadLine();



        }

        private static void JobManager_JobEnd(JobEndInfo obj)
        {
            if(obj.Name=="lala")
                Console.WriteLine("good");
            else if(obj.Name== "lala2")
                Console.WriteLine("lala2");
            else
                Console.WriteLine("bad");
        }

        private static void Encrypt()
        {
            using (var db= new OMSContext())
            {
                foreach (var item in db.CustomersSet.AsParallel())
                {
                    if(!string.IsNullOrEmpty(item.Name))
                        item.Name = Util.Helpers.Encrypt.AesEncrypt(item.Name);
                    if (!string.IsNullOrEmpty(item.Phone))
                        item.Phone = Util.Helpers.Encrypt.AesEncrypt(item.Phone);
                    if (!string.IsNullOrEmpty(item.Phone2))
                        item.Phone2 = Util.Helpers.Encrypt.AesEncrypt(item.Phone2);
                    if(!string.IsNullOrEmpty(item.PersonCard))
                        item.PersonCard = Util.Helpers.Encrypt.AesEncrypt(item.PersonCard);
                    Console.WriteLine($"{item.Name}\t加密完毕");
                }
                foreach (var item in db.AddressSet.AsParallel())
                {
                    if(!string.IsNullOrEmpty(item.Address))
                        item.Address = Util.Helpers.Encrypt.AesEncrypt(item.Address);
                    Console.WriteLine($"{item.Address}  \t加密完毕");
                }
               
                db.SaveChanges();
                Console.WriteLine("更新完毕");
            }
            
        }
    }
    public class OMSContext : DbContext
    {
        public OMSContext() : base("name=papa")
        {
            Database.SetInitializer<OMSContext>(null);
            //  Database.SetInitializer<OMSContext>(new CreateDatabaseIfNotExists<OMSContext>());
            //   Database.SetInitializer<OMSContext>(new DropCreateDatabaseIfModelChanges<OMSContext>());
            // Database.SetInitializer(new MigrateDatabaseToLatestVersion<DistrictServiceContext, PushServer.Migrations.Configuration>());
        }
      
        public IDbSet<AddressEntity> AddressSet { get; set; }
        public IDbSet<ProductEntity> ProductsSet { get; set; }
        public IDbSet<CustomerEntity> CustomersSet { get; set; }
        public IDbSet<ProductDictionary> ProductDictionarySet { get; set; }
    }
}
