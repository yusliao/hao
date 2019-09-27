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
    public class RetailProductStatisticServer : ProductStatisticServerBase
    {
        public override string ServerName => OrderSource.RETAIL;
    }
    [Export(typeof(IDistrictStatisticServer))]
    public class RetailDistrictStatisticServer : DistrictStatisticServerBase
    {
        public override string ServerName => OrderSource.RETAIL;
    }
    [Export(typeof(IOrderStatisticServer))]
    public class RetailOrderStatisticServer : OrderStatisticServerBase
    {
        public override string ServerName => OrderSource.RETAIL;
    }
}
