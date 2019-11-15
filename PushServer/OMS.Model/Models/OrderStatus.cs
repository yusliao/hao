using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel;

namespace OMS.Models
{
    
    public enum OrderStatus
    {

        //Pending,    
        //Accepted,
        //Unaccepted,
        //Deleted,
        //Error,


        /** New Status **/


        Undefined,
        /// <summary>
        /// 新建订单（新建或未完成支付订单）
        /// </summary>
        [Description("新建订单")]
        New,
        [Description("已确认订单")]
        /// <summary>
        /// 已确认订单（已确认或者已支付订单，等待发货）
        /// </summary>
        Confirmed,
        [Description("预配送订单")]
        /// <summary>
        /// 预配送（已进行物流筛单,打印物流清单，但未正式发货）
        /// </summary>
        Predelivery,
        [Description("已发货订单")]
        /// <summary>
        /// 已发货（等待客户签收，非电商平台 已完成订单）
        /// </summary>
        Delivered,
        [Description("已完成订单")]
        /// <summary>
        /// 已完成订单（客户签收，电商平台 已完成订单）
        /// </summary>
        Finished,
        [Description("已取消订单")]
        /// <summary>
        /// 已取消订单（客户/电商平台取消订单或用户退款/退货）
        /// </summary>
        Cancelled
    }
    /// <summary>
    /// 订单来源方式
    /// </summary>
    [Flags]
    public enum OrderComeFrom
    {
        [Description("正常录入")]
        None=0,
        [Description("月结补录")]
        Yuejiebulu =0x01,
        [Description("月结多出")]
        YuejieDuoyu = 0x02,

    }
    public enum PayType
    {
        None=0,
        Integral,//积分
        Money,//自付
        IntegralAndMoney,//积分+自付金
        installments //分期付款
    }
   

   
}