using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OMS.Models
{
    public class StatisticDistrictItem
    {
        [Key]
        public long ID { get; set; }
        public long StatisticDistrictID { get; set; }
        public ChinaAreaData AddressID { get; set; }
        public int OrderCount { get; set; }
        /// <summary>
        /// 订单总金额
        /// </summary>
        public decimal TotalAmount { get; set; }
        /// <summary>
        /// 总商品数量
        /// </summary>
        public int TotalProductCount { get; set; }
        /// <summary>
        /// 订单总重量
        /// </summary>
        public decimal TotalWeight { get; set; }
        /// <summary>
        /// 订单优惠额
        /// </summary>
        public decimal DiscountFee { get; set; }
        /// <summary>
        /// 复购订单数量
        /// </summary>
        public int TotalOrderRepurchase { get; set; }
        /// <summary>
        /// 总购买人数
        /// </summary>
        public int TotalCustomers { get; set; }
        /// <summary>
        /// 总复购人数
        /// </summary>
        public int TotalCustomerRepurchase { get; set; }
        /// <summary>
        /// 报表日期值
        /// </summary>
        public DateTime CreateDate { get; set; }
       

    }
    public class StatisticDistrict
    {
        public string Source { get; set; }
        public string SourceDesc { get; set; }
        public long StatisticDistrictID { get; set; }
        public ICollection<StatisticDistrictItem> StatisticDistrictriProvinceItems { get; set; }
        public ICollection<StatisticDistrictItem> StatisticDistrictriCityItems { get; set; }

        /// <summary>
        /// 报表日期值
        /// </summary>
        public DateTime CreateDate { get; set; }
        /// <summary>
        /// 报表类型 日：1，周：2，月：3，季：4，年：5
        /// </summary>
        public int StatisticType { get; set; }
        public int StatisticValue { get; set; }
        public int Year { get; set; }
    }
}
