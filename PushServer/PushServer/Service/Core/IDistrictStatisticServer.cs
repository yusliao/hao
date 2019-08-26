using OMS.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PushServer.Service
{
    interface IDistrictStatisticServer: IServerName
    {
        bool CreateDistrictMonthReport(int monthnum, int year);
        bool CreateDistrictWeekReport(int weeknum, int year);
        bool CreateDistrictDailyReport(DateTime value);
        bool CreateDistrictSeasonReport(int seasonnum, int year);
        bool CreateDistrictYearReport(int year);
        DataTable PushDistrictMonthReport(int monthnum, int year);
        DataTable PushDistrictWeekReport(int weeknum, int year);
        DataTable PushDistrictDailyReport(DateTime value);
        DataTable PushDistrictSeasonReport(int seasonnum, int year);

        DataTable PushDistrictYearReport(int year);
    }
}
