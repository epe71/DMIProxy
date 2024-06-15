using System.Text.Json.Serialization;

namespace DMIProxy.BusinessEntity.EDR
{
    public class EdrData
    {
        public string type { get; set; }
        public EnglishString title { get; set; }
        public Domain domain { get; set; }
        public Parameters parameters { get; set; }
        public Ranges ranges { get; set; }
    }

    public class Domain
    {
        public string type { get; set; }
        public string domainType { get; set; }
        public Axes axes { get; set; }
        public Referencing[] referencing { get; set; }
    }

    public class Axes
    {
        public TimeCoordinates t { get; set; }
        public GeoCoordinates x { get; set; }
        public GeoCoordinates y { get; set; }
    }

    public class TimeCoordinates
    {
        public DateTime[] values { get; set; }
    }

    public class GeoCoordinates
    {
        public float[] values { get; set; }
        public float[] bounds { get; set; }
    }

    public class Referencing
    {
        public string[] coordinates { get; set; }
        public System system { get; set; }
    }

    public class System
    {
        public string type { get; set; }
        public string calendar { get; set; }
        public string id { get; set; }
    }

    public class Parameters
    {
        [JsonPropertyName("temperature-2m")]
        public ParameterData temperature2m { get; set; }

        [JsonPropertyName("relative-humidity")]
        public ParameterData relativehumidity { get; set; }

        [JsonPropertyName("wind-speed")]
        public ParameterData windspeed { get; set; }

        [JsonPropertyName("pressure-sealevel")]
        public ParameterData pressuresealevel { get; set; }

        [JsonPropertyName("wind-dir")]
        public ParameterData winddir { get; set; }

        [JsonPropertyName("cloudcover")]
        public ParameterData cloudcover { get; set; }

        [JsonPropertyName("cloud-transmittance")]
        public ParameterData cloudtransmittance { get; set; }
        
    }

    public class ParameterData
    {
        public string type { get; set; }
        public EnglishString description { get; set; }
        public Observedproperty observedProperty { get; set; }
    }

    public class EnglishString
    {
        public string en { get; set; }
    }

    public class Observedproperty
    {
        [JsonPropertyName("label")]
        public EnglishString url { get; set; }
    }

    public class Ranges
    {
        [JsonPropertyName("temperature-2m")]
        public RangeData temperature2m { get; set; }

        [JsonPropertyName("relative-humidity-2m")]
        public RangeData relativehumidity { get; set; }

        [JsonPropertyName("wind-speed")]
        public RangeData windspeed { get; set; }

        [JsonPropertyName("pressure-sealevel")]
        public RangeData pressuresealevel { get; set; }

        [JsonPropertyName("wind-dir")]
        public RangeData winddir { get; set; }

        [JsonPropertyName("fraction-of-cloud-cover")]
        public RangeData cloudcover { get; set; }

        [JsonPropertyName("cloud-transmittance")]
        public RangeData cloudTransmit { get; set; }
    }

    public class RangeData
    {
        public string type { get; set; }
        public string dataType { get; set; }
        public string[] axisNames { get; set; }
        public int[] shape { get; set; }
        public float[] values { get; set; }
    }
}
