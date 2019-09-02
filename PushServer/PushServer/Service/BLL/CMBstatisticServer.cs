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
    public class CMBProductStatisticServer : ProductStatisticServerBase
    {
        public override string ServerName => OrderSource.CMB;
    }
    [Export(typeof(IDistrictStatisticServer))]
    public class CMBDistrictStatisticServer : DistrictStatisticServerBase
    {
        public override string ServerName => OrderSource.CMB;
    }
    [Export(typeof(IOrderStatisticServer))]
    public class CMBOrderStatisticServer : OrderStatisticServerBase
    {
        public override string ServerName => OrderSource.CMB;
    }
}
