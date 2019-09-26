using System;
using System.Collections.Generic;
namespace DistrictService
{
    public static class DistrictService
    {
        private static readonly IDistrictService _internalService = new MemoryDistrictService();
     
        public static void Initialize()
        {
            Dictionary<string, string> provinceMap = DistrictMaps.ProvinceMap;
            List<DistrictInfo> districtList = DistrictFile.DistrictList;
        }
        public static AddressInfo ResolveAddress(string address)
        {
            if (string.IsNullOrEmpty(address))
                return new AddressInfo();
            return DistrictService._internalService.ResolveAddress(address);
        }
        public static string ReviseCityName(string cityName)
        {
            return DistrictService._internalService.ReviseCityName(cityName);
        }
        public static string ReviseCountyName(string countyName)
        {
            return DistrictService._internalService.ReviseCountyName(countyName);
        }
        public static string ReviseProvinceName(string provinceName)
        {
            return DistrictService._internalService.ReviseProvinceName(provinceName);
        }
        public static DistrictInfo ReviseDistrict(string province, string city, string county)
        {
            return DistrictService._internalService.ReviseDistrict(province, city, county);
        }
    }
}