﻿using FusionStone.WeiXin;

using OMS.Models;

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Data.Entity;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
namespace PushServer.Service
{
    public class StatisticServer
    {
        //[ImportMany(typeof(IProductStatisticServer))]
        //private IEnumerable<IProductStatisticServer> ProductStatisticServerOptSet { get; set; }
        //[ImportMany(typeof(IDistrictStatisticServer))]
        //private IEnumerable<IDistrictStatisticServer> DistrictStatisticServerOptSet { get; set; }
        //[ImportMany(typeof(IOrderStatisticServer))]
        //private IEnumerable<IOrderStatisticServer> OrderStatisticServerOptSet { get; set; }
        //[ImportMany(typeof(ICustomerStatisticServer))]
        //private IEnumerable<ICustomerStatisticServer> CustomerStatisticServerOptSet { get; set; }
        //[ImportMany(typeof(IPandianServer))]
        //private IEnumerable<IPandianServer> PandianStatisticServerOptSet { get; set; }
        private static readonly StatisticServer statisticServer = new StatisticServer();
        public static StatisticServer Instance { get { return statisticServer; } }
        public  event Action<string> ShowMessageEventHandle;
        private StatisticServer()
        {
            ProductStatisticServerBase.UIMessageEventHandle += ProductStatisticServerBase_UIMessageEventHandle;
            OrderStatisticServerBase.UIMessageEventHandle += ProductStatisticServerBase_UIMessageEventHandle;
            DistrictStatisticServerBase.UIMessageEventHandle += ProductStatisticServerBase_UIMessageEventHandle;
            #region MEF配置
            MyComposePart();
           
            #endregion
        }

        private void ProductStatisticServerBase_UIMessageEventHandle(string obj)
        {
            var handle = ShowMessageEventHandle;
            if (handle != null)
                handle.BeginInvoke(obj, null, null);
        }

