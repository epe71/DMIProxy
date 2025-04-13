using DMIProxy.BusinessEntity;
using System.Text.Json;

namespace DMIProxy.DomainService
{
    public class WebScrapeService : IWebScrapeService
    {
        private string baseUrl = "https://www.dmi.dk/dmidk_byvejrWS/rest/json/id/";
        private readonly ILogger<MetObsService> _logger;

        private readonly HttpClient _httpClient;

        public WebScrapeService(
            IHttpClientFactory httpClientFactory,
            ILogger<MetObsService> logger)
        {
            _httpClient = httpClientFactory.CreateClient("LongTimeOutClient");
            _logger = logger;
        }

        public async Task<TextForcast> GetWeatherForcast(string stationId)
        {
            try
            {
                var response = await _httpClient.GetAsync(baseUrl + stationId);
                response.EnsureSuccessStatusCode();
                var jsonResponse = await response.Content.ReadAsStringAsync();
                using (JsonDocument doc = JsonDocument.Parse(jsonResponse))
                {
                    var regionalForecast = doc.RootElement.GetProperty("regionalForecast");
                    return ParseJson(regionalForecast);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting weather forcast for station id {stationId}");
                return new TextForcast();
            }
        }

        private TextForcast ParseJson(JsonElement regionalForecast)
        {
            var date = regionalForecast.GetProperty("date").GetString();
            var valid = regionalForecast.GetProperty("valid").GetString() ?? string.Empty;
            var headline = regionalForecast.GetProperty("headline").GetString() ?? string.Empty;
            var forecast = regionalForecast.GetProperty("weatherForecast").GetString() ?? string.Empty;

            return new TextForcast()
            {
                TimeStamp = DateTime.Now,
                Valid = valid,
                Headline = headline,
                Forcast = forecast
            };
        }
    }
}
