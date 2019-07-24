using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OMS.Models
{
    public class ExceptionOrder
    {
        public long ID { get; set; }
        public string OrderInfo { get; set; }
        public string OrderFileName { get; set; }
        public string Source { get; set; }

    }
}
