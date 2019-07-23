using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OMS.Models
{
    /// <summary>
    /// 重量规格代码
    /// </summary>
    public class WeightCode
    {
        [Key]
        public int Code { get; set; }
        /// <summary>
        /// 单位为克
        /// </summary>
        public int Value { get; set; }
    }
}
