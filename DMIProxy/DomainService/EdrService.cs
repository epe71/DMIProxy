using DMIProxy.Contract;
using System.Net;
using System.Text.Json;

namespace DMIProxy.DomainService
{
    /// <summary>
    /// Service for retrieving EDR forecast data from DMI's Open Data API.
    /// </summary>
    public class EdrService : IEdrService
    {
        /// <summary>
        /// DMI documentation for EDR API: [Release notes](https://www.dmi.dk/friedata/dokumentation/release-notes)
        /// [Parameter
        /// list](https://www.dmi.dk/friedata/dokumentation/data/weather-model-harmonie-edr-api-parameter-list) [Status
        /// page](https://statuspage.freshping.io/25721-DMIOpenDatas)
        /// </summary>

        private string baseUrl = "https://opendataapi.dmi.dk/v1/forecastedr/collections/harmonie_dini_sf/position";
        private readonly ILogger<EdrService> _logger;
        private readonly JsonSerializerOptions _serializerOptions;
        private readonly HttpClient _httpClient;

        public EdrService(
            IHttpClientFactory httpClientFactory,
            ILogger<EdrService> logger)
        {
            _httpClient = httpClientFactory.CreateClient("LongTimeOutClient");
            _logger = logger;
            _serializerOptions = new()
            {
                PropertyNameCaseInsensitive = true,
            };
        }

        public async Task<List<HomeAssistantDTO>> GetEdrForecast(List<string> forecastParameters)
        {
            var query = BuildQuery(forecastParameters);
            var requestUri = $"{baseUrl}?{query}";

            using var response = await _httpClient.GetAsync(requestUri);
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync();
            var dmiResult = await JsonSerializer.DeserializeAsync<JsonElement>(stream, _serializerOptions);

            var allForcasts = new List<HomeAssistantDTO>();
            foreach (var parameter in forecastParameters)
            {
                var forcast = ExtractForecastData(parameter, dmiResult);
                allForcasts.Add(forcast);
            }
            return allForcasts;
        }

        private static string BuildQuery(List<string> forecastParameters)
        {
            var weatherParameters = string.Join(",", forecastParameters);
            var parameters = new Dictionary<string, string> {
                { "coords", "POINT(10.137 56.173)" },
                { "csr", "csr84" },
                { "parameter-name", weatherParameters }
            };

            return string.Join("&", parameters.Select(p =>
                $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value)}"));
        }

        private HomeAssistantDTO ExtractForecastData(string forecastParameter, JsonElement jsonElement)
        {
            var time = jsonElement.GetProperty("domain").GetProperty("axes").GetProperty("t").GetProperty("values").EnumerateArray().Select(x => x.GetDateTime()).ToArray();
            var description = jsonElement.GetProperty("parameters").GetProperty(forecastParameter).GetProperty("description").GetProperty("en").GetString();
            var values = jsonElement.GetProperty("ranges").GetProperty(forecastParameter).GetProperty("values").EnumerateArray().Select(x => x.GetDouble()).ToList();

            var adjustedValues = AdjustData(forecastParameter, values);
            var transmittance = DataToPointDTOList(time, adjustedValues);

            var forecastDto = new HomeAssistantDTO()
            {
                name = forecastParameter,
                data = transmittance,
                description = description ?? forecastParameter
            };

            return forecastDto;
        }

        private List<double> AdjustData(string forecastParameter, List<double> values)
        {
            switch (forecastParameter)
            {
                case "temperature-2m":          return new AdjustList(values).Subtract(273.15f).Round(1).Run();
                case "relative-humidity-2m":    return new AdjustList(values).Round(2).Run();
                case "wind-speed":              return new AdjustList(values).Round(1).Run();
                case "pressure-sealevel":       return new AdjustList(values).Divide(100).Round(1).Run();
                case "wind-dir":                return new AdjustList(values).Round(0).Run();
                case "fraction-of-cloud-cover": return new AdjustList(values).Multiply(100).Round(2).Run();
                case "cloud-transmittance":     return new AdjustList(values).Multiply(100).Round(2).Run();
                case "total-precipitation":     return new AdjustList(values).Difference().Round(1).Run();
                case "global-radiation-flux":   return new AdjustList(values).Difference().Divide(1000).Round(1).Run();
                default: return values;
            }
        }

        private List<PointDTO> DataToPointDTOList(DateTime[] time, List<double> cloudTransmit)
        {
            var transmittance = new List<PointDTO>();
            for (int i = 0; i < time.Length; i++)
            {
                var point = new PointDTO()
                {
                    date = time[i].ToString("yyyy-MM-ddTHH:mm:ss"),
                    value = cloudTransmit[i]
                };
                transmittance.Add(point);
            }

            return transmittance;
        }

        private static async Task<string> ParamsToStringAsync(Dictionary<string, string> urlParams)
        {
            using (HttpContent content = new FormUrlEncodedContent(urlParams))
                return await content.ReadAsStringAsync();
        }
    }
}
