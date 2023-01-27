namespace DMIProxy.BusinessEntity
{
    public class DmiResult
    {
        public string type { get; set; }
        public List<Feature> features { get; set; }
        public DateTime timeStamp { get; set; }
        public int numberReturned { get; set; }
        public List<Link> links { get; set; }

        public double Rain1h()
        {
            var thisHour = features.FirstOrDefault(f => f.ThisHour());
            if (thisHour == null)
            {
                return 0.0;
            }
            return thisHour.Rain1h();
        }

        public double RainToday()
        {
            var rainToday = features.Where(f => f.ThisDay()).Select(f => f.Rain1h()).Sum();
            return rainToday;
        }

        public double RainThisMonth()
        {
            var rainThisMonth = features.Where(f => f.ThisMonth()).Select(f => f.Rain1h()).Sum();
            return Math.Round(rainThisMonth, 2);
        }

    }
}
