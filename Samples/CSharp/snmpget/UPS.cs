using Lextm.SharpSnmpLib;

namespace snmpget
{
    public class UPS
    {
        public class Stat
        {
            public class OID
            {
                public static readonly ObjectIdentifier BatteryStatus = new ObjectIdentifier(".1.3.6.1.2.1.33.1.2.1.0");
                public static readonly ObjectIdentifier BatteryOutputPower_Astrodyne = new ObjectIdentifier(".1.3.6.1.2.1.33.1.4.4.1.4.0");
                public static readonly ObjectIdentifier BatteryOutputPower_TripLite = new ObjectIdentifier(".1.3.6.1.2.1.33.1.4.4.1.4.1");
                public static readonly ObjectIdentifier AlarmOnBattery = new ObjectIdentifier(".1.3.6.1.2.1.33.1.6.3.2");
                public static readonly ObjectIdentifier SecondsOnBattery = new ObjectIdentifier(".1.3.6.1.2.1.33.1.2.2.0");
                public static readonly ObjectIdentifier OutputSource = new ObjectIdentifier(".1.3.6.1.2.1.33.1.4.1.0");
                public static readonly ObjectIdentifier ChargeRemainingInPercentage = new ObjectIdentifier(".1.3.6.1.2.1.33.1.2.4.0");
                public static readonly ObjectIdentifier ChargeRemainingInMinutes = new ObjectIdentifier(".1.3.6.1.2.1.33.1.2.3.0");
            }

            public class Variables
            {
                public static readonly Variable BatteryStatus = new Variable(OID.BatteryStatus);
                public static readonly Variable BatteryOutputPower_Astrodyne = new Variable(OID.BatteryOutputPower_Astrodyne);
                public static readonly Variable BatteryOutputPower_TripLite = new Variable(OID.BatteryOutputPower_TripLite);
                public static readonly Variable AlarmOnBattery = new Variable(OID.AlarmOnBattery);
                public static readonly Variable SecondsOnBattery = new Variable(OID.SecondsOnBattery);
                public static readonly Variable OutputSource = new Variable(OID.OutputSource);
                public static readonly Variable ChargeRemainingInPercentage = new Variable(OID.ChargeRemainingInPercentage);
                public static readonly Variable ChargeRemainingInMinutes = new Variable(OID.ChargeRemainingInMinutes);
            }
        }

        public class Alarm
        {
            public class OID
            {
                public static readonly ObjectIdentifier Table = new ObjectIdentifier(".1.3.6.1.2.1.33.1.6.2");
                public static readonly ObjectIdentifier OnBattery = new ObjectIdentifier(".1.3.6.1.2.1.33.1.6.3.2");
                public static readonly ObjectIdentifier LowBattery = new ObjectIdentifier(".1.3.6.1.2.1.33.1.6.3.3");
                public static readonly ObjectIdentifier DepletedBattery = new ObjectIdentifier(".1.3.6.1.2.1.33.1.6.3.4");
                public static readonly ObjectIdentifier InputBad = new ObjectIdentifier(".1.3.6.1.2.1.33.1.6.3.6");
            }

            public class Variables
            {
                public static readonly Variable Table = new Variable(OID.Table);
                public static readonly Variable OnBattery = new Variable(OID.OnBattery);
                public static readonly Variable LowBattery = new Variable(OID.LowBattery);
                public static readonly Variable DepletedBattery = new Variable(OID.DepletedBattery);
                public static readonly Variable InputBad = new Variable(OID.InputBad);
            }
        }

        public class Information
        {
            public class OID
            {
                public static readonly ObjectIdentifier ManufacturerOid = new ObjectIdentifier(".1.3.6.1.2.1.33.1.1.1.0");
            }

            public class Variables
            {
                public static readonly Variable ManufacturerOid = new Variable(OID.ManufacturerOid);
            }

        }
    }
}
