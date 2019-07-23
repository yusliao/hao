using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;
namespace OMS.Models
{
    /// <summary>
    /// 客户信息
    /// </summary>
    [Table("CustomerInfo")]
    public class CustomerEntity
    {
        [Key]
        public int CustomerId { get; set; }
        public string Name { get; set; }
        public string Phone { get; set; }
        public string Phone2 { get; set; }
        /// <summary>
        /// 身份证
        /// </summary>
        public string PersonCard { get; set; }

        /// <summary>
        /// 客户联系标识，用于关联客户
        /// </summary>
        public int Code { get; set; }
        public DateTime? CreateDate { get; set; } = DateTime.Now.Date;
        /// <summary>
        /// 地址列表
        /// </summary>
        public ICollection<AddressEntity> Addresslist { get; set; }


    }
}