using DMIProxy.Contract;
using DMIProxy.DomainService;
using ZiggyCreatures.Caching.Fusion;

namespace DMIProxy.ApplicationService
{
    public class EdrApplicationService(
        IEdrService edrService,
        INtfyService ntfyService,
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
                await ntfyService.SendNotification(errorMsg);
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
