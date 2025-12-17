using DMIProxy.BusinessEntity.MetObs;
using System.Net;
using System.Text.Json;
using static DMIProxy.DomainService.IClimateDataService;

namespace DMIProxy.DomainService
{
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

        public async Task<DmiMetObsData> GetParameterId(ParameterId parameterId)
        {
            var parameters = new Dictionary<string, string> {
                { "parameterId", parameterId.ToString() },
                { "limit", "100" },
                { "qcStatus", "manual" },
                { "timeResolution", "day" }
            };
            var encodedContent = new FormUrlEncodedContent(parameters);
            var query = await ParamsToStringAsync(parameters);

            var httpRequestMessage = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri(baseUrl + "?" + query),
                Headers = {
                    { HttpRequestHeader.Accept.ToString(), "application/json" },
                },
                Content = encodedContent
            };

            var response = await _httpClient.SendAsync(httpRequestMessage);
            response.EnsureSuccessStatusCode();
            await using var contentStream = await response.Content.ReadAsStreamAsync();

            _logger.LogDebug($"DMI ClimateData recived");

            DmiMetObsData? dmiResult = await JsonSerializer.DeserializeAsync<DmiMetObsData>(contentStream, _serializerOptions);
            if (dmiResult == null)
            {
                _logger.LogError("No response from DMI-ClimateData");
                throw new SystemException("No response from DMI-ClimateData");
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