        void MyComposePart()
        {
            var catalog = new AssemblyCatalog(Assembly.GetExecutingAssembly());
            var container = new CompositionContainer(catalog);
            //将部件（part）和宿主程序添加到组合容器
            container.ComposeParts(this);
        }

     
        /// <summary>
        /// 生成日报表
        /// </summary>
        public  void CreateDailyReport(DateTime dateTime)
        {
            /*生成昨日报表
             */
            #region 静态实现


            //foreach (var item in ProductStatisticServerOptSet)
            //{
            //    var result = item.CreateDailyReport(dateTime);
            //}
            //foreach (var item in OrderStatisticServerOptSet)
            //{
            //    var result = item.CreateDailyReport(dateTime);
            //}
            //foreach (var item in DistrictStatisticServerOptSet)
            //{
            //    var result = item.CreateDistrictDailyReport(dateTime);
            //}
            #endregion
            #region 动态方式加载
            //加载所有渠道
            
            foreach (var item in AppServer.Instance.ConfigDictionary.Values.Where(i => i.CreateReport != false))
            {
                ProductStatisticServerCommon pcomm = new ProductStatisticServerCommon(item.Name,item.Tag);
                pcomm.CreateDailyReport(dateTime);
                OrderStatisticServerCommon ocomm = new OrderStatisticServerCommon(item.Name, item.Tag);
                ocomm.CreateDailyReport(dateTime);
                DistrictStatisticServerCommon dcomm = new DistrictStatisticServerCommon(item.Name, item.Tag);
                dcomm.CreateDistrictDailyReport(dateTime);
                CustomerStatisticServerCommon ccomm = new CustomerStatisticServerCommon(item.Name, item.Tag);
                ccomm.CreateDailyReport(dateTime);

            }
            #endregion
            

        }
        public  void CreateWeekReport(int weeknum,int year)
        {
           
            //foreach (var item in ProductStatisticServerOptSet)
            //{
            //    var result = item.CreateWeekReport(weeknum,year);
            //}
            //foreach (var item in OrderStatisticServerOptSet)
            //{
            //    var result = item.CreateWeekReport(weeknum, year);
            //}
            //foreach (var item in DistrictStatisticServerOptSet)
            //{
            //    var result = item.CreateDistrictWeekReport(weeknum, year);
            //}
            #region 动态方式加载

            foreach (var item in AppServer.Instance.ConfigDictionary.Values.Where(i => i.CreateReport != false))
            {
                ProductStatisticServerCommon pcomm = new ProductStatisticServerCommon(item.Name, item.Tag);
                pcomm.CreateWeekReport(weeknum, year);
                OrderStatisticServerCommon ocomm = new OrderStatisticServerCommon(item.Name, item.Tag);
                ocomm.CreateWeekReport(weeknum, year);
                DistrictStatisticServerCommon dcomm = new DistrictStatisticServerCommon(item.Name, item.Tag);
                dcomm.CreateDistrictWeekReport(weeknum, year);
                CustomerStatisticServerCommon ccomm = new CustomerStatisticServerCommon(item.Name, item.Tag);
                ccomm.CreateWeekReport(weeknum, year);

            }
            #endregion


        }
        public bool CreateYearReport(int year)
        {


            #region 动态方式加载

            try
            {
                foreach (var item in AppServer.Instance.ConfigDictionary.Values.Where(i => i.CreateReport != false))
                {
                    ProductStatisticServerCommon pcomm = new ProductStatisticServerCommon(item.Name, item.Tag);
                    pcomm.CreateYearReport(year);
                    OrderStatisticServerCommon ocomm = new OrderStatisticServerCommon(item.Name, item.Tag);
                    ocomm.CreateYearReport(year);
                    DistrictStatisticServerCommon dcomm = new DistrictStatisticServerCommon(item.Name, item.Tag);
                    dcomm.CreateDistrictYearReport(year);
                    CustomerStatisticServerCommon ccomm = new CustomerStatisticServerCommon(item.Name, item.Tag);
                    ccomm.CreateYearReport(year);
                }
                Util.Logs.Log.GetLog(nameof(StatisticServer)).Info($"{year}创建年报表任务已提交");
                return true;
            }
            catch (Exception ex)
            {
                Util.Logs.Log.GetLog(nameof(Statistic)).Error($"历史报表{year}年生成失败。/r/n{ex.Message}");
                return false;
            }

            
            #endregion


        }
        /// <summary>
        /// 创建月订单报表
        /// </summary>
        /// <param name="monthnum"></param>
        public void CreateMonthReport(int monthnum,int year )
        {

            //foreach (var item in ProductStatisticServerOptSet)
            //{
            //    var result = item.CreateMonthReport(monthnum, year);
            //}
            //foreach (var item in OrderStatisticServerOptSet)
            //{
            //    var result = item.CreateMonthReport(monthnum, year);
            //}
            //foreach (var item in DistrictStatisticServerOptSet)
            //{
            //    var result = item.CreateDistrictMonthReport(monthnum, year);
            //}
            //foreach (var item in CustomerStatisticServerOptSet)
            //{
            //    var result = item.CreateMonthReport(monthnum, year);
            //}
            #region 动态方式加载

            foreach (var item in AppServer.Instance.ConfigDictionary.Values.Where(i => i.CreateReport != false))
            {
                ProductStatisticServerCommon pcomm = new ProductStatisticServerCommon(item.Name, item.Tag);
                pcomm.CreateMonthReport(monthnum, year);
                OrderStatisticServerCommon ocomm = new OrderStatisticServerCommon(item.Name, item.Tag);
                ocomm.CreateMonthReport(monthnum, year);
                DistrictStatisticServerCommon dcomm = new DistrictStatisticServerCommon(item.Name, item.Tag);
                dcomm.CreateDistrictMonthReport(monthnum, year);
                CustomerStatisticServerCommon ccomm = new CustomerStatisticServerCommon(item.Name, item.Tag);
                ccomm.CreateMonthReport(monthnum, year);

            }
            #endregion




        }
        /// <summary>
        /// 创建报表 
        /// </summary>
        /// <param name="dt"></param>
        /// <param name="isAll">true:重新生成当月所有的报表（维度：天，周，月）</param>
        /// <returns></returns>
        public  bool CreateReport(DateTime dt)
        {
            /*报表生成规则：
             * 今天生成昨天的日报表
             * 生成指定日期对应的周报表
             * 指定时间是当月月末的日期，则生成当月月报表
             */ 
            try
            {
                Task.Run(() =>
                {


                    CreateDailyReport(dt);

                    Util.Logs.Log.GetLog(nameof(StatisticServer)).Info($"{dt.ToString("yyyyMMdd HH:mm:ss")}日报表创建命令下达完毕");
                    CreateWeekReport(Util.Helpers.Time.GetWeekNum(dt), dt.Year);
                    Util.Logs.Log.GetLog(nameof(StatisticServer)).Info($"{Util.Helpers.Time.GetWeekNum(dt)}周报表创建命令下达完毕");


                    CreateMonthReport(dt.Month, dt.Year);//生成当前月报表
                    Util.Logs.Log.GetLog(nameof(StatisticServer)).Info($"{dt.Month}月报表创建命令下达完毕");
                    
                });

                return true;
            }
            catch (Exception ex)
            {
                Util.Logs.Log.GetLog(nameof(Statistic)).Error($"报表生成失败。/r/n{ex.Message}");
                return false;
            }
           
        }
        public bool CreateHistoryReport(int month,int year)
        {
            /*报表生成规则：
             * 今天生成昨天的日报表
             * 生成指定日期对应的周报表
             * 指定时间是当月月末的日期，则生成当月月报表
             */
            try
            {
                int end = new DateTime(year, month + 1, 1).AddDays(-1).Day;
                for (int i = 0; i < end; i++)
                {
                    var foo = new DateTime(year, month, i + 1);
                    CreateDailyReport(foo);
                    if (foo.DayOfWeek == DayOfWeek.Sunday)
                    {
                        CreateWeekReport(Util.Helpers.Time.GetWeekNum(foo), foo.Year);
                        Util.Logs.Log.GetLog(nameof(StatisticServer)).Info($"{year}-{Util.Helpers.Time.GetWeekNum(foo)}周报表创建完毕");
                    }

                }

                CreateMonthReport(month, year);//生成当前月报表
                Util.Logs.Log.GetLog(nameof(StatisticServer)).Info($"{year}-{month}创建月报表任务已提交");
                

                return true;
            }
            catch (Exception ex)
            {
                Util.Logs.Log.GetLog(nameof(Statistic)).Error($"历史报表{year}年{month}月生成失败。/r/n{ex.Message}");
                return false;
            }

        }
       

