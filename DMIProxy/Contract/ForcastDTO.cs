namespace DMIProxy.Contract
{
    public class ForcastDTO
    {
        public DateTime StartTime { get; init; }
        public float[] Temperatur2m { get; init; }
        public float[] RelativeHumidity { get; init; }
        public float[] WindSpeed { get; init; }
        public float[] PressureSeaLevel { get; init; }
        public float[] WindDir { get; init; }
        public float[] CloudCover { get; init; }
    }
}
