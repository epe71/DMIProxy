using System.Collections.ObjectModel;

namespace DMIProxy.Contract;

public class ForecastDTO
{
    public DateTime StartTime { get; init; }
    public Collection<double> Temperatur2m { get; init; }
    public Collection<double> RelativeHumidity { get; init; }
    public Collection<double> WindSpeed { get; init; }
    public Collection<double> PressureSeaLevel { get; init; }
    public Collection<double> WindDir { get; init; }
    public Collection<double> CloudCover { get; init; }
}
