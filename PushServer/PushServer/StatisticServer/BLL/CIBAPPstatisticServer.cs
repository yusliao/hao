using OMS.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PushServer.Service
{
    #region CIBAPP

   
    [Export(typeof(IProductStatisticServer))]
    public class CIBAPPProductStatisticServer: ProductStatisticServerBase
    {
        public override string ServerName => OrderSource.CIBAPP;
    }
    [Export(typeof(IDistrictStatisticServer))]
    public class CIBAPPDistrictStatisticServer : DistrictStatisticServerBase
    {
        public override string ServerName => OrderSource.CIBAPP;
    }
    [Export(typeof(IOrderStatisticServer))]
    public class CIBAPPOrderStatisticServer : OrderStatisticServerBase
    {
        public override string ServerName => OrderSource.CIBAPP;
    }
    #endregion
    #region CIBEVT
    [Export(typeof(IProductStatisticServer))]
    public class CIBEVTProductStatisticServer : ProductStatisticServerBase
    {
        public override string ServerName => OrderSource.CIBEVT;
    }
    [Export(typeof(IDistrictStatisticServer))]
    public class CIBEVTDistrictStatisticServer : DistrictStatisticServerBase
    {
        public override string ServerName => OrderSource.CIBEVT;
    }
    [Export(typeof(IOrderStatisticServer))]
    public class CIBEVTOrderStatisticServer : OrderStatisticServerBase
    {
        public override string ServerName => OrderSource.CIBEVT;
    }
    #endregion
    #region CIBSTM
    [Export(typeof(IProductStatisticServer))]
    public class CIBSTMProductStatisticServer : ProductStatisticServerBase
    {
        public override string ServerName => OrderSource.CIBSTM;
    }
    [Export(typeof(IDistrictStatisticServer))]
    public class CIBSTMDistrictStatisticServer : DistrictStatisticServerBase
    {
        public override string ServerName => OrderSource.CIBSTM;
    }
    [Export(typeof(IOrderStatisticServer))]
    public class CIBSTMOrderStatisticServer : OrderStatisticServerBase
    {
        public override string ServerName => OrderSource.CIBSTM;
    }
    #endregion
}
