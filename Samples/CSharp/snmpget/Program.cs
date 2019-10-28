// typical usage
// snmpget -c=public -v=1 localhost 1.3.6.1.2.1.1.1.0
// snmpget -c=public -v=2 localhost 1.3.6.1.2.1.1.1.0
// snmpget -v=3 -l=noAuthNoPriv -u=neither localhost 1.3.6.1.2.1.1.1.0
// snmpget -v=3 -l=authNoPriv -a=MD5 -A=authentication -u=authen localhost 1.3.6.1.2.1.1.1.0
// snmpget -v=3 -l=authPriv -a=MD5 -A=authentication -x=DES -X=privacyphrase -u=privacy localhost 1.3.6.1.2.1.1.1.0

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using Lextm.SharpSnmpLib;
using Lextm.SharpSnmpLib.Messaging;
using Mono.Options;
using Serilog;
using snmpget;

namespace SnmpGet
{
    public enum Company
    {
        Astrodyne = 0,
        Triplite = 1
    }
    internal static class Program
    {
        private static UPSData _lastKnownData;
        private static readonly ObjectIdentifier UPSBatteryStatusOid = new ObjectIdentifier(".1.3.6.1.2.1.33.1.2.1.0");
        private static readonly ObjectIdentifier UPSBatteryOutputPower_Astrodyne = new ObjectIdentifier(".1.3.6.1.2.1.33.1.4.4.1.4.0");
        private static readonly ObjectIdentifier UPSBatteryOutputPower_TripLite = new ObjectIdentifier(".1.3.6.1.2.1.33.1.4.4.1.4.1");
        private static readonly ObjectIdentifier UPSAlarmOnBattery = new ObjectIdentifier(".1.3.6.1.2.1.33.1.6.3.2");
        private static readonly ObjectIdentifier UPSSecondsOnBattery = new ObjectIdentifier(".1.3.6.1.2.1.33.1.2.2.0");
        private static readonly ObjectIdentifier UPSOutputSourceOid = new ObjectIdentifier(".1.3.6.1.2.1.33.1.4.1.0");
        private static readonly ObjectIdentifier UPSChargeRemainingInPercentageOid = new ObjectIdentifier(".1.3.6.1.2.1.33.1.2.4.0");
        private static readonly ObjectIdentifier UPSChargeRemainingInMinutesOid = new ObjectIdentifier(".1.3.6.1.2.1.33.1.2.3.0");
        private static readonly ObjectIdentifier UPSManufacturerOid = new ObjectIdentifier(".1.3.6.1.2.1.33.1.1.1.0");
        private static readonly ObjectIdentifier UPSAlarmTable = new ObjectIdentifier(".1.3.6.1.2.1.33.1.6.2");
        private static List<Variable> _variableList;
        private static IPAddress _ip;
        private static VersionCode _version = VersionCode.V1;
        private static string _community = "public";
        private static int _timeout = 1000;
        private static Company _company = Company.Astrodyne;


        public static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration().ReadFrom.AppSettings()
                //.WriteTo.Console()
                //.WriteTo.File("log.log", rollingInterval: RollingInterval.Day)
                .CreateLogger();

            var showHelp = false;
            var showVersion = false;

            var p = new OptionSet()
                .Add("c:", "Community name, (default is public)", delegate (string v)
                {
                    if (v != null) _community = v;
                })
                .Add("l:", "Security level, (default is noAuthNoPriv)", delegate (string v)
                {
                    if (v.ToUpperInvariant() == "NOAUTHNOPRIV")
                    {
                    }
                    else if (v.ToUpperInvariant() == "AUTHNOPRIV")
                    {
                    }
                    else if (v.ToUpperInvariant() == "AUTHPRIV")
                    {
                    }
                    else
                        throw new ArgumentException("no such security mode: " + v);
                })
                .Add("a:", "Authentication method (MD5 or SHA)", delegate (string v) { })
                .Add("A:", "Authentication passphrase", delegate (string v) { })
                .Add("x:", "Privacy method", delegate (string v) { })
                .Add("X:", "Privacy passphrase", delegate (string v) { })
                .Add("u:", "Security name", delegate (string v) { })
                .Add("C:", "Context name", delegate (string v) { })
                .Add("h|?|help", "Print this help information.", delegate (string v) { showHelp = v != null; })
                .Add("V", "Display version number of this application.", delegate (string v) { showVersion = v != null; })
                .Add("d", "Display message dump", delegate (string v) { })
                .Add("t:", "Timeout value (unit is second).", delegate (string v) { _timeout = int.Parse(v) * 1000; })
                .Add("type:", "Astrodyne (default) or Triplite (t)", delegate (string v)
                {
                    if (string.IsNullOrWhiteSpace(v))
                    {
                        _company = Company.Astrodyne;
                    }
                    else if (v.ToLower() == "t")
                    {
                        _company = Company.Triplite;
                    }
                    else
                    {
                        _company = Company.Astrodyne;
                    }
                })
                .Add("r:", "Retry count (default is 0)", delegate (string v) { })
                .Add("v|version:", "SNMP version (1, 2, and 3 are currently supported)", delegate (string v)
                {
                    if (v == "2c") v = "2";

                    switch (int.Parse(v))
                    {
                        case 1:
                            _version = VersionCode.V1;
                            break;
                        case 2:
                            _version = VersionCode.V2;
                            break;
                        case 3:
                            _version = VersionCode.V3;
                            break;
                        default:
                            throw new ArgumentException("no such version: " + v);
                    }
                });

