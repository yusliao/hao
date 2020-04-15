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
    public class JINWENProductStatisticServer: ProductStatisticServerBase
    {
        public override string ServerName => OrderSource.BANK_JINWEN;
    }
    [Export(typeof(IDistrictStatisticServer))]
    public class JINWENDistrictStatisticServer : DistrictStatisticServerBase
    {
        public override string ServerName => OrderSource.BANK_JINWEN;
    }
    [Export(typeof(IOrderStatisticServer))]
    public class JINWENOrderStatisticServer : OrderStatisticServerBase
    {
        public override string ServerName => OrderSource.BANK_JINWEN;
    }
    /// <summary>
    /// JINWEN
    /// </summary>
    [Export(typeof(IProductStatisticServer))]
    public class ICBC_JINWENProductStatisticServer : ProductStatisticServerBase
    {
        public override string ServerName => OrderSource.ICBC_JINWEN;
    }
    [Export(typeof(IDistrictStatisticServer))]
    public class ICBC_JINWENDistrictStatisticServer : DistrictStatisticServerBase
    {
        public override string ServerName => OrderSource.ICBC_JINWEN;
    }
    [Export(typeof(IOrderStatisticServer))]
    public class ICBC_JINWENOrderStatisticServer : OrderStatisticServerBase
    {
        public override string ServerName => OrderSource.ICBC_JINWEN;
    }
}
