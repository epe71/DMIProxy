using DMIProxy.BusinessEntity;
using System.Net;
using System.Text.Json;

namespace DMIProxy.DomainService
{
    public class MetObsService : IMetObsService
    {
        private string baseUrl = "https://dmigw.govcloud.dk/v2/metObs/collections/observation/items";
        private readonly ILogger<MetObsService> _logger;

        private readonly JsonSerializerOptions _serializerOptions = new()
        {
            PropertyNameCaseInsensitive = true,
        };

        private readonly HttpClient _httpClient;

        public MetObsService(
            IHttpClientFactory httpClientFactory, 
            ILogger<MetObsService> logger)
        {
            _httpClient = httpClientFactory.CreateClient();
            _logger = logger;
        }

        public async Task<DmiResult> GetRain(string stationId)
        {
            var apiKey = Environment.GetEnvironmentVariable("DMI_API_KEY");
            if (apiKey == null)
            {
                _logger.LogError("No DMI_API_KEY set");
                throw new ArgumentNullException(nameof(apiKey));
            }

            var parameters = new Dictionary<string, string> { 
                { "stationId", stationId }, 
                { "period", "latest-month" }, 
                { "parameterId", "precip_past1h" } 
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

            await using var contentStream = await(
                await _httpClient.SendAsync(httpRequestMessage))
            .EnsureSuccessStatusCode().Content.ReadAsStreamAsync();

            DmiResult? dmiResult = await JsonSerializer.DeserializeAsync<DmiResult>(contentStream, _serializerOptions);
            if (dmiResult == null)
            {
                _logger.LogError("No response from DMI");
                throw new SystemException("No response from DMI");
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
