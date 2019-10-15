using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OMS.Models
{
    
    public enum ProductCategory
    {
        枸杞=1,
        珍珠米,
        香米,
        现磨珍珠米,
        现磨香米,
        榛蘑,
        木耳,
        黑豆,
        红豆,
        黄豆,
        绿豆,
        小米,
        米类组合,
        豆类组合,
        蓝莓酒


    }
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
        /// <summary>
        /// 成本价，一个SKU成本价是固定的，如果价格变更需要变更SKU
        /// </summary>
        public decimal CostPrice { get; set; }
        /// <summary>
        /// 统一价
        /// </summary>
        public decimal FlatPrice { get; set; }



    }
}
