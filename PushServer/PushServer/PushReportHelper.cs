using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Util;
namespace PushServer
{
    class PushReportHelper
    {
        public static bool PushReport(DateTime dateTime,int reportType=1)
        {

            return AppServer.PushReport(dateTime,reportType);
           

        }
        public static bool PushPandianReport(int monthNum)
        {
            return AppServer.Instance.PushPandianReport(monthNum);
        }
        public static bool CreateReport()
        {
            return AppServer.CreateReport(DateTime.Now.AddDays(-1));
        }
        public static bool CreateDayReport(DateTime dateTime)
        {
            return AppServer.CreateReport(dateTime);
        }
        public static bool CreateHistoryReport(int month)
        {
            return AppServer.CreateHistoryReport(month, 2018);
        }
        public static bool CreatePandianReport(int monthNum)
        {
            return AppServer.CreatePandianReport(monthNum);
        }
    }
}
