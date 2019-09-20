using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
namespace OMS.Models
{
    public class OrderOptionRecord
    {
        public long Id { get; set; }

        [Required]
        public OrderEntity SourceOrder { get; set; }
        public OrderEntity SubOrder { get; set; }

    }
}
