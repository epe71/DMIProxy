using DMIProxy.BusinessEntity.MetObs;
using System.Net;
using System.Text.Json;
using static DMIProxy.DomainService.IClimateDataService;

namespace DMIProxy.DomainService
{
    /// <summary>
    /// Service for retrieving climate data from DMI's Open Data API. This service is responsible for making HTTP
    /// requests to the API, handling responses, and deserializing the data into usable objects. It provides methods to
    /// retrieve specific climate parameters based on parameter IDs and limits. 
    /// </summary>
    public class ClimateDataService : IClimateDataService
    {
        private string baseUrl = "https://opendataapi.dmi.dk/v2/climateData/collections/countryValue/items";
        private readonly ILogger<MetObsService> _logger;

        private readonly JsonSerializerOptions _serializerOptions;

        private readonly HttpClient _httpClient;

        public ClimateDataService(
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

        public async Task<DmiMetObsData> GetParameterId(ParameterId parameterId, int limit)
        {
            var query = BuildQuery(parameterId, limit);
            var requestUri = $"{baseUrl}?{query}";

            using var response = await _httpClient.GetAsync(requestUri);
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync();
            var result = await DeserializeResponseAsync(stream);

            _logger.LogDebug($"DMI ClimateData recived");

            return result;
        }

        private static string BuildQuery(ParameterId parameterId, int limit)
        {
            var parameters = new Dictionary<string, string>
            {
                { "parameterId", parameterId.ToString() },
                { "limit", limit.ToString() },
                { "qcStatus", "manual" },
                { "timeResolution", "day" }
            };

            return string.Join("&", parameters.Select(p =>
                $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value)}"));
        }

        private async Task<DmiMetObsData> DeserializeResponseAsync(Stream stream)
        {
            var result = await JsonSerializer.DeserializeAsync<DmiMetObsData>(stream, _serializerOptions);
            if (result == null)
            {
                _logger.LogError("No response from DMI ClimateData");
                throw new SystemException("No response from DMI-ClimateData");
            }

            return result;
        }
    }
}
