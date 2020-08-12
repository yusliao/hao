using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OMS.Models.DTO
{
    public class DeliveryData
    {
        public Guid Id { get; set; }
        public Guid OrderId { get; set; }
       
        public int Num { get; set; }
       
        public DateTime? ShippedTime { get; set; }
        public DateTime? ReceivedTime { get; set; }
        public Guid ProductId { get; set; }
      
        public virtual ProductData Product { get; set; }
        public string Logistics { get; set; }
        public string LogisticsNo { get; set; }
    }
}
