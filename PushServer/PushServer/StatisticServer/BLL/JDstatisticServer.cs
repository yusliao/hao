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
    public class JDProductStatisticServer: ProductStatisticServerBase
    {
        public override string ServerName => OrderSource.JINGDONG;
    }
    [Export(typeof(IDistrictStatisticServer))]
    public class JDDistrictStatisticServer : DistrictStatisticServerBase
    {
        public override string ServerName => OrderSource.JINGDONG;
    }
    [Export(typeof(IOrderStatisticServer))]
    public class JDOrderStatisticServer : OrderStatisticServerBase
    {
        public override string ServerName => OrderSource.JINGDONG;
    }
}
