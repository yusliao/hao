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
    public class XunXiaoProductStatisticServer : ProductStatisticServerBase
    {
        public override string ServerName => OrderSource.XUNXIAO;
    }
    [Export(typeof(IDistrictStatisticServer))]
    public class XunXiaoDistrictStatisticServer : DistrictStatisticServerBase
    {
        public override string ServerName => OrderSource.XUNXIAO;
    }
    [Export(typeof(IOrderStatisticServer))]
    public class XunXiaoOrderStatisticServer : OrderStatisticServerBase
    {
        public override string ServerName => OrderSource.XUNXIAO;
    }
}
