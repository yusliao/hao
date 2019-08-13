using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OMS.Models
{
    /// <summary>
    /// 物流信息
    /// </summary>
    public class LogisticsInfo
    {
        [Key]
       // [DatabaseGenerated(System.ComponentModel.DataAnnotations.Schema.DatabaseGeneratedOption.None)]
        public int ID { get; set; }
        /// <summary>
        /// 名称
        /// </summary>
        public string FullName { get; set; }
        /// <summary>
        /// 银行物流编码
        /// </summary>
        public string BankLogisticsCode { get; set; }


    }
}
