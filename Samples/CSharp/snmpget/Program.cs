using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
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
        private static List<Variable> _upsStatVariableList;
        private static List<Variable> _upsInformationVariables;
        private static List<Variable> _upsWriteVariables;
        private static VersionCode _version = VersionCode.V1;

        /// <summary>
        /// Holds the options for the extractor
        /// </summary>
        private static readonly Options Options = new Options();

        public static IPAddress IPAddress { get; set; }

        public static IPAddress NewIPAddress { get; set; }

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

            Log.Logger = new LoggerConfiguration().MinimumLevel.ControlledBy(levelSwitch).Enrich.FromLogContext().WriteTo.Console().WriteTo.File("log.txt", rollingInterval: RollingInterval.Day, rollOnFileSizeLimit: true).CreateLogger();

            Log.Information("Using {Options}", Options);
            //Level Options
            try
            {
                ValidateAndCorrectOptionalOptions();

                _version = VersionCode.V1;

                _upsInformationVariables = new List<Variable>
                {
                    UPS.Information.Variables.Manufacturer,
                    UPS.Information.Variables.AutoRestart,
                    UPS.Information.Variables.Ipv4DHCPEnabled,
                    UPS.Information.Variables.Ipv4IP
                };

                _upsStatVariableList = new List<Variable>
                {
                    UPS.Stat.Variables.BatteryStatus,
                    UPS.Stat.Variables.OutputSource,
                    UPS.Stat.Variables.ChargeRemainingInPercentage,
                    UPS.Stat.Variables.ChargeRemainingInMinutes,
                    UPS.Stat.Variables.SecondsOnBattery
                };

                switch (Options.Company)
                {
                    case Company.Triplite:
                        _upsStatVariableList.Add(UPS.Stat.Variables.BatteryOutputPower_TripLite);
                        break;
                    default:
                        _upsStatVariableList.Add(UPS.Stat.Variables.BatteryOutputPower_Astrodyne);
                        break;
                }

                try
                {
                    switch (Options.Operation)
                    {
                        case OperationType.Information:
                            ShowUPSInformation();
                            break;
                        case OperationType.Stats:
                            ShowUPSInformation();
                            var timer = new Timer(CaptureUPSData, null, 1000, 1000);
                            break;
                        case OperationType.Write:
                            WriteSettingsToUPS();
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    Log.Information("Press enter key to exit the application.");
                    Console.ReadLine();
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error in operation");
                }
            }
            catch (Exception exception)
            {
                Log.Error(exception, "Error in execution");
            }

            Log.CloseAndFlush();
        }

        private static void WriteSettingsToUPS()
        {
            var upsWriteVariables = new List<Variable>
            {
                new Variable(UPS.Command.OID.Ipv4DHCPEnabled, new Integer32(0)),
                new Variable(UPS.Command.OID.AutoRestart, new Integer32(1)),
                new Variable(UPS.Command.OID.Ipv4IP, new IP(Options.NewAddress))
            };
            var receiver = new IPEndPoint(IPAddress, 161);

            foreach (Variable variable in Messenger.Set(_version, receiver, new OctetString(Options.Community), upsWriteVariables, Options.TimeOut))
            {
                Log.Information("Response of set: {variableID} {variableType}", variable.Data.ToString(), variable.Id.ToString());
            }

            IList<Variable> controlConfigVariables = Messenger.Set(_version,
                receiver,
                new OctetString(Options.Community),
                new List<Variable>
                {
                    new Variable(UPS.Command.OID.ControlConfig, new Integer32(1))
                },
                Options.TimeOut);

            foreach (Variable controlConfigVariable in controlConfigVariables)
            {
                Log.Information("Response of set: {variableID} {variableType}", controlConfigVariable.Data.ToString(), controlConfigVariable.Id.ToString());
            }
        }

        private static void ShowUPSInformation()
        {
            var receiver = new IPEndPoint(IPAddress, 161);
            var upsInformation = new UPSInformation();
            foreach (Variable item in Messenger.Get(_version, receiver, new OctetString(Options.Community), _upsInformationVariables, Options.TimeOut))
            {
                if (item.Id == UPS.Information.OID.Manufacturer)
                {
                    upsInformation.Manufacturer = item.Data.ToString();
                }
                else if (item.Id == UPS.Information.OID.Ipv4IP)
                {
                    upsInformation.Ipv4IP = item.Data.ToString();
                }
                else if (item.Id == UPS.Information.OID.Ipv4DHCPEnabled)
                {
                    upsInformation.Ipv4DHCPEnabled = Convert.ToInt32(item.Data.ToString()) > 0;
                }
                else if (item.Id == UPS.Information.OID.AutoRestart)
                {
                    upsInformation.AutoRestart = Convert.ToInt32(item.Data.ToString()) > 0;
                }
            }

            Log.Information("{@UPSInformation}", upsInformation);
        }

        private static void ValidateAndCorrectOptionalOptions()
        {
            if (string.IsNullOrWhiteSpace(Options.NewAddress))
            {
                Options.NewAddress = Options.Address;
            }

            //Parse IP Address fields
            IPAddress = ParseIPAddress(Options.Address);
            NewIPAddress = ParseIPAddress(Options.NewAddress);
        }

        private static IPAddress ParseIPAddress(string ipAddress)
        {
            bool parsed = IPAddress.TryParse(ipAddress, out IPAddress _ip);
            if (!parsed)
            {
                Task<IPAddress[]> addresses = Dns.GetHostAddressesAsync(ipAddress);
                addresses.Wait();
                foreach (IPAddress address in addresses.Result.Where(address => address.AddressFamily == AddressFamily.InterNetwork))
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
            var receiver = new IPEndPoint(IPAddress, 161);
            var upsData = new UPSData();
            try
            {
                switch (Options.Company)
                {
                    case Company.Triplite:
                        foreach (Variable item in Messenger.Get(_version, receiver, new OctetString(Options.Community), _upsStatVariableList, Options.TimeOut))
                        {
                            if (item.Id == UPS.Stat.OID.BatteryStatus)
                            {
                                upsData.BatteryStatus = (BatteryStatus)Convert.ToInt32(item.Data.ToString());
                            }
                            else if (item.Id == UPS.Stat.OID.ChargeRemainingInMinutes)
                            {
                                upsData.BatteryRemainingInMinutes = Convert.ToInt32(item.Data.ToString());
                            }
                            else if (item.Id == UPS.Stat.OID.ChargeRemainingInMinutes)
                            {
                                upsData.BatteryChargeRemainingInPercentage = Convert.ToInt32(item.Data.ToString());
                            }
                            else if (item.Id == UPS.Stat.OID.OutputSource)
                            {
                                upsData.DeviceMode = (DeviceMode)Convert.ToInt32(item.Data.ToString());
                            }
                            else if (item.Id == UPS.Stat.OID.SecondsOnBattery)
                            {
                                upsData.SecondsOnBattery = Convert.ToInt32(item.Data.ToString());
                            }
                            else if (item.Id == UPS.Stat.OID.BatteryOutputPower_TripLite)
                            {
                                upsData.BatteryOutputPower = Convert.ToInt32(item.Data.ToString());
                            }
                        }

                        break;
                    default:
                        //initialize
                        upsData.DeviceMode = DeviceMode.Normal;
                        upsData.BatteryStatus = BatteryStatus.Normal;

                        foreach (Variable item in Messenger.Get(_version, receiver, new OctetString(Options.Community), _upsStatVariableList, Options.TimeOut))
                        {
                            if (item.Id == UPS.Stat.OID.ChargeRemainingInMinutes)
                            {
                                upsData.BatteryRemainingInMinutes = Convert.ToInt32(item.Data.ToString());
                            }
                            else if (item.Id == UPS.Stat.OID.ChargeRemainingInPercentage)
                            {
                                upsData.BatteryChargeRemainingInPercentage = Convert.ToInt32(item.Data.ToString());
                            }
                            else if (item.Id == UPS.Stat.OID.SecondsOnBattery)
                            {
                                upsData.SecondsOnBattery = Convert.ToInt32(item.Data.ToString());
                            }
                            else if (item.Id == UPS.Stat.OID.BatteryOutputPower_Astrodyne)
                            {
                                upsData.BatteryOutputPower = Convert.ToInt32(item.Data.ToString());
                            }
                        }

                        var variables = new List<Variable>
                        {
                            UPS.Alarm.Variables.LowBattery,
                            UPS.Alarm.Variables.OnBattery,
                            UPS.Alarm.Variables.DepletedBattery
                        };
                        int itemCount = Messenger.Walk(_version, receiver, new OctetString(Options.Community), UPS.Alarm.OID.Table, variables, Options.TimeOut, WalkMode.WithinSubtree);

                        if (itemCount == 0)
                        {
                            Log.Verbose("nothing in the alarm table");
                        }
                        else
                        {
                            foreach (Variable variable in variables.Where(variable => variable.Data.TypeCode == SnmpType.ObjectIdentifier))
                            {
                                Log.Verbose("Alarm data {@Id}, {@Data}", variable.Id.ToString(), variable.Data.ToString());
                                if (variable.Data.ToString() == UPS.Alarm.OID.OnBattery.ToString())
                                {
                                    upsData.DeviceMode = DeviceMode.Battery;
                                }
                                else if (variable.Data.ToString() == UPS.Alarm.OID.LowBattery.ToString())
                                {
                                    upsData.BatteryStatus = BatteryStatus.Low;
                                }
                                else if (variable.Data.ToString() == UPS.Alarm.OID.DepletedBattery.ToString())
                                {
                                    upsData.BatteryStatus = BatteryStatus.Depleted;
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
                if (_lastKnownData.BatteryChargeRemainingInPercentage != upsData.BatteryChargeRemainingInPercentage || _lastKnownData.BatteryRemainingInMinutes != upsData.BatteryRemainingInMinutes || _lastKnownData.BatteryStatus != upsData.BatteryStatus ||
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
