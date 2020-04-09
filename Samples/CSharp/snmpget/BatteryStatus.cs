using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace snmpget
{
    /// <summary>
    /// Enum for battery status
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum BatteryStatus
    {
        /// <summary>
        /// The unknown
        /// </summary>
        Unknown = 1,

        /// <summary>
        /// Battery Normal
        /// </summary>
        Normal,

        /// <summary>
        /// Battery low
        /// </summary>
        Low,

        /// <summary>
        /// Battery dead
        /// </summary>
        Depleted
    }
}
