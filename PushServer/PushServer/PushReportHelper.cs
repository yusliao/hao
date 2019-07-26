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
        public static bool PushReport()
        {
            return AppServer.PushReport();
        }
        public static bool PushPandianReport(int monthNum)
        {
            return AppServer.Instance.PushPandianReport(monthNum);
        }
        public static bool CreateReport(bool isAll=false)
        {
            return AppServer.CreateReport(DateTime.Now.AddDays(-1),isAll);
        }
        public static bool CreatePandianReport(int monthNum)
        {
            return AppServer.CreatePandianReport(monthNum);
        }
    }
}
