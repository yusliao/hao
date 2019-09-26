
using OMS.Models;
using System;
using System.Collections.Generic;
using System.Linq;
namespace DistrictService
{
    public class MemoryDistrictService : IDistrictService
    {
        private readonly List<DistrictInfo> _districtList;
        public MemoryDistrictService()
        {
            this._districtList = DistrictFile.DistrictList;
        }
        public string ReviseCityName(string cityName)
        {
            DistrictInfo districtInfo = this.FindByCity(cityName);
            return (districtInfo != null) ? districtInfo.City : this.GetCityNameFromMap(cityName);
        }
        public string ReviseCountyName(string countyName)
        {
            DistrictInfo districtInfo = this.FindByCounty(countyName);
            return (districtInfo != null) ? districtInfo.County : this.GetCountyNameFormMap(countyName);
        }
        public string ReviseProvinceName(string provinceName)
        {
            DistrictInfo districtInfo = this.FindByProvince(provinceName);
            return (districtInfo != null) ? districtInfo.Province : this.GetProvinceNameFromMap(provinceName);
        }
        public AddressInfo ResolveAddress(string address)
        {
            AddressInfo addressInfo = new AddressInfo();
            string text = "";
            string text2 = "";
            string text3 = "";
            string address2 = address.RemoveInnerBlankSpace();
            this.ResolveProvinceName(ref address2, ref text, ref text2);
            switch (text)
            {
                case "北京":
                case "上海":
                case "天津":
                case "重庆":
                    text2 = $"{text}市";
                    break;
                default:
                    this.ResolveCityName(ref address2, ref text, ref text2);
                    break;
            }
            this.ResolveCountyName(ref address2, ref text, ref text2, ref text3);
            bool flag = string.IsNullOrEmpty(text) && string.IsNullOrEmpty(text2) && string.IsNullOrEmpty(text3);
            if (flag)
            {
                this.ResolveAddressFromCustomMap(ref address2, ref text, ref text2, ref text3);
            }
            addressInfo.Province = text;
            addressInfo.City = text2;
            addressInfo.County = text3;
            addressInfo.Address = address2;
            return addressInfo;
        }
        public DistrictInfo ReviseDistrict(string province, string city, string county)
        {
            DistrictInfo districtInfo = this.FindByProvinceAndCityAndCounty(province, city, county);
            bool flag = districtInfo == null;
            DistrictInfo result;
            if (flag)
            {
                string province2 = this.ReviseProvinceName(province);
                string city2 = this.ReviseCityName(city);
                string county2 = this.ReviseCountyName(county);
                result = new DistrictInfo(province2, city2, county2);
            }
            else
            {
                result = districtInfo;
            }
            return result;
        }
        private void ResolveProvinceName(ref string inputAddress, ref string province, ref string city)
        {
            bool flag = false;
            string text = string.Empty;
            string[] array = new string[]
            {
                "省",
                "区",
                "市"
            };
            string[] array2 = array;
            for (int i = 0; i < array2.Length; i++)
            {
                string text2 = array2[i];
                int num = inputAddress.IndexOf(text2);
                bool flag2 = num > 0;
                if (flag2)
                {
                    text = inputAddress.Substring(0, num + 1);
                    DistrictInfo districtInfo = this.FindByProvince(text);
                    bool flag3 = districtInfo != null;
                    if (flag3)
                    {
                        province = districtInfo.Province;
                        inputAddress = inputAddress.Substring(num + 1);
                        bool flag4 = text2.Equals("市");
                        if (flag4)
                        {
                            city = province;
                        }
                        flag = true;
                        break;
                    }
                    bool flag5 = text2.Equals("省");
                    if (flag5)
                    {
                        int length = text.IndexOf("省");
                        string province2 = text.Substring(0, length);
                        districtInfo = this.FindByProvince(province2);
                        bool flag6 = districtInfo != null;
                        if (flag6)
                        {
                            province = districtInfo.Province;
                            inputAddress = inputAddress.Substring(num + 1);
                            flag = true;
                            break;
                        }
                    }
                }
            }
            bool flag7 = !flag;
            if (flag7)
            {
                this.ResolveProvinceNameFromMap(ref inputAddress, ref province, ref city);
            }
        }
        public void ResolveProvinceNameFromMap(ref string inputAddress, ref string province, ref string city)
        {
            string address = inputAddress;
            using (var db = new DistrictServiceContext())
            {
                var p = db.ChinaAreaDatas.Where(c => c.levelType == 1).FirstOrDefault(s => address.IndexOf(s.ShortName) == 0);
                if(p==null)
                {
                    Dictionary<string, string> provinceMap = DistrictMaps.ProvinceMap;

                    string text = provinceMap.Keys.FirstOrDefault((string mapKey) => address.IndexOf(mapKey) == 0);
                    bool flag = text != null;
                    if (flag)
                    {
                        province = provinceMap[text];
                        bool flag2 = province.EndsWith("市");
                        if (flag2)
                        {
                            city = province;
                        }
                        inputAddress = inputAddress.Replace(text, string.Empty).Trim();
                    }
                }
                else
                {
                    province = p.Name;
                    bool flag2 = province.EndsWith("市");
                    if (flag2)
                    {
                        city = province;
                    }
                    inputAddress = inputAddress.Replace(p.ShortName, string.Empty).Trim();
                }
            }
          
           
        }
        public string GetProvinceNameFromMap(string province)
        {
            return DistrictMaps.ProvinceMap.ContainsKey(province) ? DistrictMaps.ProvinceMap[province] : province;
        }
        private void ResolveCityName(ref string inputAddress, ref string province, ref string city)
        {
            bool flag = false;
            string city2 = string.Empty;
            string[] array = new string[]
            {
                "市",
                "州",
                "地区"
            };
            bool flag2 = !string.IsNullOrEmpty(city) && !string.IsNullOrEmpty(province);
            if (!flag2)
            {
                string[] words = new string[]
                {
                    "省",
                    "州",
                    "市"
                };
                inputAddress = inputAddress.RemoveWordsAtBegin(words);
                string[] array2 = array;
                for (int i = 0; i < array2.Length; i++)
                {
                    string text = array2[i];
                    int num = inputAddress.IndexOf(text);
                    bool flag3 = num > 0;
                    if (flag3)
                    {
                        city2 = inputAddress.Substring(0, num + text.Length);
                        DistrictInfo districtInfo = this.FindByCity(city2);
                        bool flag4 = districtInfo != null;
                        if (flag4)
                        {
                            city = districtInfo.City;
                            inputAddress = inputAddress.Substring(num + text.Length);
                            bool flag5 = string.IsNullOrEmpty(province);
                            if (flag5)
                            {
                                province = districtInfo.Province;
                            }
                            flag = true;
                            break;
                        }
                    }
                }
                bool flag6 = !flag;
                if (flag6)
                {
                    this.ResolveCityNameFromMap(ref inputAddress, ref province, ref city);
                }
            }
        }
        public void ResolveCityNameFromMap(ref string inputAddress, ref string province, ref string city)
        {
            string address = inputAddress;
            using (var db = new DistrictServiceContext())
            {
                string p = province;
                if (!string.IsNullOrEmpty(p))
                {
                    try
                    {
                        var pinfo = db.ChinaAreaDatas.FirstOrDefault(c => c.levelType == 1 && (c.Name == p||p.IndexOf(c.ShortName)==0));

                        var cinfo = db.ChinaAreaDatas.Where(c => c.levelType == 2 && c.ParentId == pinfo.ID).FirstOrDefault(c => address.IndexOf(c.ShortName) == 0);
                        if (cinfo == null)
                        {
                            Dictionary<string, string> cityMap = DistrictMaps.CityMap;
                            string text = cityMap.Keys.FirstOrDefault((string mapKey) => address.IndexOf(mapKey) == 0);
                            bool flag = text != null;
                            if (flag)
                            {
                                city = cityMap[text];
                                inputAddress = inputAddress.Replace(text, string.Empty).Trim();
                                DistrictInfo districtInfo = this.FindByCity(city);
                                bool flag2 = districtInfo != null && string.IsNullOrEmpty(province);
                                if (flag2)
                                {
                                    province = districtInfo.Province;
                                }
                            }
                        }
                        else
                        {
                            inputAddress = inputAddress.Replace(cinfo.ShortName, string.Empty).Trim();
                            city = cinfo.Name;
                        }
                    }
                    catch (Exception ex)
                    {
                        Util.Logs.Log.GetLog(nameof(MemoryDistrictService)).Error($"msg:{ex.Message},stacktrace:{ex.StackTrace},addr:{inputAddress}");
                        
                    }
                   
                }
            }

          
            
        }
        public string GetCityNameFromMap(string city)
        {
            return DistrictMaps.CityMap.ContainsKey(city) ? DistrictMaps.CityMap[city] : city;
        }
        private void ResolveCountyName(ref string inputAddress, ref string province, ref string city, ref string county)
        {
            DistrictInfo districtInfo = null;
            bool flag = false;
            string county2 = string.Empty;
            string[] array = new string[]
            {
                "区",
                "县",
                "市"
            };
            string[] array2 = array;
            for (int i = 0; i < array2.Length; i++)
            {
                string value = array2[i];
                int num = inputAddress.IndexOf(value);
                bool flag2 = num > 0;
                if (flag2)
                {
                    county2 = inputAddress.Substring(0, num + 1);
                    bool flag3 = string.IsNullOrEmpty(province) && string.IsNullOrEmpty(city);
                    if (flag3)
                    {
                        districtInfo = this.FindByCounty(county2);
                    }
                    else
                    {
                        bool flag4 = !string.IsNullOrEmpty(province) && string.IsNullOrEmpty(city);
                        if (flag4)
                        {
                            districtInfo = this.FindByProvinceAndCounty(province, county2);
                        }
                        else
                        {
                            bool flag5 = !string.IsNullOrEmpty(city) && string.IsNullOrEmpty(province);
                            if (flag5)
                            {
                                districtInfo = this.FindByCityAndCounty(city, county2);
                            }
                            else
                            {
                                bool flag6 = !string.IsNullOrEmpty(province) && !string.IsNullOrEmpty(city);
                                if (flag6)
                                {
                                    districtInfo = this.FindByProvinceAndCityAndCounty(province, city, county2);
                                }
                            }
                        }
                    }
                    bool flag7 = districtInfo != null;
                    if (flag7)
                    {
                        county = districtInfo.County;
                        inputAddress = inputAddress.Substring(num + 1);
                        bool flag8 = string.IsNullOrEmpty(city);
                        if (flag8)
                        {
                            city = districtInfo.City;
                        }
                        bool flag9 = string.IsNullOrEmpty(province);
                        if (flag9)
                        {
                            province = districtInfo.Province;
                        }
                        flag = true;
                        break;
                    }
                }
            }
            bool flag10 = !flag;
            if (flag10)
            {
                this.ResolveCountyNameFromMap(ref inputAddress, ref province, ref city, ref county);
            }
        }
        public void ResolveCountyNameFromMap(ref string inputAddress, ref string province, ref string city, ref string county)
        {
            string address = inputAddress;
            using (var db = new DistrictServiceContext())
            {
                try
                {
                    string sourcecity = city;
                    if (!string.IsNullOrEmpty(sourcecity))
                    {
                        var cityinfo = db.ChinaAreaDatas.FirstOrDefault(c => c.levelType == 2 && c.Name == sourcecity);
                        OMS.Models.ChinaAreaData countyinfo = null;
                        if (cityinfo != null)
                        {
                            countyinfo = db.ChinaAreaDatas.Where(c => c.levelType == 3 && c.ParentId == cityinfo.ID).FirstOrDefault(c => address.Contains(c.ShortName) == true);
                        }
                        else //适用于地级市降级为区，县
                        {
                            Dictionary<string, string> cityMap = DistrictMaps.CityMap;
                            string text = cityMap.Keys.FirstOrDefault((string mapKey) => sourcecity.IndexOf(mapKey) == 0);
                            bool flag = text != null;
                            if (flag)
                            {
                                city = cityMap[text];
                                ResolveCountyNameFromMap(ref inputAddress, ref province, ref city, ref county);
                                return;
                                
                            }
                        }
                        if (countyinfo == null)
                        {
                            DistrictInfo districtInfo = null;
                          
                            Dictionary<string, string> countyMap = DistrictMaps.CountyMap;
                            string text = countyMap.Keys.FirstOrDefault((string mapKey) => address.IndexOf(mapKey) == 0);
                            bool flag = text != null;
                            if (flag)
                            {
                                county = countyMap[text];
                                inputAddress = inputAddress.Replace(text, string.Empty).Trim();
                                bool flag2 = string.IsNullOrEmpty(province) && string.IsNullOrEmpty(city);
                                if (flag2)
                                {
                                    districtInfo = this.FindByCounty(county);
                                }
                                else
                                {
                                    bool flag3 = !string.IsNullOrEmpty(province) && string.IsNullOrEmpty(city);
                                    if (flag3)
                                    {
                                        districtInfo = this.FindByProvinceAndCounty(province, county);
                                    }
                                    else
                                    {
                                        bool flag4 = !string.IsNullOrEmpty(city) && string.IsNullOrEmpty(province);
                                        if (flag4)
                                        {
                                            districtInfo = this.FindByCityAndCounty(city, county);
                                        }
                                        else
                                        {
                                            bool flag5 = !string.IsNullOrEmpty(province) && !string.IsNullOrEmpty(city);
                                            if (flag5)
                                            {
                                                districtInfo = this.FindByProvinceAndCityAndCounty(province, city, county);
                                            }
                                        }
                                    }
                                }
                                bool flag6 = districtInfo != null && string.IsNullOrEmpty(city);
                                if (flag6)
                                {
                                    city = districtInfo.City;
                                }
                                bool flag7 = districtInfo != null && string.IsNullOrEmpty(province);
                                if (flag7)
                                {
                                    province = districtInfo.Province;
                                }
                            }
                        }
                        else
                        {
                            inputAddress = inputAddress.Replace(countyinfo.ShortName, string.Empty).Trim();
                            county = countyinfo.Name;
                        }
                    }
                    else
                    {
                        DistrictInfo districtInfo = null;

                        Dictionary<string, string> countyMap = DistrictMaps.CountyMap;
                        string text = countyMap.Keys.FirstOrDefault((string mapKey) => address.IndexOf(mapKey) == 0);
                        bool flag = text != null;
                        if (flag)
                        {
                            county = countyMap[text];
                            inputAddress = inputAddress.Replace(text, string.Empty).Trim();
                            bool flag2 = string.IsNullOrEmpty(province) && string.IsNullOrEmpty(city);
                            if (flag2)
                            {
                                districtInfo = this.FindByCounty(county);
                            }
                            else
                            {
                                bool flag3 = !string.IsNullOrEmpty(province) && string.IsNullOrEmpty(city);
                                if (flag3)
                                {
                                    districtInfo = this.FindByProvinceAndCounty(province, county);
                                }
                                else
                                {
                                    bool flag4 = !string.IsNullOrEmpty(city) && string.IsNullOrEmpty(province);
                                    if (flag4)
                                    {
                                        districtInfo = this.FindByCityAndCounty(city, county);
                                    }
                                    else
                                    {
                                        bool flag5 = !string.IsNullOrEmpty(province) && !string.IsNullOrEmpty(city);
                                        if (flag5)
                                        {
                                            districtInfo = this.FindByProvinceAndCityAndCounty(province, city, county);
                                        }
                                    }
                                }
                            }
                            bool flag6 = districtInfo != null && string.IsNullOrEmpty(city);
                            if (flag6)
                            {
                                city = districtInfo.City;
                            }
                            bool flag7 = districtInfo != null && string.IsNullOrEmpty(province);
                            if (flag7)
                            {
                                province = districtInfo.Province;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Util.Logs.Log.GetLog(nameof(MemoryDistrictService)).Error($"msg:{ex.Message},stacktrace:{ex.StackTrace},addr:{inputAddress}");

                }
               
            }


          
        }
        public string GetCountyNameFormMap(string county)
        {
            return DistrictMaps.CountyMap.ContainsKey(county) ? DistrictMaps.CountyMap[county] : county;
        }
        private void ResolveAddressFromCustomMap(ref string inputAddress, ref string province, ref string city, ref string county)
        {
            string address = inputAddress;
           

            Dictionary<string, AddressInfo> customMap = DistrictMaps.CustomMap;
            string text = customMap.Keys.FirstOrDefault((string mapKey) => mapKey.Equals(address));
            bool flag = text != null;
            if (flag)
            {
                AddressInfo addressInfo = customMap[text];
                bool flag2 = string.IsNullOrEmpty(province);
                if (flag2)
                {
                    province = addressInfo.Province;
                }
                bool flag3 = string.IsNullOrEmpty(city);
                if (flag3)
                {
                    city = addressInfo.City;
                }
                bool flag4 = string.IsNullOrEmpty(county);
                if (flag4)
                {
                    county = addressInfo.County;
                }
                inputAddress = addressInfo.Address;
            }
        }
        private DistrictInfo FindByCity(string city)
        {
            var foo= this._districtList.FirstOrDefault((DistrictInfo d) => d.City.Contains(city));
           
            return foo;
        }
        private DistrictInfo FindByCityAndCounty(string city, string county)
        {
            return this._districtList.FirstOrDefault((DistrictInfo d) => d.City.Contains(city) && d.County.Contains(county));
        }
        private DistrictInfo FindByCounty(string county)
        {
            return this._districtList.FirstOrDefault((DistrictInfo d) => d.County.Contains(county));
        }
        private DistrictInfo FindBy(string keyword)
        {
            return this._districtList.FirstOrDefault((DistrictInfo d) => d.Province.Contains(keyword) || d.City.Contains(keyword) || d.County.Contains(keyword));
        }
        private DistrictInfo FindByProvince(string province)
        {
            return this._districtList.FirstOrDefault((DistrictInfo d) => d.Province.Contains(province));
        }
        private DistrictInfo FindByProvinceAndCity(string province, string city)
        {
            return this._districtList.FirstOrDefault((DistrictInfo d) => d.Province.Contains(province) && d.City.Contains(city));
        }
        private DistrictInfo FindByProvinceAndCityAndCounty(string province, string city, string county)
        {
            return this._districtList.FirstOrDefault((DistrictInfo d) => d.Province.Contains(province) && d.City.Contains(city) && d.County.Contains(county));
        }
        private DistrictInfo FindByProvinceAndCounty(string province, string county)
        {
            return this._districtList.FirstOrDefault((DistrictInfo d) => d.Province.Contains(province) && d.County.Contains(county));
        }
    }
}
