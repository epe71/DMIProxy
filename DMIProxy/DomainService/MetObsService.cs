using DMIProxy.BusinessEntity.MetObs;
using System.Net;
using System.Text.Json;

namespace DMIProxy.DomainService
{
    public class MetObsService : IMetObsService
    {
        private string baseUrl = "https://dmigw.govcloud.dk/v2/metObs/collections/observation/items";
        private readonly ILogger<MetObsService> _logger;

        private readonly JsonSerializerOptions _serializerOptions;

        private readonly HttpClient _httpClient;

        public MetObsService(
            IHttpClientFactory httpClientFactory, 
            ILogger<MetObsService> logger)
        {
            _httpClient = httpClientFactory.CreateClient("LongTimeOutClient");
            _logger = logger;
            _serializerOptions = new()
            {
                PropertyNameCaseInsensitive = true,
            };
        }

        public async Task<DmiMetObsData> GetRain(string stationId)
        {
            var apiKey = Environment.GetEnvironmentVariable("DMI_METOBS_API_KEY");
            if (apiKey == null)
            {
                _logger.LogError("No DMI_METOBS_API_KEY set");
                throw new ArgumentNullException(nameof(apiKey));
            }

            var parameters = new Dictionary<string, string> { 
                { "stationId", stationId }, 
                { "period", "latest-month" }, 
                { "parameterId", "precip_past1h" } 
            };
            var encodedContent = new FormUrlEncodedContent(parameters);
            var query = await ParamsToStringAsync(parameters);

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

            var response = await _httpClient.SendAsync(httpRequestMessage);
            response.EnsureSuccessStatusCode();
            await using var contentStream = await response.Content.ReadAsStreamAsync();

            _logger.LogDebug($"DMI MetObs data recived for {stationId}");

            DmiMetObsData? dmiResult = await JsonSerializer.DeserializeAsync<DmiMetObsData>(contentStream, _serializerOptions);
            if (dmiResult == null)
            {
                _logger.LogError("No response from DMI-MetObs");
                throw new SystemException("No response from DMI-MetObs");
            }
            return dmiResult;
        }

        private static async Task<string> ParamsToStringAsync(Dictionary<string, string> urlParams)
        {
            using (HttpContent content = new FormUrlEncodedContent(urlParams))
                return await content.ReadAsStringAsync();
        }
    }
}
