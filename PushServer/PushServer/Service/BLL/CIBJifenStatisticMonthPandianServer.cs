using OMS.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.Entity;
using System.Data;
using Util;
using System.ComponentModel.Composition;

namespace PushServer.Service
{
    
    [Export(typeof(IPandianServer))]
    
    public class CIBJifenStatisticMonthPandianServer : ProductPandianStatisticServerBase
    {
         public override string ServerName => OrderSource.CIBJifenPanDian;
    }
}