        //public  bool PushPandianReport(int monthNum,string pandianFolder)
        //{
        //    var lst = Instance.PandianStatisticServerOptSet.ToList();
        //    foreach (var item in lst)
        //    {

        //        System.Threading.ThreadPool.QueueUserWorkItem(o =>
        //        {
        //            var dt = item.PushMonthReport(monthNum, DateTime.Now.Year);
        //            if (dt != null && dt.Rows.Count > 0)
        //            {
        //                var filename = System.IO.Path.Combine(pandianFolder, "pandian", $"ERP-{item.ServerName}-{monthNum}月份盘点订单{DateTime.Now.ToString("yyyyMMddHHmmss")}.xlsx");
        //                NPOIExcel.Export(dt, filename);
        //                if (Environment.UserInteractive)
        //                {
        //                    Console.WriteLine($"ERP-{item.ServerName}-{monthNum}月份盘点订单生成成功。文件名:{filename}");
        //                }
        //            }
        //        });
        //    }
        //        //按渠道生成对账单
        //    var prolst = Instance.ProductStatisticServerOptSet.ToList();
        //    foreach (var pro in prolst)
        //    {

        //        System.Threading.ThreadPool.QueueUserWorkItem(o =>
        //        {
        //            var dt = pro.PushMonthReport(monthNum, DateTime.Now.Year);
        //            if (dt != null && dt.Rows.Count > 0)
        //            {
        //                var filename = System.IO.Path.Combine(pandianFolder, "pandian", $"ERP-{pro.ServerName}-{monthNum}月份盘点订单{DateTime.Now.ToString("yyyyMMddHHmmss")}.xlsx");
        //                NPOIExcel.Export(dt, filename);
        //                if (Environment.UserInteractive)
        //                {
        //                    Console.WriteLine($"ERP-{pro.ServerName}-{monthNum}月份盘点订单生成成功。文件名:{filename}");
        //                }


        //            }
        //        });

        //    }


