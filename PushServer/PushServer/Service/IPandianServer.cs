using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PushServer.Service
{
    interface IPandianServer
    {
        string ServerName { get;  }
        bool CreateMonthPandianReport(int monthnum);
        DataTable PushPandianReport(int monthNum);
    }
}
