using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OMS.Models.DTO
{
    public class ProductDicDTO
    {
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
        public ProductEntity ERPProduct { get; set; }
        /// <summary>
        /// 渠道
        /// </summary>
        public string Source { get; set; }
        /// <summary>
        /// 支付价格
        /// </summary>
        public decimal PayPrice { get; set; }
        /// <summary>
        /// 状态,[flag] 0：启用
        /// </summary>
        public int State { get; set; }

        public DateTime CreateTime { get; set; } = DateTime.Now;

    }
}
