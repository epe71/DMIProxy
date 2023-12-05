namespace DMIProxy.BusinessEntity.MetObs
{
    public class Feature
    {
        public Guid id { get; set; }
        public string type { get; set; }
        public Geometry geometry { get; set; }
        public Properties properties { get; set; }

        public bool ThisHour()
        {
            return properties.ThisHour();
        }

        public bool ThisDay()
        {
            return properties.ThisDay();
        }

        public bool ThisMonth()
        {
            return properties.ThisMonth();
        }

        public double Rain1h()
        {
            return properties.Rain1h();
        }
    }
}
