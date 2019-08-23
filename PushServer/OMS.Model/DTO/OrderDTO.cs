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
        public string fileName { get; set; }
        public string sourceSN { get; set; }

        public int count { get; set; }
        public string source { get; set; }
        public string sourceDesc { get; set; }
        public string orderSN { get; set; }
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
        public string MyProperty { get; set; }

    }
}
