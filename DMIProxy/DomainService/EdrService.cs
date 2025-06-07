using DMIProxy.Contract;
using System.Net;
using System.Text.Json;

namespace DMIProxy.DomainService
{
    public class EdrService : IEdrService
    {
        // parameter list: https://confluence.govcloud.dk/pages/viewpage.action?pageId=110690581
        // Status page: https://statuspage.freshping.io/25721-DMIOpenDatas

        private string baseUrl = "https://dmigw.govcloud.dk/v1/forecastedr/collections/harmonie_dini_sf/position";
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

        public async Task<HomeAssistantDTO> GetEdrForecast(string forecastParameter)
        {
            var httpRequestMessage = await SetupRequestMessage(forecastParameter);

            var response = await _httpClient.SendAsync(httpRequestMessage);
            response.EnsureSuccessStatusCode();
            await using var contentStream = await response.Content.ReadAsStreamAsync();

            var dmiResult = await JsonSerializer.DeserializeAsync<JsonElement>(contentStream, _serializerOptions);

            return ExtractForecastData(forecastParameter, dmiResult);
        }


        private async Task<HttpRequestMessage> SetupRequestMessage(string weatherParameters)
        {
            var apiKey = Environment.GetEnvironmentVariable("DMI_EDR_API_KEY");
            if (apiKey == null)
            {
                _logger.LogError("No DMI_EDR_API_KEY set");
                throw new ArgumentNullException(nameof(apiKey));
            }

            var parameters = new Dictionary<string, string> {
                { "coords", "POINT(10.137 56.173)" },
                { "csr", "csr84" },
                { "parameter-name", weatherParameters }
            };
            var encodedContent = new FormUrlEncodedContent(parameters);
            string query = await ParamsToStringAsync(parameters);

            var httpRequestMessage = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri(baseUrl + "?" + query),
                Headers = {
                    { HttpRequestHeader.Accept.ToString(), "application/json" },
                    { "X-Gravitee-Api-Key", apiKey }
                },
                Content = encodedContent
            };
            return httpRequestMessage;
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
