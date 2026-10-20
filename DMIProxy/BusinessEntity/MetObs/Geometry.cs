using System.Text.Json.Serialization;

namespace DMIProxy.BusinessEntity.MetObs;

public class Geometry
{
    [JsonPropertyName("type")]
    public string Type { get; init; }

    [JsonPropertyName("coordinates")]
    public List<double> Coordinates { get; init; }
}
