using OMS.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PushServer.Service
{
    
    [Export(typeof(ProductStatisticServerBase))]
    public class ALLProductStatisticServer : ProductStatisticServerBase
    {
        public override string ServerName => OrderSource.ALL;
    }
    [Export(typeof(DistrictStatisticServerBase))]
    public class ALLDistrictStatisticServer : DistrictStatisticServerBase
    {
        public override string ServerName => OrderSource.ALL;
    }
    [Export(typeof(OrderStatisticServerBase))]
    public class ALLOrderStatisticServer:OrderStatisticServerBase
    {
        public override string ServerName => OrderSource.ALL;
    }

}
