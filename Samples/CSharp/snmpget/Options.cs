﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using CommandLine;
using CommandLine.Text;
using Newtonsoft.Json;
using Serilog.Events;
using SnmpGet;

namespace snmpget
{
    internal class Options
    {
        /// <summary>
        /// Gets or sets the log level.
        /// </summary>
        /// <value>
        /// The log level.
        /// </value>
        [Option('l', "logLevel", Required = false, HelpText = "Logging level", DefaultValue = LogEventLevel.Information)]
        public LogEventLevel LogLevel { get; set; }

        [Option("company", Required = false, HelpText = "company Type", DefaultValue = Company.Astrodyne)]
        public Company Company { get; set; }

        [Option('t', "timeout", Required = false, HelpText = "Time Out", DefaultValue = 1000)]
        public int TimeOut { get; set; }

        public Options()
        {
            LogLevel = LogEventLevel.Debug;
        }
        [Option('c', "community", Required = false, HelpText = "Community", DefaultValue = "public")]
        public string Community { get; set; }

        [Option('a', "address", Required = true, HelpText = "IP Address")]
        public string Address { get; set; }

        public IPAddress IPAddress { get; set; }

        public IPAddress NewIPAddress { get; set; }

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
            return HelpText.AutoBuild(this, current => HelpText.DefaultParsingErrorsHandler(this, current));
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