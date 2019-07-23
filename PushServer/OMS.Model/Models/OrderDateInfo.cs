using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
namespace OMS.Models
{
    public class OrderDateInfo
    {
        [Key]
        public long ID { get; set; }
        public DateTime  CreateTime { get; set; }
        /// <summary>
        /// 时间戳，北京时区
        /// </summary>
        public long TimeStamp { get; set; }

        public int WeekNum { get; set; }
        public int MonthNum { get; set; }
        public int SeasonNum { get; set; }
        public int Year { get; set; }
    }
}
