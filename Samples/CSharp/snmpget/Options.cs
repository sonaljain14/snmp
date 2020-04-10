using System;
using CommandLine;
using CommandLine.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Serilog.Events;
using SnmpGet;

namespace snmpget
{
    internal class Options
    {
        public Options()
        {
            LogLevel = LogEventLevel.Debug;
        }

        /// <summary>
        /// Gets or sets the log level.
        /// </summary>
        /// <value>
        /// The log level.
        /// </value>
        [Option('l', "logLevel", Required = false, HelpText = "Logging level: " + nameof(LogEventLevel.Information) + "|" + nameof(LogEventLevel.Debug) + "|" + nameof(LogEventLevel.Verbose), DefaultValue = LogEventLevel.Information)]
        [JsonConverter(typeof(StringEnumConverter))]
        public LogEventLevel LogLevel { get; set; }

        [Option("company", Required = false, HelpText = "company Type: " + nameof(Company.Astrodyne) + "|" + nameof(Company.Triplite), DefaultValue = Company.Astrodyne)]
        public Company Company { get; set; }

        [Option('o', "operation", Required = false, HelpText = "What operation to perform: " + nameof(OperationType.Information) + "|" + nameof(OperationType.Stats) + "|" + nameof(OperationType.Write), DefaultValue = OperationType.Information)]
        public OperationType Operation { get; set; }

        [Option('t', "timeout", Required = false, HelpText = "Time Out", DefaultValue = 1000)]
        public int TimeOut { get; set; }

        [Option('c', "community", Required = false, HelpText = "Community", DefaultValue = "public")]
        public string Community { get; set; }

        [Option('a', "address", Required = true, HelpText = "IP Address")]
        public string Address { get; set; }

        [Option('n', "newaddress", Required = false, HelpText = "New IP Address. If nothing is provided then IP Address will be reused.")]
        public string NewAddress { get; set; }

        /// <summary>
        /// Gets the usage.
        /// </summary>
        /// <returns>
        /// Help text
        /// </returns>
        [HelpOption]
        public string GetUsage()
        {
            return HelpText.AutoBuild(this, current =>
            {
                current.AdditionalNewLineAfterOption = false;
                current.AddDashesToOption = true;
                HelpText.DefaultParsingErrorsHandler(this, current);
            });
        }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return Environment.NewLine + JsonConvert.SerializeObject(this,
                Formatting.Indented,
                new JsonSerializerSettings
                {
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                });
        }
    }
}
