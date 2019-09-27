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
    public class YouZanProductStatisticServer : ProductStatisticServerBase
    {
        public override string ServerName => OrderSource.YOUZAN;
    }
    [Export(typeof(IDistrictStatisticServer))]
    public class YouZanDistrictStatisticServer : DistrictStatisticServerBase
    {
        public override string ServerName => OrderSource.YOUZAN;
    }
    [Export(typeof(IOrderStatisticServer))]
    public class YouZanOrderStatisticServer : OrderStatisticServerBase
    {
        public override string ServerName => OrderSource.YOUZAN;
    }
}
