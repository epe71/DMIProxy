using DMIProxy.BusinessEntity.EDR;
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

        private readonly JsonSerializerOptions _serializerOptions = new()
        {
            PropertyNameCaseInsensitive = true,
        };

        private readonly HttpClient _httpClient;
        public EdrService(
            IHttpClientFactory httpClientFactory,
            ILogger<EdrService> logger)
        {
            _httpClient = httpClientFactory.CreateClient("LongTimeOutClient");
            _logger = logger;
        }

        public async Task<ForcastDTO> GetForcast()
        {
            var weatherParameters = "temperature-2m,wind-speed,wind-dir,pressure-sealevel,relative-humidity-2m,fraction-of-cloud-cover";
            HttpRequestMessage httpRequestMessage = await SetupRequestMessage(weatherParameters);

            var response = await _httpClient.SendAsync(httpRequestMessage);
            response.EnsureSuccessStatusCode();
            await using var contentStream = await response.Content.ReadAsStreamAsync();

            var dmiResult = await JsonSerializer.DeserializeAsync<EdrData>(contentStream, _serializerOptions);
            if (dmiResult == null)
            {
                _logger.LogError("No response from DMI-EDR");
                throw new SystemException("No response from DMI-EDR");
            }
            return ExtractData(dmiResult);
        }

        public async Task<HomeAssistantDTO> GetCloudForcast()
        {
            var weatherParameters = "cloud-transmittance";
            HttpRequestMessage httpRequestMessage = await SetupRequestMessage(weatherParameters);

            var response = await _httpClient.SendAsync(httpRequestMessage);
            response.EnsureSuccessStatusCode();
            await using var contentStream = await response.Content.ReadAsStreamAsync();

            var dmiResult = await JsonSerializer.DeserializeAsync<EdrData>(contentStream, _serializerOptions);
            if (dmiResult == null)
            {
                _logger.LogError("No response from DMI-EDR");
                throw new SystemException("No response from DMI-EDR");
            }
            return ExtractCloudData(dmiResult);
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

        private ForcastDTO ExtractData(EdrData data)
        {
            var startTime = data.domain.axes.t.values.First();

            var rawWindspeed = ConvertToDoubles(data.ranges.windspeed.values);
            var windspeed = ArrayRound(rawWindspeed, 1);

            var rawWindDir = ConvertToDoubles(data.ranges.winddir.values);
            var windDir = ArrayRound(rawWindDir, 0);

            var rawHumidityPct = ConvertToDoubles(data.ranges.relativehumidity.values);
            var humidityPct = ArrayRound(rawHumidityPct, 2);

            var rawCloudCoverPct = ConvertToDoubles(data.ranges.cloudcover.values);
            var cloudCoverPct = ArrayMultiply(rawCloudCoverPct, 100);
            cloudCoverPct = ArrayRound(cloudCoverPct, 2);

            var rawPresurehPa = ConvertToDoubles(data.ranges.pressuresealevel.values);
            var presurehPa = ArrayDivide(rawPresurehPa, 100);
            presurehPa = ArrayRound(presurehPa, 1);

            var rawTemperatureCelsius = ConvertToDoubles(data.ranges.temperature2m.values);
            var temperaturCelsius = ArraySubtract(rawTemperatureCelsius, 273.15f);
            temperaturCelsius = ArrayRound(temperaturCelsius, 1);

            var forcastDto = new ForcastDTO()
            { 
                StartTime = startTime,
                WindSpeed = windspeed,
                WindDir = windDir,
                CloudCover = cloudCoverPct,
                RelativeHumidity = humidityPct,
                PressureSeaLevel = presurehPa,
                Temperatur2m = temperaturCelsius
            };

            return forcastDto;
        }

        private HomeAssistantDTO ExtractCloudData(EdrData data)
        {
            var time = data.domain.axes.t.values;
            var values = ConvertToDoubles(data.ranges.cloudTransmit.values);

            var cloudTransmit = ArrayMultiply(values, 100);
            var cloudTransmitRounded = ArrayRound(cloudTransmit, 2);
            var transmittance = DataToPointDTOList(time, cloudTransmitRounded);

            var forcastDto = new HomeAssistantDTO()
            {
                data = transmittance,
                description = data.parameters.cloudtransmittance.description.en
            };

            return forcastDto;
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

        private List<double> ConvertToDoubles(float[] data)
        {
            var dataList = data.ToList();
            List<double> converted = dataList.Select(x => (double)x).ToList();
            return converted;
        }

        private List<double> ArrayDivide(List<double> numbers, double fraction)
        {
            var calculatedNumbers = new List<double>();
            foreach (var number in numbers)
            {
                var newValue = number / fraction;
                calculatedNumbers.Add(newValue);
            }
            return calculatedNumbers;
        }

        private List<double> ArrayMultiply(List<double> numbers, double times)
        {
            var calculatedNumbers = new List<double>();
            foreach (var number in numbers)
            {
                var newValue = number * times;
                calculatedNumbers.Add(newValue);
            }
            return calculatedNumbers;
        }

        private List<double> ArraySubtract(List<double> numbers, double subtract)
        {
            var calculatedNumbers = new List<double>();
            foreach (var number in numbers)
            {
                var newValue = number - subtract;
                calculatedNumbers.Add(newValue);
            }
            return calculatedNumbers;
        }

        private List<double> ArrayRound(List<double> numbers, int digits)
        {
            var calculatedNumbers = new List<double>();
            foreach (var number in numbers)
            {
                var newValue = (double)Math.Round(number, digits); 
                calculatedNumbers.Add(newValue);
            }
            return calculatedNumbers;
        }

        private static async Task<string> ParamsToStringAsync(Dictionary<string, string> urlParams)
        {
            using (HttpContent content = new FormUrlEncodedContent(urlParams))
                return await content.ReadAsStringAsync();
        }
    }
}
