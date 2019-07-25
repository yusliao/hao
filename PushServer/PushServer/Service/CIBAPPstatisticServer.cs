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
    public class CIBAPPstatisticServer : ProductStatisticServerBase
    {
        public override string ServerName => OrderSource.CIBAPP;
    }
}
