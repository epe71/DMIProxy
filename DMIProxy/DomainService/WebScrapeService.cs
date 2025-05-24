using DMIProxy.BusinessEntity;
using System.Text.Json;

namespace DMIProxy.DomainService
{
    public class WebScrapeService : IWebScrapeService
    {
        private string baseUrl = "https://www.dmi.dk/dmidk_byvejrWS/rest/json/id/";
        private readonly ILogger<MetObsService> _logger;
        private IDateTimeProvider _dateTimeProvider;

        private readonly HttpClient _httpClient;

        public WebScrapeService(
            IHttpClientFactory httpClientFactory,
            ILogger<MetObsService> logger,
            IDateTimeProvider dateTimeProvider)
        {
            _httpClient = httpClientFactory.CreateClient("LongTimeOutClient");
            _logger = logger;
            _dateTimeProvider = dateTimeProvider;
        }

        public async Task<TextForecast> GetWeatherForecast(string stationId)
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
                _logger.LogError(ex, $"Error getting weather forecast for station id {stationId}");
                return new TextForecast();
            }
        }

        private TextForecast ParseJson(JsonElement regionalForecast)
        {
            var date = regionalForecast.GetProperty("date").GetString();
            var valid = regionalForecast.GetProperty("valid").GetString() ?? string.Empty;
            var headline = regionalForecast.GetProperty("headline").GetString() ?? string.Empty;
            var forecast = regionalForecast.GetProperty("weatherForecast").GetString() ?? string.Empty;

            return new TextForecast()
            {
                TimeStamp = _dateTimeProvider.Now,
                Valid = valid,
                Headline = headline,
                Forecast = forecast
            };
        }
    }
}
