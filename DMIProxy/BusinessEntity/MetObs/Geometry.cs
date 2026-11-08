using System.Collections.ObjectModel;
using System.Text.Json.Serialization;

namespace DMIProxy.BusinessEntity.MetObs;

public class Geometry
{
    [JsonPropertyName("type")]
    public string Type { get; init; }

    [JsonPropertyName("coordinates")]
    public Collection<double> Coordinates { get; init; }
}
