namespace DMIProxy.Contract
{
    public class ForcastDTO
    {
        public DateTime StartTime { get; set; }
        public float[] Temperatur2m { get; set; }
        public float[] RelativeHumidity { get; set; }
        public float[] WindSpeed { get; set; }
        public float[] PressureSeaLevel { get; set; }
        public float[] WindDir { get; set; }
        public float[] CloudCover { get; set; }
    }
}
