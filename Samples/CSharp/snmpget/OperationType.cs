using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace snmpget
{
    [JsonConverter(typeof(StringEnumConverter))]
    internal enum OperationType
    {
        Information,
        Stats,
        Write
    }
}