        //    return true;
        //}
        /// <summary>
        /// 报表推送
        /// </summary>
        /// <param name="serverNames">可推送的条目列表</param>
        /// <returns></returns>
        public bool PushDailyReport(string[] serverNames,DateTime dateTime)
        {
            /*推送报表规则
             * 推送OrderSource对象指定的条目的推送报表
             * 推送昨天的日报表
             * 如果今天是周一，推送上周的周报表
             * 如果今天是月初，推送上月月报表
             */ 

            //foreach (var item in serverNames)
            //{
            //    if (item == OrderSource.CIB)//CIB与CIBAPP合并发送
            //        continue;
            //    var dt = dateTime;
            //    OrderStatisticServerOptSet.FirstOrDefault(i=>i.ServerName==item)?.PushDailyReport(dt);

            //}
            var lst = AppServer.Instance.ConfigDictionary.Where(p => serverNames.Contains(p.Key)).Select(p=>p.Value);
            foreach (var item in lst)
            {
                if (item.Name == OrderSource.CIB||item.Name==OrderSource.CIBEVT)//CIB,CIBAPP,CIBEVT合并发送
                    continue;
                OrderStatisticServerCommon ocomm = new OrderStatisticServerCommon(item.Name, item.Tag);
                ocomm.PushDailyReport(dateTime);

            }
            return true;
        }
        public bool PushWeeklyReport(string[] serverNames, DateTime dateTime)
        {
            /*推送报表规则
             * 推送OrderSource对象指定的条目的推送报表
             * 推送昨天的日报表
             * 如果今天是周一，推送上周的周报表
             * 如果今天是月初，推送上月月报表
             */

            //foreach (var item in serverNames)
            //{
            //    if (item == OrderSource.CIB)//CIB与CIBAPP合并发送
            //        continue;
            //    var dt = dateTime;

            //    OrderStatisticServerOptSet.FirstOrDefault(i => i.ServerName == item)?.PushWeekReport(Util.Helpers.Time.GetWeekNum(dt), dt.Year);

            //}

            var lst = AppServer.Instance.ConfigDictionary.Where(p => serverNames.Contains(p.Key)).Select(p => p.Value);
            foreach (var item in lst)
            {
                if (item.Name == OrderSource.CIB || item.Name == OrderSource.CIBEVT)//CIB,CIBAPP,CIBEVT合并发送
                    continue;
                OrderStatisticServerCommon ocomm = new OrderStatisticServerCommon(item.Name, item.Tag);
                ocomm.PushWeekReport(Util.Helpers.Time.GetWeekNum(dateTime), dateTime.Year);

            }
            return true;
        }
        public bool PushMonthlyReport(string[] serverNames, DateTime dateTime)
        {
            /*推送报表规则
             * 推送OrderSource对象指定的条目的推送报表
             * 推送昨天的日报表
             * 如果今天是周一，推送上周的周报表
             * 如果今天是月初，推送上月月报表
             */

            //foreach (var item in serverNames)
            //{
            //    if (item == OrderSource.CIB)//CIB与CIBAPP合并发送
            //        continue;
            //    var dt = dateTime;

            //    OrderStatisticServerOptSet.FirstOrDefault(i => i.ServerName == item)?.PushMonthReport(dt.Month, dt.Year);

            //}

            var lst = AppServer.Instance.ConfigDictionary.Where(p => serverNames.Contains(p.Key)).Select(p => p.Value);
            foreach (var item in lst)
            {
                if (item.Name == OrderSource.CIB || item.Name == OrderSource.CIBEVT)//CIB,CIBAPP,CIBEVT合并发送
                    continue;
                var dt = dateTime;
                OrderStatisticServerCommon ocomm = new OrderStatisticServerCommon(item.Name, item.Tag);
                ocomm.PushMonthReport(dt.Month, dt.Year);

            }

            return true;
        }
        public bool PushReport(string[] serverNames, DateTime dateTime,StatisticType statisticType)
        {
            switch (statisticType)
            {
                case StatisticType.Day:
                    PushDailyReport(serverNames, dateTime);
                    break;
                case StatisticType.Week:
                    PushWeeklyReport(serverNames, dateTime);
                    break;
                case StatisticType.Month:
                    PushMonthlyReport(serverNames, dateTime);
                    break;
                case StatisticType.Quarter:
                    break;
                case StatisticType.Year:
                    break;
                default:
                    break;
            }
           
            return true;
        }

        //public  bool CreatePandianReport(int monthNum)
        //{
        //    var lst = Instance.PandianStatisticServerOptSet.ToList();
        //    foreach (var item in lst)
        //    {
        //        System.Threading.ThreadPool.QueueUserWorkItem(o =>
        //        {
        //            try
        //            {
        //                item.CreateMonthReport(monthNum, DateTime.Now.Year);
        //            }
        //            catch (Exception ex)
        //            {
        //                Util.Logs.Log.GetLog($"生成盘点报表失败，来源:{item.ServerName},message:{ex.Message},StackTrace:{ex.StackTrace}");

        //            }

        //        });
        //    }

        //    return true;


        //}
    }
    public enum StatisticType
    {
        Day=1,
        Week,
        Month,
        Quarter,
        Year
    }
}
