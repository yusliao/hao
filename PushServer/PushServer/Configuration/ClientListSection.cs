using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;

namespace PushServer.Configuration
{
    class ClientListSection:ConfigurationSection
    {
        [ConfigurationProperty("name")]
        public string Name { get; set; }
        [ConfigurationProperty("clients")]
        public ClientCollection Clients { get { return this["clients"] as ClientCollection; } }
    }
}
