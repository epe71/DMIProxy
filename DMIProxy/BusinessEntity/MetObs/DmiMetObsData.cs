using System.Collections.ObjectModel;
using System.Text.Json.Serialization;

namespace DMIProxy.BusinessEntity.MetObs;

public class DmiMetObsData
{
    [JsonPropertyName("type")]
    public string Type { get; init; }

    [JsonPropertyName("features")]
    public Collection<Feature> Features { get; init; }

    [JsonPropertyName("timeStamp")]
    public DateTime TimeStamp { get; init; }

    [JsonPropertyName("numberReturned")]
    public int NumberReturned { get; init; }

    [JsonPropertyName("links")]
    public Collection<Link> Links { get; init; }

    public double Rain1h()
    {
        var thisHour = Features.FirstOrDefault(f => f.ThisHour());
        if (thisHour == null)
        {
            return 0.0;
        }
        return thisHour.Rain1h();
    }

    public double RainToday()
    {
        var rainToday = Features.Where(f => f.ThisDay()).Select(f => f.Rain1h()).Sum();
        return rainToday;
    }

    public double RainThisMonth()
    {
        var rainThisMonth = Features.Where(f => f.ThisMonth()).Select(f => f.Rain1h()).Sum();
        return Math.Round(rainThisMonth, 2);
    }

    public bool AllRecived()
    {
        if (Features.Count == NumberReturned)
        {
            return true;
        }
        return false;
    }
}
