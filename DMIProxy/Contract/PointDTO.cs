using System.Text.Json.Serialization;

namespace DMIProxy.Contract
{
    public class PointDTO
    {
        [JsonPropertyName("date")]
        public string Date { get; init; }
        [JsonPropertyName("value")]
        public double Value { get; init; }
    }
}
