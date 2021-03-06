﻿using OMS.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PushServer.Service
{
    
    [Export(typeof(IProductStatisticServer))]
    public class ALLProductStatisticServer : ProductStatisticServerBase
    {
        public override string ServerName => OrderSource.ALL;
    }
    [Export(typeof(IDistrictStatisticServer))]
    public class ALLDistrictStatisticServer : DistrictStatisticServerBase
    {
        public override string ServerName => OrderSource.ALL;
    }
    [Export(typeof(IOrderStatisticServer))]
    public class ALLOrderStatisticServer:OrderStatisticServerBase
    {
        public override string ServerName => OrderSource.ALL;
    }

}
