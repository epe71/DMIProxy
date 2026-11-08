using System.Text.Json.Serialization;

namespace DMIProxy.BusinessEntity.MetObs
{
    public class Feature
    {
        [JsonPropertyName("id")]
        public Guid Id { get; set; }
        [JsonPropertyName("type")]
        public string Type { get; set; }
        [JsonPropertyName("geometry")]
        public Geometry Geometry { get; set; }
        [JsonPropertyName("properties")]
        public Properties Properties { get; set; }

        public bool ThisHour()
        {
            return Properties.ThisHour();
        }

        public bool ThisDay()
        {
            return Properties.ThisDay();
        }

        public bool ThisMonth()
        {
            return Properties.ThisMonth();
        }

        public double Rain1h()
        {
            return Properties.Rain1h();
        }
    }
}
