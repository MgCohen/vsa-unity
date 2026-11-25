using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace VSA.TestSuite.Features.Objects
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum ObjectState
    {
        Missing,
        Inactive,
        Active,
    }
}
