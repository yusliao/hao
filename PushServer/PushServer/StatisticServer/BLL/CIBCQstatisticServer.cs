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
    public class CIBCQProductStatisticServer : ProductStatisticServerBase
    {
        public override string ServerName => OrderSource.CIBVIP_CQ;
    }
    [Export(typeof(IDistrictStatisticServer))]
    public class CIBCQDistrictStatisticServer : DistrictStatisticServerBase
    {
        public override string ServerName => OrderSource.CIBVIP_CQ;
    }
    [Export(typeof(IOrderStatisticServer))]
    public class CIBCQOrderStatisticServer : OrderStatisticServerBase
    {
        public override string ServerName => OrderSource.CIBVIP_CQ;
    }
}
