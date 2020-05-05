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
        [Description("银行-兴业银行积分PC")]
        public const string CIB = "CIB";
        /// <summary>
        /// 分期商城
        /// </summary>
        [Description("银行-兴业分期商城")]
        public const string CIBSTM = "CIBSTM";
        /// <summary>
        /// 积分加自付金
        /// </summary>
        [Description("银行-兴业积分加自付金")]
        public const string CIBEVT = "CIBEVT";
        [Description("银行-兴业银行积分")]
        public const string CIBAPP = "CIBAPP";
        [Description("银行-招商银行网上商城")]
        public const string CMB = "CMB";
        [Description("银行-兴业银行积点商城")]
        public const string CIBVIP = "CIBVIP";
        [Description("银行-重庆兴业银行黑白金客户")]
        public const string CIBVIP_CQ = "CIBVIP_CQ";
        [Description("银行-重庆白金客户")]
        public const string CIBW_CQ = "CIBW_CQ";
        [Description("银行-重庆黑金客户")]
        public const string CIBB_CQ = "CIBB_CQ";
        [Description("客情-周期配送")]
        public const string FRIEND = "FRIEND";
        [Description("客情-非周期配送")]
        public const string FRIEND_Un = "FRIEND_Un";
        [Description("其他-零售仓")]
        public const string RETAIL = "RETAIL";
        [Description("银行-广发银行商城")]
        public const string CGB = "CGB";
        [Description("银行-民生银行商城")]
        public const string CMBC = "CMBC";
        [Description("电商-到家网")]
        public const string CMPMC = "CMPMC";
        [Description("电商-京东水清清食品旗舰店")]
        public const string JINGDONG = "JINGDONG";
        [Description("银行-金文科技代发")]
        public const string BANK_JINWEN = "BANK_JINWEN";
        [Description("银行-金文（工行代发）")]
        public const string ICBC_JINWEN = "ICBC_JINWEN";
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
        [Description("线下订单")]
        public const string OFFLINE = "OFFLINE";
        [Description("电商-邮乐农品平台林江米业旗舰店")]
        public const string ULE = "ULE";
        [Description("所有订单")]
        public const string ALL = "ALL";
        [Description("渠道-欧阳总")]
        public const string OUYANG = "OUYANG";
        [Description("渠道-张鹏")]
        public const string ZHANGPENG = "ZHANGPENG";
        [Description("长春-品味鲜米")]
        public const string SELF_CC = "SELF_CC";
        [Description("深圳-品味鲜米")]
        public const string SELF_SZ = "SELF_SZ";
        [Description("渠道-代莲清")]
        public const string DAILIANQING = "DAILIANQING";
        [Description("其他-林江农业销售客户")]
        public const string SELF_X = "SELF_X";
        [Description("其他-大米网")]
        public const string DAMI = "DAMI";
        [Description("渠道-聂亚峰")]
        public const string NIEYAFENG = "NIEYAFENG";

    }

}