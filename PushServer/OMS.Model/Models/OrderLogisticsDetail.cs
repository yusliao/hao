using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OMS.Models
{
    public class OrderLogisticsDetail
    {
        [Key]
        public int ID { get; set; }
        public string OrderSn { get; set; }
        /// <summary>
        /// 物流名称
        /// </summary>
        public string Logistics { get; set; } = "中通快递";
        /// <summary>
        /// 物流单号
        /// </summary>
        public string LogisticsNo { get; set; }
        /// <summary>
        /// 物流价格
        /// </summary>
        public decimal? LogisticsPrice { get; set; }
        /// <summary>
        /// 发货时间
        /// </summary>
        public DateTime? SendingTime { get; set; }
        /// <summary>
        /// 配货时间
        /// </summary>
        public DateTime? PickingTime { get; set; }
        /// <summary>
        /// 到货时间
        /// </summary>
        public DateTime? RecvTime { get; set; }

    }
}
