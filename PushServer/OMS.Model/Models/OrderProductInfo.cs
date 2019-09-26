using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
namespace OMS.Models
{
    /// <summary>
    /// 商品实体信息,SKU-weightModel 一一对应
    /// </summary>
    [Table("ProductInfos")]
    
    public class ProductEntity
    {
        [Key]
        public string sku { get; set; }
        public string Category { get; set; }
        public int CategoryCode { get; set; }
        /// <summary>
        /// 品牌
        /// </summary>
        public string Brand { get; set; }
        /// <summary>
        /// 商品名称
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 商品简称
        /// </summary>
        public string ShortName { get; set; }

        /// <summary>
        /// 详细描述
        /// </summary>
        public string Desc { get; set; }
       
        /// <summary>
        /// 单重 g为单位
        /// </summary>
        public decimal QuantityPerUnit { get; set; }
        /// <summary>
        /// 重量规格代码
        /// </summary>
    
        public WeightCode weightModel { get; set; }
        /// <summary>
        /// 产地
        /// </summary>
        public AddressEntity Address { get; set; }

    }
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