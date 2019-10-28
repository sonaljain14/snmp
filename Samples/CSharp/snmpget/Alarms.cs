using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace snmpget
{
    public class Alarms
    {
        public static readonly string UPSAlarmOnBattery = ".1.3.6.1.2.1.33.1.6.3.2";
        public static readonly string UPSAlarmLowBattery = ".1.3.6.1.2.1.33.1.6.3.3";
        public static readonly string UPSAlarmDepletedBattery = ".1.3.6.1.2.1.33.1.6.3.4";
        public static readonly string UPSAlarmInputBad = ".1.3.6.1.2.1.33.1.6.3.6";
    }
}
