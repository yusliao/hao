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
    public class TMProductStatisticServer: ProductStatisticServerBase
    {
        public override string ServerName => OrderSource.TIANMAO;
    }
    [Export(typeof(IDistrictStatisticServer))]
    public class TMDistrictStatisticServer : DistrictStatisticServerBase
    {
        public override string ServerName => OrderSource.TIANMAO;
    }
    [Export(typeof(IOrderStatisticServer))]
    public class TMOrderStatisticServer : OrderStatisticServerBase
    {
        public override string ServerName => OrderSource.TIANMAO;
    }
}
