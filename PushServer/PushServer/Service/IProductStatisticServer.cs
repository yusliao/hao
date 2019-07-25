using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PushServer.Service
{
    /// <summary>
    /// 商品报表服务
    /// </summary>
    interface IProductStatisticServer
    {
       
        bool CreateMonthReport(int monthnum,int year);
        bool CreateWeekReport(int weeknum,int year);
        bool CreateDailyReport(DateTime value);
        bool CreateSeasonReport(int seasonnum,int year);
        bool CreateYearReport(int year);
        DataTable PushMonthReport(int monthnum, int year);
        DataTable PushWeekReport(int weeknum, int year);
        DataTable PushDailyReport(DateTime value);
        DataTable PushSeasonReport(int seasonnum, int year);

        DataTable PushYearReport(int year);
    }
    interface IServerName
    {
        string ServerName { get; }
    }
}
