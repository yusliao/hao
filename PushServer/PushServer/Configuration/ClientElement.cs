﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using Util.Configuration;

namespace PushServer.Configuration
{
    [Serializable]
    public class ClientElement : ConfigurationElementBase, IClientConfig
    {
        public ClientElement(string elementName)
        {
            Name = elementName;
        }
        public ClientElement() { }
        [ConfigurationProperty("ExcelOrderFolder", IsRequired=false)]
        public string ExcelOrderFolder
        {
            get
            {
                var folder = this["ExcelOrderFolder"] as string;
                if(string.IsNullOrEmpty(folder))
                {
                    var path = System.Configuration.ConfigurationManager.AppSettings["BasePath"];
                    if (!System.IO.Directory.Exists(path))
                        System.IO.Directory.CreateDirectory(path);
                    folder = $"{path}/Gaoxing/{Name}";
                }
                string[] dirs = folder.Split(',');
                foreach (var item in dirs)
                {
                    if(!string.IsNullOrEmpty(item)&&!System.IO.Directory.Exists(item))
                        System.IO.Directory.CreateDirectory(item);
                }
               
                return folder;
            }
        }

        [ConfigurationProperty("LastSyncDate")]
        public string LastSyncDate
        {
            get { return this["LastSyncDate"] as string; }
            set { this["LastSyncDate"] = value; }
        }

        [ConfigurationProperty("CreateLogisticsExcel", IsRequired = false)]
        public bool? CreateLogisticsExcel
        {
            get { return this["CreateLogisticsExcel"] as bool?; }
        }
        [ConfigurationProperty("CreateReport", IsRequired = false)]
        public bool? CreateReport => this["CreateReport"] as bool?;

        [ConfigurationProperty("SessionKey", IsRequired = false)]
        public string SessionKey =>this["SessionKey"] as string; 

        [ConfigurationProperty("ServerUrl", IsRequired = false)]
        public string ServerUrl => this["ServerUrl"] as string;
        [ConfigurationProperty("Tag", IsRequired = false)]
        public string Tag => this["Tag"] as string;
        [ConfigurationProperty("Enabled", IsRequired = false)]
        public bool? Enabled => this["Enabled"] as bool?;


    }
     public interface IClientConfig
    {
        string Name { get; }
        string Tag { get;}
        string ExcelOrderFolder { get; }
        string LastSyncDate { get; set; }
        bool? CreateLogisticsExcel { get;  }
        bool? CreateReport { get; }
        string SessionKey { get; }
        string ServerUrl { get; }
        /// <summary>
        /// 是否激活该渠道，影响推送报表
        /// </summary>
        bool? Enabled { get; }
    }
}
