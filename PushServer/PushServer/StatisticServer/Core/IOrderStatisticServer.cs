using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PushServer.Service
{
    interface IOrderStatisticServer:IServerName
    {
        bool CreateMonthReport(int monthnum, int year);
        bool CreateWeekReport(int weeknum, int year);
        bool CreateDailyReport(DateTime value);
        bool CreateSeasonReport(int seasonnum, int year);
        bool CreateYearReport(int year);
        void PushMonthReport(int monthnum, int year);
        void PushWeekReport(int weeknum, int year);
        void PushDailyReport(DateTime value);
        void PushSeasonReport(int seasonnum, int year);

        void PushYearReport(int year);
       
    }
}
