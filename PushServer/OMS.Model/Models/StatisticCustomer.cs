using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OMS.Models
{
    /// <summary>
    /// 客户报表
    /// </summary>
    public class StatisticCustomer
    {
        [Key]
        public long Id { get; set; }
        /// <summary>
        /// 渠道
        /// </summary>
        public string Source { get; set; }
        /// <summary>
        /// 渠道描述
        /// </summary>
        public string SourceDesc { get; set; }
        /// <summary>
        /// 姓名
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 手机号
        /// </summary>
        public string Phone { get; set; }
        /// <summary>
        /// 单数
        /// </summary>
        public int OrderCount { get; set; }
        /// <summary>
        /// 盒数
        /// </summary>
        public int ProductCount { get; set; }
        /// <summary>
        /// 客户信息
        /// </summary>
        public CustomerEntity Customer { get; set; }
        public DateTime CreateDate { get; set; }
        /// <summary>
        /// 报表类型 日：1，周：2，月：3，季：4，年：5
        /// </summary>
        public int StatisticType { get; set; }
        public int StatisticValue { get; set; }
        public int Year { get; set; }

    }
}
