namespace snmpget
{
    public class UPSData
    {
        /// <summary>
        /// Property to store UPS Manufacturer.
        /// </summary>
        /// <value>The ups manufacturer.</value>
        public string Manufacturer { get; set; }

        /// <summary>
        /// Property to store UPS Charge Remaining In Minutes.
        /// </summary>
        /// <value>The estimate ups charge remaining in minutes.</value>
        public int BatteryRemainingInMinutes { get; set; }

        /// <summary>
        /// Property to store UPS Battery Percentage Remaining.
        /// </summary>
        /// <value>The ups battery percentage remaining.</value>
        public int BatteryChargeRemainingInPercentage { get; set; }

        /// <summary>
        /// Property to store Ups Battery Status.
        /// </summary>
        /// <value>The ups battery status.</value>
        public BatteryStatus BatteryStatus { get; set; }

        /// <summary>
        /// Property to store UPS Output Source.
        /// </summary>
        /// <value>The ups output source.</value>
        public DeviceMode DeviceMode { get; set; }

        /// <summary>
        /// Gets or sets the seconds on battery.
        /// </summary>
        /// <value>
        /// The seconds on battery.
        /// </value>
        public int SecondsOnBattery { get; set; }

        public int BatteryOutputPower { get; set; }
    }
}