            if (args.Length == 0)
            {
                ShowHelp(p);
                return;
            }

            List<string> extra;
            try
            {
                extra = p.Parse(args);
            }
            catch (OptionException ex)
            {
                Log.Error(ex, "Error in parsing options");
                return;
            }

            if (showHelp)
            {
                ShowHelp(p);
                return;
            }

            if (extra.Count < 1)
            {
                Log.Error("invalid variable number: {VariableCount}", extra.Count);
                return;
            }

            if (showVersion)
            {
                Log.Information(Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyVersionAttribute>().Version);
                return;
            }

            var parsed = IPAddress.TryParse(extra[0], out _ip);
            if (!parsed)
            {
                var addresses = Dns.GetHostAddressesAsync(extra[0]);
                addresses.Wait();
                foreach (var address in
                    addresses.Result.Where(address => address.AddressFamily == AddressFamily.InterNetwork))
                {
                    _ip = address;
                    break;
                }

                if (_ip == null)
                {
                    Log.Error("invalid host or wrong IP address found: {IPAddress}", extra[0]);
                    return;
                }
            }

            _variableList = new List<Variable>
            {
                new Variable(UPSBatteryStatusOid),
                new Variable(UPSOutputSourceOid),
                new Variable(UPSChargeRemainingInPercentageOid),
                new Variable(UPSChargeRemainingInMinutesOid),
                new Variable(UPSManufacturerOid),
                new Variable(UPSSecondsOnBattery),
            };
            switch (_company)
            {
                case Company.Triplite:
                    _variableList.Add(new Variable(UPSBatteryOutputPower_TripLite));
                    break;
                default:
                    _variableList.Add(new Variable(UPSBatteryOutputPower_Astrodyne));
                    break;
            }

            try
            {
                if (_version != VersionCode.V3)
                {
                    var timer = new Timer(CaptureUPSData, null, 1000, 1000);
                }
            }
            catch (SnmpException ex)
            {
                Log.Error(ex, ex.Message);
            }
            catch (SocketException ex)
            {
                Log.Error(ex, ex.Message);
            }

            Log.Information("Press enter key to exit the application.");
            Console.ReadLine();
            Log.CloseAndFlush();
        }

