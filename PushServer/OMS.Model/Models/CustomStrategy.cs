using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OMS.Models
{
    public class CustomStrategy
    {
        [Key]
        public long ID { get; set; }
        /// <summary>
        /// 客户对象
        /// </summary>
        public CustomerEntity Customer { get; set; }
        /// <summary>
        /// 策略枚举值
        /// </summary>
        public int StrategyValue { get; set; }
    }
    [Flags]
    public enum CustomStrategyEnum
    {
        None=0x00,
        [Description("01")]
        /// <summary>
        /// 发送空包裹
        /// </summary>
        EmptyPackage=0x01,
    }
}
