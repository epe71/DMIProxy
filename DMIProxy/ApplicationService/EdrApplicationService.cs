using DMIProxy.Contract;
using DMIProxy.DomainService;
using ZiggyCreatures.Caching.Fusion;

namespace DMIProxy.ApplicationService
{
    /// <summary>
    /// Service for retrieving EDR forecast data and caching it for efficient access. The service uses an underlying
    /// IEdrService to fetch the forecast data and an IFusionCache to store the results, reducing the need for frequent
    /// calls to the external service. The cache is configured with a duration of 4 hours and includes fail-safe options
    /// to ensure that stale data can be served if the external service is temporarily unavailable. Logging is included
    /// to track the retrieval and caching of forecast data.
    /// </summary>
    public class EdrApplicationService(
        IEdrService edrService,
        IFusionCache cache,
        ILogger<EdrApplicationService> logger) : IEdrApplicationService
    {
        public async Task<HomeAssistantDTO> GetEdrForecast(string forecastParameter)
        {
            var rainDto = await cache.GetOrSetAsync<HomeAssistantDTO>(
                $"EDR-{forecastParameter}",
                async (_, _) => await GetEdrForecast_NoCache(forecastParameter),
                new HomeAssistantDTO() { name = forecastParameter, description = "No data ready right now" },
                options => options.SetDuration(TimeSpan.FromHours(4))
                .SetFailSafe(true, TimeSpan.FromHours(6), TimeSpan.FromSeconds(60))
                .SetFactoryTimeouts(TimeSpan.FromSeconds(1))

            );

            return rainDto;
        }

        private async Task<HomeAssistantDTO> GetEdrForecast_NoCache(string forecastParameter)
        { 
            var forecastDtos = await edrService.GetEdrForecast([forecastParameter]);
            if (forecastDtos == null || forecastDtos.Count == 0)
            {
                string errorMsg = $"Failed to retrieve forecast data: {forecastParameter}";
                throw new InvalidOperationException(errorMsg);
            }

            HomeAssistantDTO? forcastDto = null;
            foreach (var dto in forecastDtos)
            {
                if (dto.name == forecastParameter)
                {
                    forcastDto = dto;
                }
                logger.LogInformation("Forecast data for {Parameter}: {Description}", forecastParameter, dto.description);
            }

            return forcastDto ?? throw new InvalidOperationException("Forcast data not saved in cache after update");
        }
    }
}
