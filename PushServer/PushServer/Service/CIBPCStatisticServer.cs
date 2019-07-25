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
    [Export(typeof(IProductStatisticServer))]
    public class CIBPCProductStatisticServer : ProductStatisticServerBase
    {
        public override string ServerName => OrderSource.CIB;
    }
    [Export(typeof(IDistrictStatisticServer))]
    public class CIBPCDistrictStatisticServer : DistrictStatisticServerBase
    {
        public override string ServerName => OrderSource.CIB;
    }
}
