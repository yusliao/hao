using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

using System.ComponentModel;

namespace OMS.Models
{
    /// <summary>
    /// 订单渠道，渠道标识必须是大写字母
    /// </summary>
    public class OrderSource
    {
        [Description("银行-兴业银行积分")]
        public const string CIB = "CIB";
        [Description("银行-兴业银行积分")]
        public const string CIBAPP = "CIBAPP";
        [Description("银行-招商银行网上商城")]
        public const string CMB = "CMB";
        [Description("银行-兴业银行积点商城")]
        public const string CIBVIP = "CIBVIP";
        [Description("银行-重庆兴业银行黑白金客户")]
        public const string CIBVIP_CQ = "CIBVIP_CQ";
        [Description("客情-周期配送")]
        public const string FRIEND = "FRIEND";
        [Description("其他-零售仓")]
        public const string RETAIL = "RETAIL";
        [Description("广发银行商城")]
        public const string CGB = "CGB";
        [Description("民生银行")]
        public const string CMBC = "CMBC";
        [Description("电商-京东水清清食品旗舰店")]
        public const string JINGDONG = "JINGDONG";
        [Description("水清清官方优品农铺")]
        public const string TAOBAO = "TAOBAO";
        [Description("电商-天猫商城旗舰店")]
        public const string TIANMAO = "TIANMAO";
        [Description("水清清官方旗舰店")]
        public const string WEIDIAN = "WEIDIAN";
        [Description("水清清旗舰店")]
        public const string YHD = "YHD";
        [Description("银行-迅销科技代发")]
        public const string XUNXIAO = "XUNXIAO";
        [Description("电商-有赞商城")]
        public const string YOUZAN = "YOUZAN";
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