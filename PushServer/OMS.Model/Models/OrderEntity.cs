using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
namespace OMS.Models
{
    [Table("OrderInfo")]
    public class OrderEntity
    {
        /// <summary>
        /// 平台
        /// </summary>
        public string Source { get; set; }
        public string SourceDesc { get; set; }
        [Key]
        /// <summary>
        /// 订单编号
        /// </summary>
        public string OrderSn { get; set; }
        /// <summary>
        /// 平台订单编号
        /// </summary>
        public string SourceSn { get; set; }
        /// <summary>
        /// 下单时间
        /// </summary>
        public DateTime CreatedDate { get; set; }
        /// <summary>
        /// 订单复购信息
        /// </summary>
        public OrderRepurchase OrderRepurchase { get; set; }
        /// <summary>
        /// 订单时间信息
        /// </summary>
        public OrderDateInfo OrderDateInfo { get; set; }
        /// <summary>
        /// 下单客户信息
        /// </summary>
        public CustomerEntity Customer { get; set; }
        /// <summary>
        /// 收货人信息
        /// </summary>
        public CustomerEntity Consignee { get; set; }
        
        /// <summary>
        /// 地址描述
        /// </summary>
        public AddressEntity ConsigneeAddress { get; set; }

        /// <summary>
        /// 订单状态
        /// </summary>
        public int OrderStatus { get; set; }
        public string OrderStatusDesc { get; set; }


        /// <summary>
        /// 商品信息
        /// </summary>
        public ICollection<OrderProductInfo> Products { get; set; }
        public ICollection<OrderLogisticsDetail> OrderLogistics { get; set; }
        /// <summary>
        /// 订单操作记录集
        /// </summary>
        public ICollection<OrderOptionRecord> OrderOptionRecords { get; set; }

        /// <summary>
        /// 备注
        /// </summary>
        public string Remarks { get; set; }
        /// <summary>
        /// 订单附加信息
        /// </summary>
        public OrderExtendInfo OrderExtendInfo { get; set; }
        /// <summary>
        /// 订单来源方式0：正常录入，1:月结补录,2:ERP导出单补录
        /// </summary>
        public int OrderComeFrom { get; set; }
        /// <summary>
        /// 订单类型0：销售订单，1:退换货订单,2:退货/退钱,4:周期购订单
        /// </summary>
        public int OrderType { get; set; }
        /// <summary>
        /// 支付方式，1：积分，2：自付金，3：积分+自付金，4：分期
        /// </summary>
        public int PayType { get; set; }

    }
}