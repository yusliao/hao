using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OMS.Models
{
    /// <summary>
    /// 复购
    /// </summary>
    public class OrderRepurchase
    {
        [Key]
        public long ID { get; set; }
        public bool DailyRepurchase { get; set; }
       
        public bool WeekRepurchase { get; set; }
        public bool MonthRepurchase { get; set; }
        public bool SeasonRepurchase { get; set; }
        public bool YearRepurchase { get; set; }
    }
}
