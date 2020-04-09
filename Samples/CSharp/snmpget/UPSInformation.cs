using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace snmpget
{
    internal class UPSInformation
    {
        public string Manufacturer { get; set; }

        public bool Ipv4DHCPEnabled { get; set; }

        public string Ipv4IP { get; set; }

        public bool AutoRestart { get; set; } 
    }
}
