using DMIProxy.Contract;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text.Json;

namespace DMIProxy.DomainService;

/// <summary>
/// Service for retrieving EDR forecast data from DMI's Open Data API.
/// </summary>
public class EdrService : IEdrService
{
    /// <summary>
    /// DMI documentation for EDR API: [Release notes](https://www.dmi.dk/friedata/dokumentation/release-notes)
    /// [Parameter
    /// list](https://www.dmi.dk/friedata/dokumentation/data/weather-model-harmonie-edr-api-parameter-list) [Status
    /// page](https://statuspage.freshping.io/25721-DMIOpenDatas)
    /// </summary>

    private readonly string baseUrl = "https://opendataapi.dmi.dk/v1/forecastedr/collections/harmonie_dini_sf/position";
    private readonly JsonSerializerOptions _serializerOptions;
    private readonly HttpClient _httpClient;

    public EdrService(
        IHttpClientFactory httpClientFactory)
    {
        ArgumentNullException.ThrowIfNull(httpClientFactory);

        _httpClient = httpClientFactory.CreateClient("DmiClient");
        _serializerOptions = new()
        {
            PropertyNameCaseInsensitive = true,
        };
    }

    public async Task<EdrForecastResult> GetEdrForecast(Collection<string> forecastParameters)
    {
        ArgumentNullException.ThrowIfNull(forecastParameters);

        var query = BuildQuery(forecastParameters);
        var requestUri = new Uri($"{baseUrl}?{query}");

        using var response = await _httpClient.GetAsync(requestUri);
        var statusCode = response.StatusCode;
        response.EnsureDmiSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        var dmiResult = await JsonSerializer.DeserializeAsync<JsonElement>(stream, _serializerOptions);

        var allForcasts = new Collection<HomeAssistantDTO>();
        foreach (var parameter in forecastParameters)
        {
            var forcast = ExtractForecastData(parameter, dmiResult);
            allForcasts.Add(forcast);
        }
        return new EdrForecastResult(statusCode, allForcasts);
    }

    private static string BuildQuery(Collection<string> forecastParameters)
    {
        var weatherParameters = string.Join(",", forecastParameters);
        var parameters = new Dictionary<string, string> {
            { "coords", "POINT(10.137 56.173)" },
            { "csr", "csr84" },
            { "parameter-name", weatherParameters }
        };

        return string.Join("&", parameters.Select(p =>
            $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value)}"));
    }

    private static HomeAssistantDTO ExtractForecastData(string forecastParameter, JsonElement jsonElement)
    {
        var time = jsonElement.GetProperty("domain").GetProperty("axes").GetProperty("t").GetProperty("values").EnumerateArray().Select(x => x.GetDateTime()).ToArray();
        var description = jsonElement.GetProperty("parameters").GetProperty(forecastParameter).GetProperty("description").GetProperty("en").GetString();
        var values = (Collection<double>)jsonElement.GetProperty("ranges").GetProperty(forecastParameter).GetProperty("values").EnumerateArray().Select(x => x.GetDouble());

        var adjustedValues = AdjustData(forecastParameter, values);
        var transmittance = DataToPointDTOList(time, adjustedValues);

        var forecastDto = new HomeAssistantDTO()
        {
            Name = forecastParameter,
            Data = transmittance,
            Description = description ?? forecastParameter
        };

        return forecastDto;
    }

    private static Collection<double> AdjustData(string forecastParameter, Collection<double> values)
    {
        return forecastParameter switch
        {
            "temperature-2m" => new AdjustList(values).Subtract(273.15f).Round(1).Run(),
            "relative-humidity-2m" => new AdjustList(values).Round(2).Run(),
            "wind-speed" => new AdjustList(values).Round(1).Run(),
            "pressure-sealevel" => new AdjustList(values).Divide(100).Round(1).Run(),
            "wind-dir" => new AdjustList(values).Round(0).Run(),
            "fraction-of-cloud-cover" => new AdjustList(values).Multiply(100).Round(2).Run(),
            "cloud-transmittance" => new AdjustList(values).Multiply(100).Round(2).Run(),
            "total-precipitation" => new AdjustList(values).Difference().Round(1).Run(),
            "global-radiation-flux" => new AdjustList(values).Difference().Divide(1000).Round(1).Run(),
            _ => values,
        };
    }

    private static Collection<PointDTO> DataToPointDTOList(DateTime[] time, Collection<double> cloudTransmit)
    {
        var transmittance = new Collection<PointDTO>();
        for (int i = 0; i < time.Length; i++)
        {
            var point = new PointDTO()
            {
                Date = time[i].ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture),
                Value = cloudTransmit[i]
            };
            transmittance.Add(point);
        }

        return transmittance;
    }
}
