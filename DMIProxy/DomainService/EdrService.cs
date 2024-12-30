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

        public async Task<HomeAssistantDTO> GetEdrForcast(string forcastParameter)
        {
            var httpRequestMessage = await SetupRequestMessage(forcastParameter);

            var response = await _httpClient.SendAsync(httpRequestMessage);
            response.EnsureSuccessStatusCode();
            await using var contentStream = await response.Content.ReadAsStreamAsync();

            var dmiResult = await JsonSerializer.DeserializeAsync<JsonElement>(contentStream, _serializerOptions);

            return ExtractForcastData(forcastParameter, dmiResult);
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

        private HomeAssistantDTO ExtractForcastData(string forcastParameter, JsonElement jsonElement)
        {
            var time = jsonElement.GetProperty("domain").GetProperty("axes").GetProperty("t").GetProperty("values").EnumerateArray().Select(x => x.GetDateTime()).ToArray();
            var description = jsonElement.GetProperty("parameters").GetProperty(forcastParameter).GetProperty("description").GetProperty("en").GetString();
            var values = jsonElement.GetProperty("ranges").GetProperty(forcastParameter).GetProperty("values").EnumerateArray().Select(x => x.GetDouble()).ToList();

            var adjustedValues = AdjustData(forcastParameter, values);
            var transmittance = DataToPointDTOList(time, adjustedValues);

            var forcastDto = new HomeAssistantDTO()
            {
                data = transmittance,
                description = description ?? forcastParameter
            };

            return forcastDto;
        }

        private List<double> AdjustData(string forcastParameter, List<double> values)
        {
            switch (forcastParameter)
            {
                case "temperature-2m":          return ArrayRound(ArraySubtract(values, 273.15f), 1);
                case "relative-humidity-2m":    return ArrayRound(values, 2);
                case "wind-speed":              return ArrayRound(values, 1);
                case "pressure-sealevel":       return ArrayRound(ArrayDivide(values, 100), 1);
                case "wind-dir":                return ArrayRound(values, 0);
                case "fraction-of-cloud-cover": return ArrayRound(ArrayMultiply(values, 100), 2);
                case "cloud-transmittance":     return ArrayRound(ArrayMultiply(values, 100), 2);
                case "total-precipitation":     return ArrayRound(Difference(values), 1);
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

        private List<double> ArrayDivide(List<double> numbers, double fraction)
        {
            return numbers.Select(number => number / fraction).ToList();
        }

        private List<double> ArrayMultiply(List<double> numbers, double times)
        {
            return numbers.Select(number => number * times).ToList();
        }

        private List<double> ArraySubtract(List<double> numbers, double subtract)
        {
            return numbers.Select(number => number - subtract).ToList();
        }

        private List<double> ArrayRound(List<double> numbers, int digits)
        {
            return numbers.Select(number => Math.Round(number, digits)).ToList();
        }
        private List<double> Difference(List<double> numbers)
        {
            var differences = new List<double>();
            differences.Add(0.0);
            for (int i = 0; i < numbers.Count - 1; i++)
            {
                differences.Add(numbers[i + 1] - numbers[i]);
            }
            return differences;
        }

        private static async Task<string> ParamsToStringAsync(Dictionary<string, string> urlParams)
        {
            using (HttpContent content = new FormUrlEncodedContent(urlParams))
                return await content.ReadAsStringAsync();
        }
    }
}
