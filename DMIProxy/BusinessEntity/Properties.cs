namespace DMIProxy.BusinessEntity
{
    public class Properties
    {
        public DateTime created { get; set; }
        public DateTime observed { get; set; }
        public string parameterId { get; set; }
        public string stationId { get; set; }
        public double value { get; set; }

        public bool ThisHour()
        {
            var span = DateTime.UtcNow - observed;
            if (span.TotalMinutes < 60)
            {
                return true;
            }
            return false;
        }

        public bool ThisDay()
        {
            if (DateTime.Today <= observed)
            {
                return true;
            }
            return false;
        }

        public bool ThisMonth()
        {
            var monthStart = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1, 0, 0, 0);
            if (monthStart <= observed)
            {
                return true;
            }
            return false;
        }

        public double Rain1h()
        {
            if (parameterId != "precip_past1h")
            {
                throw new InvalidOperationException("Not a rain measurement");
            }
            return value;
        }
    }
}
