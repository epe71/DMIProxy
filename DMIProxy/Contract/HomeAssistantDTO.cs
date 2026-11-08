using System.Collections.ObjectModel;
using System.Text.Json.Serialization;

namespace DMIProxy.Contract;

public class HomeAssistantDTO
{
    [JsonPropertyName("name")]
    public string Name { get; init; }

    [JsonPropertyName("data")]
    public Collection<PointDTO> Data { get; init; }

    [JsonPropertyName("description")]
    public string Description { get; init; }
}
