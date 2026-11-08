using DMIProxy.Contract;
using DMIProxy.DomainService;
using System.Collections.ObjectModel;
using ZiggyCreatures.Caching.Fusion;

namespace DMIProxy.ApplicationService;

/// <summary>
/// Service for retrieving EDR forecast data and caching it for efficient access. The service uses an underlying
/// IEdrService to fetch the forecast data and an IFusionCache to store the results, reducing the need for frequent
/// calls to the external service. The cache is configured with a duration of 6 hours and includes fail-safe options
/// to ensure that stale data can be served if the external service is temporarily unavailable. Logging is included
/// to track the retrieval and caching of forecast data.
/// </summary>
public class EdrApplicationService(
    IEdrService edrService,
    IFusionCache cache,
    EdrStatusHistory statusHistory,
    ILogger<EdrApplicationService> logger) : IEdrApplicationService
{
    private int cacheDurationHours = 8;

    public async Task<HomeAssistantDTO> GetEdrForecast(string forecastParameter)
    {
        var rainDto = await cache.GetOrSetAsync<HomeAssistantDTO>(
            $"EDR-{forecastParameter}",
            async (_, _) => await GetEdrForecast_NoCache(forecastParameter),
            new HomeAssistantDTO() { Name = forecastParameter, Description = "No data ready right now" },
            options => options.SetDuration(TimeSpan.FromHours(cacheDurationHours))
            .SetFailSafe(true, TimeSpan.FromHours(cacheDurationHours + 4), TimeSpan.FromMinutes(2))
            .SetFactoryTimeouts(TimeSpan.FromSeconds(1))
        );

        return rainDto;
    }

    private async Task<HomeAssistantDTO> GetEdrForecast_NoCache(string forecastParameter)
    {
        Collection<HomeAssistantDTO>? forecastDtos;
        try
        {
            var forecastParameters = BatchForecastParameters(forecastParameter);
            var result = await edrService.GetEdrForecast(forecastParameters);
            await statusHistory.AddAsync((int)result.StatusCode);
            forecastDtos = result.Forecasts;
        }
        catch (HttpRequestException exception)
        {
            await statusHistory.AddAsync(exception.StatusCode is { } statusCode ? (int)statusCode : 0);
            throw;
        }
        catch
        {
            await statusHistory.AddAsync(0);
            throw;
        }

        if (forecastDtos == null || forecastDtos.Count == 0)
        {
            string errorMsg = $"Failed to retrieve forecast data: {forecastParameter}";
            throw new InvalidOperationException(errorMsg);
        }

        HomeAssistantDTO? returnDto = null;
        foreach (var dto in forecastDtos)
        {
            await cache.SetAsync($"EDR-{dto.Name}", dto, options => options.SetDuration(TimeSpan.FromHours(cacheDurationHours)));

            if (dto.Name == forecastParameter)
            {
                returnDto = dto;
            }
            logger.LogInformation("Forecast data for {Parameter}: {Description}", forecastParameter, dto.Description);
        }

        return returnDto ?? throw new InvalidOperationException("Forcast data not saved in cache after update");
    }

    private static Collection<string> BatchForecastParameters(string forecastParameter)
    {
        return forecastParameter switch
        {
            "temperature-2m" => ["temperature-2m", "relative-humidity-2m"],
            "relative-humidity-2m" => ["relative-humidity-2m", "temperature-2m"],
            "wind-speed" => ["wind-speed", "wind-dir"],
            "wind-dir" => ["wind-dir", "wind-speed"],
            "fraction-of-cloud-cover" => ["fraction-of-cloud-cover", "cloud-transmittance", "global-radiation-flux"],
            "cloud-transmittance" => ["cloud-transmittance", "fraction-of-cloud-cover", "global-radiation-flux"],
            "global-radiation-flux" => ["global-radiation-flux", "fraction-of-cloud-cover", "cloud-transmittance"],
            "pressure-sealevel" => ["pressure-sealevel", "total-precipitation"],
            "total-precipitation" => ["total-precipitation", "pressure-sealevel"],
            _ => [forecastParameter],
        };
    }
}
