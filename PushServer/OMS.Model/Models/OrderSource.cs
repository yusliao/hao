using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

using System.ComponentModel;

namespace OMS.Models
{
    
    public class OrderSource
    {
        [Description("银行-兴业银行积分")]
        public const string CIB = "CIB";
        [Description("银行-兴业银行积分")]
        public const string CIBAPP = "CIBAPP";
        [Description("兴业分期商城")]
        public const string CIBSTM = "CIBSTM";
        [Description("银行-兴业银行积点商城")]
        public const string CIBVIP = "CIBVIP";
        [Description("银行-重庆兴业银行黑白金客户")]
        public const string CIBVIP_CHONGQING = "CIBVIP_CHONGQING";
        [Description("兴业活动")]
        public const string CIBEVT = "CIBEVT";
        [Description("兴业生活商城")]
        public const string CIBLFM = "CIBLFM";
        [Description("广发银行商城")]
        public const string CGB = "CGB";
        [Description("民生银行")]
        public const string CMBC = "CMBC";
        [Description("水清清官方旗舰店")]
        public const string JINGDONG = "JINGDONG";
        [Description("水清清官方优品农铺")]
        public const string TAOBAO = "TAOBAO";
        [Description("电商-天猫商城旗舰店")]
        public const string TIANMAO = "TIANMAO";
        [Description("水清清官方旗舰店")]
        public const string WeiDian = "WeiDian";
        [Description("水清清旗舰店")]
        public const string YHD = "YHD";
        [Description("兴业积分")]
        public const string CMPMC = "CMPMC";
        [Description("水清清优选")]
        public const string YouZan = "YouZan";
        [Description("工行融E购")]
        public const string ICBC = "ICBC";
        [Description("工行爱购")]
        public const string ICIB = "ICIB";
        [Description("ERP录入")]
        public const string ERP = "ERP";
        [Description("CIB积分盘点")]
        public const string CIBJifenPanDian = "CIBJifenPanDian";
        [Description("所有订单")]
        public const string ALL = "ALL";

    }

}