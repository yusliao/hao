using System;
namespace DistrictService
{
    public class DistrictInfo
    {
        public string Province
        {
            get;
            set;
        }
        public string City
        {
            get;
            set;
        }
        public string County
        {
            get;
            set;
        }
        public DistrictInfo()
        {
        }
        public DistrictInfo(string province, string city, string county)
        {
            this.Province = province;
            this.City = city;
            this.County = county;
        }
        public override string ToString()
        {
            return string.Format("{0} {1} {2}", this.Province, this.City, this.County);
        }
    }

}
