using System;
namespace DistrictService
{
    public interface IDistrictService
    {
        AddressInfo ResolveAddress(string address);
        DistrictInfo ReviseDistrict(string province, string city, string county);
        string ReviseProvinceName(string provinceName);
        string ReviseCityName(string cityName);
        string ReviseCountyName(string countyName);
    }
}
