namespace DMIProxy.BusinessEntity.MetObs
{
    public class DmiMetObsData
    {
        public string type { get; init; }
        public List<Feature> features { get; init; }
        public DateTime timeStamp { get; init; }
        public int numberReturned { get; init; }
        public List<Link> links { get; init; }

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

        public bool AllRecived()
        {
            if (features.Count() == numberReturned)
            {
                return true;
            }
            return false;
        }
    }
}
