using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;
namespace OMS.Models
{
    /// <summary>
    /// 地址信息
    /// </summary>
    [System.ComponentModel.DataAnnotations.Schema.Table("AddressInfo")]
    
    public class AddressEntity
    {
        [Key]
        public int Id { get; set; }
        public string Province { get; set; }
        public string City { get; set; }
        public string County { get; set; }
        
        public string ZipCode { get; set; }
        public string Address { get; set; }
        /// <summary>
        /// 地址指纹，对Address字段进行MD5计算
        /// </summary>
        public string MD5 { get; set; }
    }
}