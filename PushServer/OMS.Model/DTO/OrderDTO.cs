
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OMS.Models.DTO
{
    public class OrderDTO
    {
        public string productName { get; set; }
        public string productsku { get; set; }
        /// <summary>
        /// 单价
        /// </summary>
        public Decimal pricePerUnit { get; set; }
        /// <summary>
        /// 让利金额
        /// </summary>
        public Decimal discountFee { get; set; }
        /// <summary>
        /// 实付金额
        /// </summary>
        public Decimal totalAmount { get; set; }
        public string fileName { get; set; }
        public string sourceSN { get; set; }

        public int count { get; set; }
        public string source { get; set; }
        public string sourceDesc { get; set; }
        public string orderSN { get; set; }
        /// <summary>
        /// 订单来源方式0：销售订单，1:退换货订单,2:退货/退钱,4:周期购订单
        /// </summary>
        public int orderType { get; set; }
        public string orderSN_old { get; set; }
        public DateTime createdDate { get; set; }
        public OrderStatus orderStatus { get; set; }
        public string consigneeName { get; set; }
        public string consigneePhone { get; set; }
        public string consigneeProvince { get; set; }
        public string consigneeCity { get; set; }
        public string consigneeCounty { get; set; }
        public string consigneeAddress { get; set; }
        public string consigneeZipCode { get; set; }
        public string consigneePhone2 { get; set; }
        public int weightCode { get; set; }
        public string weightCodeDesc { get; set; }
        public string Warehouse { get; set; }
        /// <summary>
        /// 订单来源方式0：正常录入，1:月结补录,2:ERP导出单补录
        /// </summary>
        public int OrderComeFrom { get; set; }
        public PayType PayType { get; set; }



    }
    public class BusinessOrderDTO:OrderDTO
    {

        public BusinessBuyer Buyer { get; set; } = new BusinessBuyer();
        public BusinessSupplier Supplier { get; set; } = new BusinessSupplier();
     
        

    }
}
