using System.Text.Json.Serialization;

namespace DMIProxy.BusinessEntity.MetObs
{
    public class Link
    {
        [JsonPropertyName("href")]
        public string Href { get; set; }
        [JsonPropertyName("rel")]
        public string Rel { get; set; }
        [JsonPropertyName("type")]
        public string Type { get; set; }
        [JsonPropertyName("title")]
        public string Title { get; set; }
    }
}
