using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
namespace OMS.Models
{
    public class ExceptionOrder
    {
        public long ID { get; set; }
        public string OrderInfo { get; set; }
        public string OrderFileName { get; set; }
        public string Source { get; set; }
        public string ErrorMessage { get; set; }
        public string SourceSn { get; set; }

        public ExceptionType ErrorCode { get; set; }
        public DateTime CreateTime { get; set; }

    }
    public enum ExceptionType
    {
        [Description("未知错误")]
        None =0,
        [Description("手机号或联系人为空")]
        PhoneNumOrPersonNameIsNull,
        [Description("渠道商品编号未知")]
        ProductIdUnKnown,
        [Description("渠道商品名称未知")]
        ProductNameUnKnown,
        [Description("ERP商品编码未知")]
        ProductCodeUnKnown,
        [Description("订单编号为空")]
        SourceSnIsNull,
        [Description("商品数量为空")]
        ProductCountIsNull,
        [Description("商品数量不正确")]
        ProductCountError,
        [Description("物流单号未知")]
        LogisticsNumUnKnown,
        [Description("订单中不存在该商品")]
        OrderProductsNoExisted,
        [Description("通过售后单未找到销售订单")]
        OrderNoExistedFromSubOrder,
        [Description("推送错误")]
        PushException



    }
}
