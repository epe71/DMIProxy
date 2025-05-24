namespace DMIProxy.Contract
{
    public class ForecastDTO
    {
        public DateTime StartTime { get; init; }
        public List<double> Temperatur2m { get; init; }
        public List<double> RelativeHumidity { get; init; }
        public List<double> WindSpeed { get; init; }
        public List<double> PressureSeaLevel { get; init; }
        public List<double> WindDir { get; init; }
        public List<double> CloudCover { get; init; }
    }
}
