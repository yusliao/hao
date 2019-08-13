using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
namespace OMS.Models
{
   
    public class OrderPandianWithMonth
    {
        /// <summary>
        /// 平台
        /// </summary>
        public string Source { get; set; }
        public string SourceDesc { get; set; }
       
        /// <summary>
        /// 订单类型
        /// </summary>
        public string OrderType { get; set; }
        [Key]
        /// <summary>
        /// 平台订单编号
        /// </summary>
        public string SourceSn { get; set; }
        /// <summary>
        /// 下单时间
        /// </summary>
        public DateTime CreatedDate { get; set; }
      

        /// <summary>
        /// 订单状态
        /// </summary>
        public int OrderStatus { get; set; }
        public string OrderStatusDesc { get; set; }


        /// <summary>
        /// 商品信息
        /// </summary>
        public ICollection<OrderPandianProductInfo> Products { get; set; }
        /// <summary>
        /// 备注
        /// </summary>
        public string Remarks { get; set; }

    }
    public class OrderPandianProductInfo
    {
        public int MonthNum { get; set; }
        public int Year { get; set; }
        [Key]
        public long Id { get; set; }
        /// <summary>
        /// 平台来源
        /// </summary>
        public string Source { get; set; }
        public string SourceDesc { get; set; }
        /// <summary>
        /// 商品信息
        /// </summary>
        public string sku { get; set; }
        public string ProductPlatName { get; set; }
        /// <summary>
        /// 商品数量
        /// </summary>
        public int ProductCount { get; set; }
        /// <summary>
        /// 商品重量
        /// </summary>
        public decimal ProductWeight { get; set; }

        /// <summary>
        /// 实付金额
        /// </summary>
        public decimal TotalAmount { get; set; }
        /// <summary>
        /// 优惠金额
        /// </summary>
        public decimal DiscountFee { get; set; }
        /// <summary>
        /// 重量规格代码
        /// </summary>

        public int weightCode { get; set; }
        /// <summary>
        /// 重量规格代码
        /// </summary>

        public string weightCodeDesc { get; set; }
        /// <summary>
        /// 仓库
        /// </summary>
        public string Warehouse { get; set; } = "林江农业销售公司总仓";
    }
}