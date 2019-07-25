using OMS.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PushServer.Service
{
    [Export(typeof(IProductStatisticServer))]
    public class CIBAPPProductStatisticServer: ProductStatisticServerBase
    {
        public override string ServerName => OrderSource.CIBAPP;
    }
    [Export(typeof(IDistrictStatisticServer))]
    public class CIBAPPDistrictStatisticServer : DistrictStatisticServerBase
    {
        public override string ServerName => OrderSource.CIBAPP;
    }
}
