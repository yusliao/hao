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
            Schedule(() =>
            {
                Util.Logs.Log.GetLog(nameof(PushJob)).Info("每天13点定时报表统计业务正在生成统计报表...");
              
                AppServer.CreateReport(DateTime.Now.AddDays(-1));
            }).WithName("CreateReport1").ToRunEvery(1).Days().At(12, 0);
            Schedule(() =>
            {
                AppServer.PushDailyReport(DateTime.Now.AddDays(-1));
                Util.Logs.Log.GetLog(nameof(PushJob)).Info("每天14点定时报表推送完毕");
            }).WithName("PushDailyReport").ToRunEvery(1).Days().At(14, 0);
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
                case "CreateReport1":
                    WxPushNews.SendErrorText($"当前时间：{DateTime.Now}，12点定时任务--生成统计报表完毕...");
                    break;
                case "PushDailyReport":
                    WxPushNews.SendErrorText($"当前时间：{DateTime.Now}，每天14点定时报表推送完毕...");
                    break;
                case "PushWeeklyReport":
                    WxPushNews.SendErrorText($"当前时间：{DateTime.Now}，每周一15点定时周报表推送完毕...");
                    break;
                case "PushMonthlyReport":
                    WxPushNews.SendErrorText($"当前时间：{DateTime.Now}，每月一号16点定时月报表推送完毕...");
                    break;
                case "ImportErpToOMS":
                    WxPushNews.SendErrorText($"当前时间：{DateTime.Now}，每天21点定时ERP订单导入解析完毕...");
                    break;
                case "CreateReport2":
                    WxPushNews.SendErrorText($"当前时间：{DateTime.Now}，每天23点定时任务--生成统计报表完毕...");
                    break;
                default:
                    WxPushNews.SendErrorText($"当前时间：{DateTime.Now}，任务名称{obj.Name}执行完毕");
                    break;
            }
        }
    }
}
