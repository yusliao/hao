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
            }).ToRunEvery(1).Days().At(12, 0);
            Schedule(() =>
            {
                AppServer.PushDailyReport(DateTime.Now.AddDays(-1));
                Util.Logs.Log.GetLog(nameof(PushJob)).Info("每天14点定时报表推送完毕");
            }).ToRunEvery(1).Days().At(14, 0);
            Schedule(() =>
            {
                AppServer.PushWeeklyReport(DateTime.Now.AddDays(-1));
                Util.Logs.Log.GetLog(nameof(PushJob)).Info("每周一15点定时周报表推送完毕");
            }).ToRunEvery(0).Weeks().On(DayOfWeek.Monday).At(15,0);
            Schedule(() =>
            {
                AppServer.PushMonthlyReport(DateTime.Now.AddDays(-1));
                Util.Logs.Log.GetLog(nameof(PushJob)).Info("每月一号16点定时月报表推送完毕");
            }).ToRunEvery(0).Months().On(1).At(16, 0);
            Schedule(() =>
            {
                Util.Logs.Log.GetLog(nameof(PushJob)).Info("每天23点定时报表统计业务正在生成统计报表...");
                AppServer.CreateReport(DateTime.Now.AddDays(-1));
            }).ToRunEvery(0).Days().At(23, 0);

          
          


        }
    }
}
