using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OMS.Models
{
    /// <summary>
    /// 采购方信息
    /// </summary>
    public class BusinessBuyer
    {
        public int Id { get; set; }
        /// <summary>
        /// 采购方称呼
        /// </summary>
        public string Name { get; set; }
        public string Contact { get; set; }
        public string Phone { get; set; }
        public string Phone2 { get; set; }
        public DateTime CreateTime { get; set; }
        public ICollection<AddressEntity> Addresslist { get; set; }

        public string DeliverType { get; set; }
        /// <summary>
        /// 是否需要发票
        /// </summary>
        public string NeedInvoice { get; set; }
        /// <summary>
        /// 发票类别
        /// </summary>
        public string InvoiceType { get; set; }
        public string InvoiceName { get; set; }
        /// <summary>
        /// 发票税点
        /// </summary>
        public float InvoiceValue { get; set; }
        /// <summary>
        /// 支付方式
        /// </summary>
        public string PaymentType { get; set; }
        /// <summary>
        /// 支付约定
        /// </summary>
        public string Paymentmark { get; set; }
        /// <summary>
        /// 合同编号
        /// </summary>
        public string ContractCode { get; set; }
        /// <summary>
        /// 项目名称
        /// </summary>
        public string ProjectName { get; set; }

        public string Sales { get; set; }

    }
    /// <summary>
    /// 供货方信息
    /// </summary>
    public class BusinessSupplier
    {
        public int Id { get; set; }
        /// <summary>
        /// 公司名称
        /// </summary>
        public string Company { get; set; }
        public string Contact { get; set; }
        public string Phone { get; set; }
        public string Phone2 { get; set; }
        public DateTime CreateTime { get; set; }
        
    }
}
