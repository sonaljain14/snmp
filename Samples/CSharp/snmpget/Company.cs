using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace SnmpGet
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum Company
    {
        Astrodyne = 0,
        Triplite = 1
    }
}
