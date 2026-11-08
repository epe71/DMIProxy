using System.Text.Json.Serialization;

namespace DMIProxy.BusinessEntity.MetObs
{
    public class Properties
    {
        [JsonPropertyName("created")]
        public DateTime Created { get; set; }
        [JsonPropertyName("observed")]
        public DateTime Observed { get; set; }
        [JsonPropertyName("parameterId")]
        public string ParameterId { get; set; }
        [JsonPropertyName("stationId")]
        public string StationId { get; set; }
        [JsonPropertyName("value")]
        public double Value { get; set; }
        [JsonPropertyName("noValuesInCalculation")]
        public int NoValuesInCalculation { get; set; }
        [JsonPropertyName("timeResolution")]
        public string TimeResolution { get; set; }
        [JsonPropertyName("qcStatus")]
        public string QcStatus { get; set; }
        [JsonPropertyName("from")]
        public DateTime From { get; set; }
        [JsonPropertyName("to")]
        public DateTime To { get; set; }
        [JsonPropertyName("calculatedAt")]
        public DateTime CalculatedAt { get; set; }
        [JsonPropertyName("validity")]
        public bool Validity { get; set; }


        public bool ThisHour()
        {
            var span = DateTime.UtcNow - Observed;
            if (span.TotalMinutes < 60)
            {
                return true;
            }
            return false;
        }

        public bool ThisDay()
        {
            if (DateTime.Today <= Observed)
            {
                return true;
            }
            return false;
        }

        public bool ThisMonth()
        {
            var monthStart = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1, 0, 0, 0);
            if (monthStart <= Observed)
            {
                return true;
            }
            return false;
        }

        public double Rain1h()
        {
            if (ParameterId != "precip_past1h")
            {
                throw new InvalidOperationException("Not a rain measurement");
            }
            return Value;
        }
    }
}
