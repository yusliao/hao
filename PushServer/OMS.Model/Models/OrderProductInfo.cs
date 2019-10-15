using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
namespace OMS.Models
{
    /// <summary>
    /// 订单商品信息
    /// </summary>
    public class OrderProductInfo
    {

        public long Id { get; set; }
        public int MonthNum { get; set; }
        /// <summary>
        /// 平台来源
        /// </summary>
        public string Source { get; set; }
        /// <summary>
        /// 商品信息
        /// </summary>
        public string sku { get; set; }
        public string OrderSn { get; set; }
        /// <summary>
        /// 平台商品ID
        /// </summary>
        public string ProductPlatId { get; set; }
        /// <summary>
        /// 平台商品Name
        /// </summary>
        public string ProductPlatName { get; set; }
        /// <summary>
        /// 商品数量
        /// </summary>
        public int ProductCount { get; set; }
        /// <summary>
        /// 商品总重量
        /// </summary>
        public decimal ProductWeight { get; set; }
        /// <summary>
        /// 单价
        /// </summary>
        public Decimal AmounPerUnit { get; set; }

        /// <summary>
        /// 实付金额
        /// </summary>
        public decimal TotalAmount { get; set; }
        /// <summary>
        /// 总成本价
        /// </summary>
        public decimal TotalCostPrice { get; set; }
        /// <summary>
        /// 统一价下的支付金额
        /// </summary>
        public decimal TotalFlatAmount { get; set; }



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
        /// 仓库 ，仓库信息通过物流商品对象来提供
        /// </summary>
       [Obsolete]
        public string Warehouse { get; set; } = "林江农业销售公司总仓";
    }
    /// <summary>
    /// 物流商品信息
    /// </summary>
    public class LogisticsProductInfo
    {

        public long Id { get; set; }
      
        /// <summary>
        /// ERP商品编号
        /// </summary>
        public string sku { get; set; }
      
        /// <summary>
        /// 平台商品ID
        /// </summary>
        public string ProductPlatId { get; set; }
        /// <summary>
        /// 平台商品Name
        /// </summary>
        public string ProductPlatName { get; set; }
        /// <summary>
        /// 物流商品数量
        /// </summary>
        public int ProductCount { get; set; }
        /// <summary>
        /// 物流商品总重量
        /// </summary>
        public decimal ProductWeight { get; set; }

      
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