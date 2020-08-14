using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentScheduler;
namespace PushServer.JobServer
{
    public class PushJob:Registry
    {
        public PushJob()
        {
            //生成每天统计报表
            //Schedule(() =>
            //{
            //    Util.Logs.Log.GetLog(nameof(PushJob)).Info("每天12点定时报表统计业务正在生成统计报表...");
              
            //    AppServer.CreateReport(DateTime.Now.AddDays(-1));
            //}).WithName("CreateReport1").ToRunEvery(1).Days().At(12, 0);
            Schedule(() =>
            {
                Util.Logs.Log.GetLog(nameof(PushJob)).Info("周一12点定时报表统计业务正在生成统计报表...");

                AppServer.CreateReport(DateTime.Now.AddDays(-1));
            }).WithName("CreateDailyReport1").ToRunEvery(0).Weeks().On(DayOfWeek.Monday).At(12, 0);
            Schedule(() =>
            {
                Util.Logs.Log.GetLog(nameof(PushJob)).Info("周二12点定时报表统计业务正在生成统计报表...");

                AppServer.CreateReport(DateTime.Now.AddDays(-1));
            }).WithName("CreateDailyReport2").ToRunEvery(0).Weeks().On(DayOfWeek.Tuesday).At(12, 0);
            Schedule(() =>
            {
                Util.Logs.Log.GetLog(nameof(PushJob)).Info("周三12点定时报表统计业务正在生成统计报表...");

                AppServer.CreateReport(DateTime.Now.AddDays(-1));
            }).WithName("CreateDailyReport3").ToRunEvery(0).Weeks().On(DayOfWeek.Wednesday).At(12, 0);
            Schedule(() =>
            {
                Util.Logs.Log.GetLog(nameof(PushJob)).Info("周四12点定时报表统计业务正在生成统计报表...");

                AppServer.CreateReport(DateTime.Now.AddDays(-1));
            }).WithName("CreateDailyReport4").ToRunEvery(0).Weeks().On(DayOfWeek.Thursday).At(12, 0);
            Schedule(() =>
            {
                Util.Logs.Log.GetLog(nameof(PushJob)).Info("周五12点定时报表统计业务正在生成统计报表...");

                AppServer.CreateReport(DateTime.Now.AddDays(-1));
            }).WithName("CreateDailyReport5").ToRunEvery(0).Weeks().On(DayOfWeek.Friday).At(12, 0);
            Schedule(() =>
            {
                Util.Logs.Log.GetLog(nameof(PushJob)).Info("周六12点定时报表统计业务正在生成统计报表...");

                AppServer.CreateReport(DateTime.Now.AddDays(-1));
            }).WithName("CreateDailyReport6").ToRunEvery(0).Weeks().On(DayOfWeek.Saturday).At(19, 0);
            Schedule(() =>
            {
                Util.Logs.Log.GetLog(nameof(PushJob)).Info("周日12点定时报表统计业务正在生成统计报表...");

                AppServer.CreateReport(DateTime.Now.AddDays(-1));
            }).WithName("CreateDailyReport7").ToRunEvery(0).Weeks().On(DayOfWeek.Sunday).At(19, 0);

            //推送每天报表
            //Schedule(() =>
            //{
            //    AppServer.PushDailyReport(DateTime.Now.AddDays(-1));
            //    Util.Logs.Log.GetLog(nameof(PushJob)).Info("每天14点定时报表推送完毕");
            //}).WithName("PushDailyReport").ToRunEvery(1).Days().At(14, 0);
            Schedule(() =>
            {
                AppServer.PushDailyReport(DateTime.Now.AddDays(-1));
                Util.Logs.Log.GetLog(nameof(PushJob)).Info("周一14点定时报表推送完毕");
            }).WithName("PushDailyReport1").ToRunEvery(0).Weeks().On(DayOfWeek.Monday).At(14, 0);
            Schedule(() =>
            {
                AppServer.PushDailyReport(DateTime.Now.AddDays(-1));
                Util.Logs.Log.GetLog(nameof(PushJob)).Info("周二14点定时报表推送完毕");
            }).WithName("PushDailyReport2").ToRunEvery(0).Weeks().On(DayOfWeek.Tuesday).At(14, 0);
            Schedule(() =>
            {
                AppServer.PushDailyReport(DateTime.Now.AddDays(-1));
                Util.Logs.Log.GetLog(nameof(PushJob)).Info("周三14点定时报表推送完毕");
            }).WithName("PushDailyReport3").ToRunEvery(0).Weeks().On(DayOfWeek.Wednesday).At(14, 0);
            Schedule(() =>
            {
                AppServer.PushDailyReport(DateTime.Now.AddDays(-1));
                Util.Logs.Log.GetLog(nameof(PushJob)).Info("周四14点定时报表推送完毕");
            }).WithName("PushDailyReport4").ToRunEvery(0).Weeks().On(DayOfWeek.Thursday).At(14, 0);
            Schedule(() =>
            {
                AppServer.PushDailyReport(DateTime.Now.AddDays(-1));
                Util.Logs.Log.GetLog(nameof(PushJob)).Info("周五14点定时报表推送完毕");
            }).WithName("PushDailyReport5").ToRunEvery(0).Weeks().On(DayOfWeek.Friday).At(14, 0);
            Schedule(() =>
            {
                AppServer.PushDailyReport(DateTime.Now.AddDays(-1));
                Util.Logs.Log.GetLog(nameof(PushJob)).Info("周六20点定时报表推送完毕");
            }).WithName("PushDailyReport6").ToRunEvery(0).Weeks().On(DayOfWeek.Saturday).At(20, 0);
            Schedule(() =>
            {
                AppServer.PushDailyReport(DateTime.Now.AddDays(-1));
                Util.Logs.Log.GetLog(nameof(PushJob)).Info("周日20点定时报表推送完毕");
            }).WithName("PushDailyReport7").ToRunEvery(0).Weeks().On(DayOfWeek.Sunday).At(20, 0);



            Schedule(() =>
            {
                AppServer.PushWeeklyReport(DateTime.Now.AddDays(-1));
                Util.Logs.Log.GetLog(nameof(PushJob)).Info("每周一15点定时周报表推送完毕");
            }).WithName("PushWeeklyReport").ToRunEvery(1).Weeks().On(DayOfWeek.Monday).At(15,0);
            Schedule(() =>
            {
                AppServer.PushMonthlyReport(DateTime.Now.AddDays(-1));
                Util.Logs.Log.GetLog(nameof(PushJob)).Info("每月一号16点定时月报表推送完毕");
            }).WithName("PushMonthlyReport").ToRunEvery(1).Months().On(1).At(16, 0);
            Schedule(() =>
            {
                Util.Logs.Log.GetLog(nameof(PushJob)).Info("每天19点定时历史报表正在生成...");
                AppServer.CreateReport(DateTime.Now.AddDays(-3));
            }).WithName("CreateRepeatReport").ToRunEvery(1).Days().At(19, 0);
            Schedule(() =>
            {
                Util.Logs.Log.GetLog(nameof(PushJob)).Info("每天21点定时ERP订单导入正在解析...");
                
                AppServer.ImportErpToOMS();
            }).WithName("ImportErpToOMS").ToRunEvery(1).Days().At(21, 0);
            Schedule(() =>
            {
                Util.Logs.Log.GetLog(nameof(PushJob)).Info("每天23点定时报表统计业务正在生成统计报表...");
                AppServer.CreateReport(DateTime.Now.AddDays(-1));
            }).WithName("CreateReport2").ToRunEvery(1).Days().At(23, 0);
            

        }
        public static void OnJobEnd(JobEndInfo obj)
        {
            switch (obj.Name)
            {
                case "CreateDailyReport1":
                case "CreateDailyReport2":
                case "CreateDailyReport3":
                case "CreateDailyReport4":
                case "CreateDailyReport5":
                    WxPushNews.SendErrorText($"当前时间：{DateTime.Now}，12点定时任务--生成统计报表完毕...");
                    Util.Logs.Log.GetLog(nameof(PushJob)).Info($"当前时间：{DateTime.Now}，12点定时任务--生成统计报表完毕...");
                    break;
                case "CreateReport6":
                case "CreateReport7":
                    WxPushNews.SendErrorText($"当前时间：{DateTime.Now}，19点定时任务--生成统计报表完毕...");
                    Util.Logs.Log.GetLog(nameof(PushJob)).Info($"当前时间：{DateTime.Now}，19点定时任务--生成统计报表完毕...");
                    break;
                case "PushDailyReport1":
                case "PushDailyReport2":
                case "PushDailyReport3":
                case "PushDailyReport4":
                case "PushDailyReport5":
                    WxPushNews.SendErrorText($"当前时间：{DateTime.Now}，每天14点定时报表推送完毕...");
                    Util.Logs.Log.GetLog(nameof(PushJob)).Info($"当前时间：{DateTime.Now}，每天14点定时报表推送完毕...");
                    break;
                case "PushDailyReport6":
                case "PushDailyReport7":
                    WxPushNews.SendErrorText($"当前时间：{DateTime.Now}，每天20点定时报表推送完毕...");
                    Util.Logs.Log.GetLog(nameof(PushJob)).Info($"当前时间：{DateTime.Now}，每天20点定时报表推送完毕...");
                    break;
                case "PushWeeklyReport":
                    WxPushNews.SendErrorText($"当前时间：{DateTime.Now}，每周一15点定时周报表推送完毕...");
                    Util.Logs.Log.GetLog(nameof(PushJob)).Info($"当前时间：{DateTime.Now}，每周一15点定时周报表推送完毕...");
                    break;
                case "PushMonthlyReport":
                    WxPushNews.SendErrorText($"当前时间：{DateTime.Now}，每月一号16点定时月报表推送完毕...");
                    Util.Logs.Log.GetLog(nameof(PushJob)).Info($"当前时间：{DateTime.Now}，每月一号16点定时月报表推送完毕...");
                    break;
                case "CreateRepeatReport":
                    WxPushNews.SendErrorText($"当前时间：{DateTime.Now}，每天19点定时历史报表生成完毕...");
                    Util.Logs.Log.GetLog(nameof(PushJob)).Info($"当前时间：{DateTime.Now}，每天19点定时历史报表生成完毕...");
                    break;
                case "ImportErpToOMS":
                    WxPushNews.SendErrorText($"当前时间：{DateTime.Now}，每天21点定时ERP订单导入解析完毕...");
                    Util.Logs.Log.GetLog(nameof(PushJob)).Info($"当前时间：{DateTime.Now}，每天21点定时ERP订单导入解析完毕...");
                    break;
                case "CreateReport2":
                    WxPushNews.SendErrorText($"当前时间：{DateTime.Now}，每天23点定时任务--生成统计报表完毕...");
                    Util.Logs.Log.GetLog(nameof(PushJob)).Info($"当前时间：{DateTime.Now}，每天23点定时任务...");
                    break;
                default:
                    WxPushNews.SendErrorText($"当前时间：{DateTime.Now}，任务名称{obj.Name}执行完毕");
                    Util.Logs.Log.GetLog(nameof(PushJob)).Info($"当前时间：{DateTime.Now}，任务名称{obj.Name}执行完毕");
                    break;
            }
        }
    }
}
