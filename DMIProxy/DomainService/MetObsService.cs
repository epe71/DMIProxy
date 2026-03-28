using DMIProxy.BusinessEntity.MetObs;
using System.Net;
using System.Text.Json;

namespace DMIProxy.DomainService
{
    public class MetObsService : IMetObsService
    {
        private string baseUrl = "https://opendataapi.dmi.dk/v2/metObs/collections/observation/items";
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
            var query = BuildQuery(stationId);
            var requestUri = $"{baseUrl}?{query}";

            using var response = await _httpClient.GetAsync(requestUri);
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync();
            var result = await DeserializeResponseAsync(stream, stationId);

            _logger.LogDebug("DMI MetObs data received for {StationId}", stationId);

            return result;
        }

        private static string BuildQuery(string stationId)
        {
            var parameters = new Dictionary<string, string>
            {
                ["stationId"] = stationId,
                ["period"] = "latest-month",
                ["parameterId"] = "precip_past1h"
            };

            return string.Join("&", parameters.Select(p =>
                $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value)}"));
        }

        private async Task<DmiMetObsData> DeserializeResponseAsync(Stream stream, string stationId)
        {
            var result = await JsonSerializer.DeserializeAsync<DmiMetObsData>(stream, _serializerOptions);
            if (result == null)
            {
                _logger.LogError("No response from DMI-MetObs for {StationId}", stationId);
                throw new SystemException("No response from DMI-MetObs");
            }

            return result;
        }
    }
}
