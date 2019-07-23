using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OMS.Models
{
    public class ChinaAreaData
    {
        
        public double ID { get; set; }
        public string Name { get; set; }
        public double ParentId { get; set; }
        public string ShortName { get; set; }
        public double levelType { get; set; }
        public string CityCode { get; set; }
        public string ZipCode { get; set; }
        public string MergerName { get; set; }
        public string Ing { get; set; }
        public string Lat { get; set; }
        public string Pinyin { get; set; }
    }
}