        private static void CaptureUPSData(object state)
        {
            var receiver = new IPEndPoint(_ip, 161);
            var upsData = new UPSData();
            try
            {
                switch (_company)
                {
                    case Company.Triplite:
                        foreach (var item in Messenger.Get(_version, receiver, new OctetString(_community), _variableList, _timeout))
                        {
                            if (item.Id == UPSBatteryStatusOid)
                                upsData.BatteryStatus = (BatteryStatus)Convert.ToInt32(item.Data.ToString());
                            else if (item.Id == UPSChargeRemainingInMinutesOid)
                                upsData.BatteryRemainingInMinutes = Convert.ToInt32(item.Data.ToString());
                            else if (item.Id == UPSChargeRemainingInPercentageOid)
                                upsData.BatteryChargeRemainingInPercentage = Convert.ToInt32(item.Data.ToString());
                            else if (item.Id == UPSManufacturerOid)
                                upsData.Manufacturer = item.Data.ToString();
                            else if (item.Id == UPSOutputSourceOid)
                                upsData.DeviceMode = (DeviceMode)Convert.ToInt32(item.Data.ToString());
                            else if (item.Id == UPSSecondsOnBattery)
                                upsData.SecondsOnBattery = Convert.ToInt32(item.Data.ToString());
                            else if (item.Id == UPSBatteryOutputPower_TripLite)
                                upsData.BatteryOutputPower = Convert.ToInt32(item.Data.ToString());
                        }

                        break;
                    default:
                        bool tableMode = false;
                        //initialize
                        upsData.DeviceMode = DeviceMode.Normal;
                        upsData.BatteryStatus = BatteryStatus.Normal;

                        foreach (var item in Messenger.Get(_version, receiver, new OctetString(_community), _variableList, _timeout))
                            if (item.Id == UPSChargeRemainingInMinutesOid)
                                upsData.BatteryRemainingInMinutes = Convert.ToInt32(item.Data.ToString());
                            else if (item.Id == UPSChargeRemainingInPercentageOid)
                                upsData.BatteryChargeRemainingInPercentage = 100 - Convert.ToInt32(item.Data.ToString());
                            else if (item.Id == UPSManufacturerOid)
                                upsData.Manufacturer = item.Data.ToString();
                            else if (item.Id == UPSSecondsOnBattery)
                                upsData.SecondsOnBattery = Convert.ToInt32(item.Data.ToString());
                            else if (item.Id == UPSBatteryOutputPower_Astrodyne)
                                upsData.BatteryOutputPower = Convert.ToInt32(item.Data.ToString());

                        if (tableMode)
                        {
                            foreach (var variable in Messenger.GetTable(_version, receiver, new OctetString(_community), UPSAlarmTable, 10000, 100000))
                            {
                                if (variable.Id.TypeCode == SnmpType.ObjectIdentifier)
                                {
                                    if (variable.Id.ToString() == Alarms.UPSAlarmLowBattery)
                                        upsData.BatteryStatus = BatteryStatus.Low;
                                    if (variable.Id.ToString() == Alarms.UPSAlarmOnBattery)
                                        upsData.DeviceMode = DeviceMode.Battery;
                                    else if (variable.Id.ToString() == Alarms.UPSAlarmDepletedBattery)
                                        upsData.BatteryStatus = BatteryStatus.Depleted;
                                }
                            }
                        }
                        else
                        {
                            var variables = new List<Variable>
                            {
                                new Variable(new ObjectIdentifier(Alarms.UPSAlarmLowBattery)),
                                new Variable(new ObjectIdentifier(Alarms.UPSAlarmOnBattery)),
                                new Variable(new ObjectIdentifier(Alarms.UPSAlarmDepletedBattery))
                            };
                            var itemCount = Messenger.Walk(_version, receiver, new OctetString(_community), UPSAlarmTable, variables, _timeout, WalkMode.WithinSubtree);

                            if (itemCount == 0)
                            {
                                Log.Verbose("nothing in the alarm table");
                            }
                            else
                            {
                                foreach (var variable in variables.Where(variable => variable.Data.TypeCode == SnmpType.ObjectIdentifier))
                                {
                                    if (variable.Data.ToString() == new ObjectIdentifier(Alarms.UPSAlarmOnBattery).ToString())
                                    {
                                        upsData.DeviceMode = DeviceMode.Battery;
                                    }
                                    else if (variable.Data.ToString() == new ObjectIdentifier(Alarms.UPSAlarmLowBattery).ToString())
                                    {
                                        upsData.BatteryStatus = BatteryStatus.Low;
                                    }
                                    else if (variable.Data.ToString() == new ObjectIdentifier(Alarms.UPSAlarmDepletedBattery).ToString())
                                    {
                                        upsData.BatteryStatus = BatteryStatus.Depleted;
                                    }
                                }
                            }
                        }
                        break;
                }

                LogIfUPSDataChanges(upsData);
            }
            catch (Exception e)
            {
                Log.Error(e, "Failed to get data from the UPS");
            }
        }

        private static void LogIfUPSDataChanges(UPSData upsData)
        {
            //compare the UPS data with last known state
            if (_lastKnownData == null)
            {
                _lastKnownData = upsData;
                Log.Information("{@UPSData}", upsData);
            }
            else
            {
                if (_lastKnownData.BatteryChargeRemainingInPercentage != upsData.BatteryChargeRemainingInPercentage ||
                    _lastKnownData.BatteryRemainingInMinutes != upsData.BatteryRemainingInMinutes ||
                    _lastKnownData.BatteryStatus != upsData.BatteryStatus ||
                    _lastKnownData.DeviceMode != upsData.DeviceMode)
                {
                    Log.Information("{@UPSData}", upsData);
                    _lastKnownData = upsData;
                }
                else
                {
                    Log.Verbose("{@UPSData}", upsData);
                }
            }
        }

        private static void ShowHelp(OptionSet optionSet)
        {
            Log.Information("#SNMP is available at https://sharpsnmp.com");
            Log.Information("snmpget [Options] IP-address|host-name OID [OID] ...");
            Log.Information("Options:");
            Log.Information("Options Set {@OptionSet}", optionSet);
        }
    }
}
