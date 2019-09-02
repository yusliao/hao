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
    public class FriendProductStatisticServer : ProductStatisticServerBase
    {
        public override string ServerName => OrderSource.FRIEND;
    }
    [Export(typeof(IDistrictStatisticServer))]
    public class FriendDistrictStatisticServer : DistrictStatisticServerBase
    {
        public override string ServerName => OrderSource.FRIEND;
    }
    [Export(typeof(IOrderStatisticServer))]
    public class FriendOrderStatisticServer : OrderStatisticServerBase
    {
        public override string ServerName => OrderSource.FRIEND;
    }
}
