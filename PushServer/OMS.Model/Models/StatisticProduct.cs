﻿using OMS.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace OMS.Models
{
    /// <summary>
    /// 商品报表
    /// </summary>
    public class StatisticProduct
    {
        [Key]
        public int ID { get; set; }
        public string Source { get; set; }
        public string SourceDesc { get; set; }
        public string ProductPlatName { get; set; }
        public int weightCode { get; set; }
        public string weightCodeDesc { get; set; }
        public int ProductCount { get; set; }
        public decimal ProductTotalAmount { get; set; }
        public decimal ProductTotalWeight { get; set; }
        public int Year { get; set; }
        /// <summary>
        /// 报表日期值
        /// </summary>
        public DateTime CreateDate { get; set; }
        /// <summary>
        /// 报表类型 日：1，周：2，月：3，季：4，年：5
        /// </summary>
        public int StatisticType { get; set; }
        public int StatisticValue { get; set; }
    }
}
