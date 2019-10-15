using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;
namespace OMS.Models
{
    /// <summary>
    /// 订单扩展信息
    /// 订单数据的初级加工
    /// </summary>
    public class OrderExtendInfo
    {
        [Key]
        public long Id { get; set; }
        public string OrderSn { get; set; }
        /// <summary>
        /// 用来标记日统计中是否是复购单
        /// </summary>
        public bool IsReturningCustomer { get; set; }
        /// <summary>
        /// 是否促销
        /// </summary>
        public bool IsPromotional { get; set; }
        /// <summary>
        /// 订单总支付金额
        /// </summary>
        public decimal TotalAmount { get; set; }
        /// <summary>
        /// 总成本价
        /// </summary>
        public decimal TotalCostPrice { get; set; }
        /// <summary>
        /// 订单统一价下支付金额
        /// </summary>
        public decimal TotalFlatAmount { get; set; }
        /// <summary>
        /// 总商品数量
        /// </summary>
        public int TotalProductCount { get; set; }
        /// <summary>
        /// 订单总重量
        /// </summary>
        public decimal TotalWeight { get; set; }
        /// <summary>
        /// 订单优惠额
        /// </summary>
        public decimal DiscountFee { get; set; }
        /// <summary>
        /// 下单日期
        /// </summary>
        public DateTime? CreatedDate { get; set; }
     
        public BusinessBuyer Buyer { get; set; }
        public BusinessSupplier Supplier { get; set; }
       


    }
}