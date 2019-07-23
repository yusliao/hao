using System;
namespace DistrictService
{
    public class AddressInfo : DistrictInfo
    {
        public string Address
        {
            get;
            set;
        }
        public AddressInfo()
        {
        }
        public AddressInfo(string province, string city, string county, string address) : base(province, city, county)
        {
            this.Address = address;
        }
        public override string ToString()
        {
            return string.Format("{0} {1}", base.ToString(), this.Address);
        }
    }
}
