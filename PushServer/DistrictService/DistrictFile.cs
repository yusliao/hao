using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
namespace DistrictService
{
    public  class DistrictFile
    {
        private class Item
        {
            public string Code
            {
                get;
                set;
            }
            public string Name
            {
                get;
                set;
            }
            public Item(string code, string name)
            {
                this.Code = code;
                this.Name = name;
            }
        }
        private class Province : DistrictFile.Item
        {
            public List<DistrictFile.City> Cities
            {
                get;
                set;
            }
            public Province(string code, string name) : base(code, name)
            {
                this.Cities = new List<DistrictFile.City>();
            }
        }
        private class City : DistrictFile.Item
        {
            public List<DistrictFile.County> Counties
            {
                get;
                set;
            }
            public City(string code, string name) : base(code, name)
            {
                this.Counties = new List<DistrictFile.County>();
            }
        }
        private class County : DistrictFile.Item
        {
            public County(string code, string name) : base(code, name)
            {
            }
        }
        private static readonly List<DistrictInfo> _districtList= new List<DistrictInfo>(DistrictFile.BuildDistrictInfoList());
        public static List<DistrictInfo> DistrictList
        {
            get
            {
                return DistrictFile._districtList;
            }
        }
       
        private static List<DistrictInfo> BuildDistrictInfoList()
        {
            List<DistrictInfo> list = new List<DistrictInfo>();
            List<DistrictFile.Province> list2 = DistrictFile.ParseDistrictFileContent();
            bool flag = list2.Any<DistrictFile.Province>();
            if (flag)
            {
                foreach (DistrictFile.Province current in list2)
                {
                    foreach (DistrictFile.City current2 in current.Cities)
                    {
                        foreach (DistrictFile.County current3 in current2.Counties)
                        {
                            list.Add(new DistrictInfo(current.Name, current2.Name, current3.Name));
                        }
                        bool flag2 = current2.Counties.Count == 0;
                        if (flag2)
                        {
                            list.Add(new DistrictInfo(current.Name, current2.Name, current2.Name));
                        }
                    }
                }
            }
            return list;
        }
        private static List<DistrictFile.Province> ParseDistrictFileContent()
        {
            string districtFileName = Configuration.DistrictFileName;
            bool flag = !File.Exists(districtFileName);
            if (flag)
            {
                throw new FileNotFoundException(districtFileName);
            }
            List<DistrictFile.Province> list = new List<DistrictFile.Province>();
            using (StreamReader streamReader = new StreamReader(districtFileName))
            {
                string text = string.Empty;
           
                while (!string.IsNullOrEmpty((text = streamReader.ReadLine())))
                {
                        
                    string[] source = text.Split(new string[]
                    {
                    " "
                    }, StringSplitOptions.RemoveEmptyEntries);
                    string text2 = source.First<string>().Trim();
                    string text3 = source.Last<string>().Trim();
                    bool flag2 = text3.Equals("市辖区") || text3.Equals("县");
                   
                    if (!flag2)
                    {
                        bool flag3 = text2.EndsWith("0000");
                        if (flag3)
                        {
                            DistrictFile.Province province = new DistrictFile.Province(text2, text3);
                            bool flag4 = text3.EndsWith("市");
                            if (flag4)
                            {
                                province.Cities.Add(new DistrictFile.City(text2, text3));
                            }
                            list.Add(province);
                        }
                        else
                        {
                            bool flag5 = text2.EndsWith("00");
                            if (flag5)
                            {
                                list.Last<DistrictFile.Province>().Cities.Add(new DistrictFile.City(text2, text3));
                            }
                            else
                            {
                                DistrictFile.City city = list.Last<DistrictFile.Province>().Cities.Last<DistrictFile.City>();
                                city.Counties.Add(new DistrictFile.County(text2, text3));
                            }
                        }
                    }
                   

                }
                
               
            }
            return list;
        }
    }
}
