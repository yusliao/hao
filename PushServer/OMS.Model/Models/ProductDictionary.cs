using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OMS.Models
{
    public class ProductDictionary
    {
        /// <summary>
        /// 自增主键
        /// </summary>
        [Key]
        public int Id { get; set; }
        /// <summary>
        /// 平台商品编号
        /// </summary>
        public string ProductId { get; set; }
        /// <summary>
        /// OMS系统商品编号
        /// </summary>
        public string ProductCode { get; set; }
        /// <summary>
        /// 商品在平台上的名称
        /// </summary>
        public string ProductNameInPlatform { get; set; }
        public string Source { get; set; }
        /// <summary>
        /// 渠道价格
        /// </summary>
        public decimal Price { get; set; }
        /// <summary>
        /// 状态：0已启用，1作废
        /// </summary>
        public int State { get; set; }

    }
}
