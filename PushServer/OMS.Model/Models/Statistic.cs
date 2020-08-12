using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OMS.Models
{
    /// <summary>
    /// 订单报表
    /// </summary>
    public class Statistic
    {
        [Key]
        public long ID { get; set; }
        public string Source { get; set; }
        /// <summary>
        /// 通过desc来匹配那些合并的渠道
        /// </summary>
        public string SourceDesc { get; set; }
        
        /// <summary>
        /// 总订单数
        /// </summary>
        public int TotalOrderCount { get; set; }
        /// <summary>
        /// 总商品数量
        /// </summary>
        public int TotalProductCount { get; set; }
        /// <summary>
        /// 促销订单数
        /// </summary>
        public int PromotionalOrderCount { get; set; }
        /// <summary>
        /// 总金额
        /// </summary>
        public decimal TotalAmount { get; set; }
        /// <summary>
        /// 总成本
        /// </summary>
        public decimal TotalCostAmount { get; set; }
        /// <summary>
        /// 统一价计算的总金额
        /// </summary>
        public decimal TotalFlatAmount { get; set; }
        /// <summary>
        /// 总重量
        /// </summary>
        public decimal TotalWeight { get; set; }
        /// <summary>
        /// 总客户数
        /// </summary>
        public int TotalCustomer { get; set; }
        /// <summary>
        /// 总复购人数
        /// </summary>
        public int TotalCustomerRepurchase { get; set; }
        /// <summary>
        /// 总复购订单数
        /// </summary>
        public int TotalOrderRepurchase { get; set; }
        /// <summary>
        /// 总计复购盒数
        /// </summary>
        public int TotalProductRepurchase { get; set; }
        /// <summary>
        /// 报表日期值
        /// </summary>
        public DateTime CreateDate { get; set; }
        /// <summary>
        /// 报表类型 日：1，周：2，月：3，季：4，年：5
        /// </summary>
        public int StatisticType { get; set; }
        public int StatisticValue { get; set; }
        public int Year { get; set; }
    }
}
