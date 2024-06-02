using DMIProxy.BusinessEntity.EDR;
using DMIProxy.Contract;
using System.Net;
using System.Text.Json;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace DMIProxy.DomainService
{
    public class EdrService : IEdrService
    {
        // parameter list: https://confluence.govcloud.dk/pages/viewpage.action?pageId=110690581

        private string baseUrl = "https://dmigw.govcloud.dk/v1/forecastedr/collections/harmonie_dini_sf/position";
        private readonly ILogger<EdrService> _logger;

        private readonly JsonSerializerOptions _serializerOptions = new()
        {
            PropertyNameCaseInsensitive = true,
        };

        private readonly HttpClient _httpClient;
        public EdrService(
            IHttpClientFactory httpClientFactory,
            ILogger<EdrService> logger)
        {
            _httpClient = httpClientFactory.CreateClient();
            _logger = logger;
        }

        public async Task<ForcastDTO> GetForcast()
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
                { "parameter-name", "temperature-2m,wind-speed,wind-dir,pressure-sealevel,relative-humidity-2m,fraction-of-cloud-cover,cloud-transmittance" } 
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

            _httpClient.Timeout = TimeSpan.FromSeconds(30);
            await using var contentStream = await (
                await _httpClient.SendAsync(httpRequestMessage))
            .EnsureSuccessStatusCode().Content.ReadAsStreamAsync();

            EdrData? dmiResult = await JsonSerializer.DeserializeAsync<EdrData>(contentStream, _serializerOptions);
            if (dmiResult == null)
            {
                _logger.LogError("No response from DMI-EDR");
                throw new SystemException("No response from DMI-EDR");
            }
            return ExtractData(dmiResult);
        }

        private ForcastDTO ExtractData(EdrData data)
        {
            var startTime = data.domain.axes.t.values.First();
            var windspeed = ArrayRound(data.ranges.windspeed.values, 1);
            var windDir = ArrayRound(data.ranges.winddir.values, 0);
            var humidityPct = ArrayRound(data.ranges.relativehumidity.values, 2);

            var cloudTransmit = ArrayMultiply(data.ranges.cloudTransmit.values, 100);
            cloudTransmit = ArrayRound(cloudTransmit, 2);

            var cloudCoverPct = ArrayMultiply(data.ranges.cloudcover.values, 100);
            cloudCoverPct = ArrayRound(cloudCoverPct, 2);

            var presurehPa = ArrayDivide(data.ranges.pressuresealevel.values, 100);
            presurehPa = ArrayRound(presurehPa, 1);

            var temperaturCelsius = ArraySubtract(data.ranges.temperature2m.values, 273.15f);
            temperaturCelsius = ArrayRound(temperaturCelsius, 1);

            var forcastDto = new ForcastDTO()
            { 
                StartTime = startTime,
                WindSpeed = windspeed,
                WindDir = windDir,
                CloudCover = cloudTransmit,
                RelativeHumidity = humidityPct,
                PressureSeaLevel = presurehPa,
                Temperatur2m = temperaturCelsius
            };

            return forcastDto;
        }

        private float[] ArrayDivide(float[] numbers, float fraction)
        {
            var result = new float[numbers.Length];
            for (int i = 0; i < numbers.Length; i++)
            {
                result[i] = numbers[i]/fraction;
            }
            return result;
        }
        private float[] ArrayMultiply(float[] numbers, float times)
        {
            var result = new float[numbers.Length];
            for (int i = 0; i < numbers.Length; i++)
            {
                result[i] = numbers[i] * times;
            }
            return result;
        }

        private float[] ArraySubtract(float[] numbers, float subtract)
        {
            var result = new float[numbers.Length];
            for (int i = 0; i < numbers.Length; i++)
            {
                result[i] = numbers[i] - subtract;
            }
            return result;
        }

        private float[] ArrayRound(float[] numbers, int digits)
        {
            var result = new float[numbers.Length];
            for (int i = 0; i < numbers.Length; i++)
            {
                result[i] = (float)Math.Round(numbers[i], digits);
            }
            return result;
        }

        private static async Task<string> ParamsToStringAsync(Dictionary<string, string> urlParams)
        {
            using (HttpContent content = new FormUrlEncodedContent(urlParams))
                return await content.ReadAsStringAsync();
        }
    }
}
