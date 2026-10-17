using System.Text.Json.Serialization;

namespace DMIProxy.BusinessEntity.MetObs
{
    public class Geometry
    {
        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("coordinates")]
        public List<double> Coordinates { get; set; }
    }
}
