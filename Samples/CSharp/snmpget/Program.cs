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
using CommandLine;
using Lextm.SharpSnmpLib;
using Lextm.SharpSnmpLib.Messaging;
using Serilog;
using Serilog.Core;
using snmpget;

namespace SnmpGet
{
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
        //private static IPAddress _ip;
        private static VersionCode _version = VersionCode.V1;
        private static string _community = "public";
        private static int _timeout = 1000;
        private static Company _company = Company.Astrodyne;
        /// <summary>
        /// Holds the options for the extractor
        /// </summary>
        private static readonly Options Options = new Options();

        public static void Main(string[] args)
        {
            if (!Parser.Default.ParseArguments(args, Options))
            {
                return;
            }

            var levelSwitch = new LoggingLevelSwitch
            {
                MinimumLevel = Options.LogLevel
            };

            Log.Logger = new LoggerConfiguration().MinimumLevel.ControlledBy(levelSwitch).Enrich.FromLogContext()
                .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{Properties}] {Message:lj}{NewLine}{Exception}")
                .WriteTo.File("log.txt", rollingInterval: RollingInterval.Day, rollOnFileSizeLimit: true)
                .CreateLogger();

            //Level Options
            ValidateAndCorrectOptionalOptions();

            Log.Information("Using {Options}", Options);

            _version = VersionCode.V1;

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
                    //var timer = new Timer(CaptureUPSData, null, 1000, 1000);
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

        private static void ValidateAndCorrectOptionalOptions()
        {
            if (string.IsNullOrWhiteSpace(Options.NewAddress))
            {
                Options.NewAddress = Options.Address;
            }

            //Parse IP Address fields
            Options.IPAddress = ParseIPAddress(Options.Address);
            Options.NewIPAddress = ParseIPAddress(Options.NewAddress);
        }

        private static IPAddress ParseIPAddress(string ipAddress)
        {
            var parsed = IPAddress.TryParse(ipAddress, out var _ip);
            if (!parsed)
            {
                var addresses = Dns.GetHostAddressesAsync(ipAddress);
                addresses.Wait();
                foreach (var address in
                    addresses.Result.Where(address => address.AddressFamily == AddressFamily.InterNetwork))
                {
                    _ip = address;
                    break;
                }
            }

            if (_ip == null)
            {
                throw new ArgumentException("invalid ip address", nameof(ipAddress));
            }

            return _ip;
        }

        private static void CaptureUPSData(object state)
        {
            var receiver = new IPEndPoint(Options.IPAddress, 161);
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
                                upsData.BatteryChargeRemainingInPercentage = Convert.ToInt32(item.Data.ToString());
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
                                    Log.Verbose("Alarm data {@Id}, {@Data}", variable.Id.ToString(), variable.Data.ToString());
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
    }
}
