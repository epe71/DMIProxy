using DMIProxy.Contract;
using DMIProxy.DomainService;

namespace DMIProxy.ApplicationService
{
    public class EdrApplicationService(
        IEdrService edrService,
        INtfyService ntfyService,
        IRequestCache requestCache,
        ILogger<EdrApplicationService> logger) : IEdrApplicationService
    {

        public async Task<HomeAssistantDTO> GetEdrForecast(string forecastParameter)
        {
            // Try to get from cache first
            if (requestCache.GetEdrForecastDTO(forecastParameter, out HomeAssistantDTO? cachedForecast))
            {
                return cachedForecast ?? throw new InvalidOperationException($"Forecast data could not be retrieved: {forecastParameter}");
            }

            // Determine if this key should be updated
            var keysToUpdate = requestCache.GetEdrKeysToUpdate(forecastParameter);
            if (keysToUpdate.Count == 0)
            {
                logger.LogInformation("Initialize cache, no update for: {ForecastParameter}", forecastParameter);
                return new HomeAssistantDTO { description = "Starting service, no forecast parameter ready." };
            }

            try
            {
                var forecastDtos = await edrService.GetEdrForecast(keysToUpdate);
                if (forecastDtos == null || forecastDtos.Count == 0)
                {
                    string errorMsg = $"Failed to retrieve forecast data: {string.Join(", ", keysToUpdate)}";
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
                    requestCache.SaveEdrForecastDTO(dto.name, dto);
                }

                return forcastDto ?? throw new InvalidOperationException("Forcast data not saved in cache after update");
            }
            catch (Polly.CircuitBreaker.BrokenCircuitException ex)
            {
                await ntfyService.SendNotification($"Polly Circuit Breaker error: {ex.Message}");
                logger.LogError("Polly Circuit Breaker error: {Message}", ex.Message);
                return new HomeAssistantDTO { description = ex.Message };
            }
        }
    }
}
