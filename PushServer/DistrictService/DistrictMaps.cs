using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
namespace DistrictService
{
    public static class DistrictMaps
    {
        private static readonly Dictionary<string, string> _provinceMap;
        private static readonly Dictionary<string, string> _cityMap;
        private static readonly Dictionary<string, string> _countyMap;
        private static readonly Dictionary<string, AddressInfo> _customMap;
        public static Dictionary<string, string> ProvinceMap
        {
            get
            {
                return DistrictMaps._provinceMap;
            }
        }
        public static Dictionary<string, string> CityMap
        {
            get
            {
                return DistrictMaps._cityMap;
            }
        }
        public static Dictionary<string, string> CountyMap
        {
            get
            {
                return DistrictMaps._countyMap;
            }
        }
        public static Dictionary<string, AddressInfo> CustomMap
        {
            get
            {
                return DistrictMaps._customMap;
            }
        }
        static DistrictMaps()
        {
            string provinceMapFileName = Configuration.ProvinceMapFileName;
            string cityMapFileName = Configuration.CityMapFileName;
            string countyMapFileName = Configuration.CountyMapFileName;
            string customMapFileName = Configuration.CustomMapFileName;
            bool flag = !File.Exists(provinceMapFileName);
            if (flag)
            {
                throw new FileNotFoundException(provinceMapFileName);
            }
            bool flag2 = !File.Exists(cityMapFileName);
            if (flag2)
            {
                throw new FileNotFoundException(cityMapFileName);
            }
            bool flag3 = !File.Exists(countyMapFileName);
            if (flag3)
            {
                throw new FileNotFoundException(countyMapFileName);
            }
            bool flag4 = !File.Exists(customMapFileName);
            if (flag4)
            {
                throw new FileNotFoundException(customMapFileName);
            }
            DistrictMaps._provinceMap = new Dictionary<string, string>(DistrictMaps.ParseMapFileContent(provinceMapFileName));
            DistrictMaps._cityMap = new Dictionary<string, string>(DistrictMaps.ParseMapFileContent(cityMapFileName));
            DistrictMaps._countyMap = new Dictionary<string, string>(DistrictMaps.ParseMapFileContent(countyMapFileName));
            DistrictMaps._customMap = new Dictionary<string, AddressInfo>(DistrictMaps.ParseCustomMapFileContent(customMapFileName));
        }
        private static Dictionary<string, string> ParseMapFileContent(string fileName)
        {
            Dictionary<string, string> dictionary = new Dictionary<string, string>();
            using (StreamReader streamReader = new StreamReader(fileName))
            {
                string text = streamReader.ReadToEnd();
                string[] array = text.Split(new string[]
                {
                    ","
                }, StringSplitOptions.RemoveEmptyEntries);
                string[] array2 = array;
                for (int i = 0; i < array2.Length; i++)
                {
                    string text2 = array2[i];
                    string[] array3 = text2.Split(new string[]
                    {
                        "="
                    }, StringSplitOptions.RemoveEmptyEntries);
                    string key = array3.First<string>().Trim();
                    string value = array3.Last<string>().Trim();
                    bool flag = !dictionary.ContainsKey(key) && array3.Length == 2;
                    if (flag)
                    {
                        dictionary.Add(key, value);
                    }
                }
            }
            return dictionary;
        }
        private static Dictionary<string, AddressInfo> ParseCustomMapFileContent(string fileName)
        {
            Dictionary<string, AddressInfo> dictionary = new Dictionary<string, AddressInfo>();
            using (StreamReader streamReader = new StreamReader(fileName))
            {
                string text = streamReader.ReadToEnd();
                string[] array = text.Split(new string[]
                {
                    ","
                }, StringSplitOptions.RemoveEmptyEntries);
                string[] array2 = array;
                for (int i = 0; i < array2.Length; i++)
                {
                    string text2 = array2[i];
                    string[] source = text2.Split(new string[]
                    {
                        "="
                    }, StringSplitOptions.RemoveEmptyEntries);
                    string key = source.First<string>().Trim();
                    string text3 = source.Last<string>().Trim();
                    string[] array3 = text3.Split(new char[]
                    {
                        '/'
                    });
                    bool flag = !dictionary.ContainsKey(key) && array3.Length == 4;
                    if (flag)
                    {
                        dictionary.Add(key, new AddressInfo(array3[0], array3[1], array3[2], array3[3]));
                    }
                }
            }
            return dictionary;
        }
    }
}
