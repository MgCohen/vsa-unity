using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace VSA.TestSuite.Spike.Features.Objects
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum ObjectState
    {
        Missing,
        Inactive,
        Active,
    }
}
