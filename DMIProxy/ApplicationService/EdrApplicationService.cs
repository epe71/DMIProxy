using DMIProxy.Contract;
using DMIProxy.DomainService;

namespace DMIProxy.ApplicationService
{
    public class EdrApplicationService : IEdrApplicationService
    {
        private IEdrService _service;
        private INtfyService _ntfyService;
        private IRequestCache _requestCache;
        private readonly ILogger<EdrApplicationService> _logger;

        public EdrApplicationService(
            IEdrService edrService, 
            INtfyService ntfyService,
            IRequestCache requestCache, 
            ILogger<EdrApplicationService> logger)
        {
            _service = edrService;
            _ntfyService = ntfyService;
            _requestCache = requestCache;
            _logger = logger;
        }

        public async Task<HomeAssistantDTO> GetEdrForecast(string forecastParameter)
        {
            // Try to get from cache first
            if (_requestCache.GetEdrForecastDTO(forecastParameter, out HomeAssistantDTO? cachedForecast))
            {
                return cachedForecast ?? throw new InvalidOperationException($"Forecast data could not be retrieved: {forecastParameter}");
            }

            // Determine if this key should be updated
            var keysToUpdate = _requestCache.GetEdrKeysToUpdate(forecastParameter);
            if (keysToUpdate.Count == 0)
            {
                _logger.LogInformation("Initialize cache, no update for: {ForecastParameter}", forecastParameter);
                return new HomeAssistantDTO { description = "Starting service, no forecast parameter ready." };
            }

            try
            {
                var forecastDtos = await _service.GetEdrForecast(keysToUpdate);
                if (forecastDtos == null || forecastDtos.Count == 0)
                {
                    string errorMsg = $"Failed to retrieve forecast data: {string.Join(", ",keysToUpdate)}";
                    await _ntfyService.SendNotification(errorMsg);
                    throw new InvalidOperationException(errorMsg);
                }

                HomeAssistantDTO? forcastDto = null;
                foreach (var dto in forecastDtos)
                {
                    if ( dto.name == forecastParameter)
                    {
                        forcastDto = dto;
                    }
                    _logger.LogInformation("Forecast data for {Parameter}: {Description}", forecastParameter, dto.description);
                    _requestCache.SaveEdrForecastDTO(dto.name, dto);
                }

                await _ntfyService.SendNotification($"Forecast data retrieved successfully for: {string.Join(", ", keysToUpdate)}");
                return forcastDto ?? throw new InvalidOperationException("Forcast data not saved in cache after update");
            }
            catch (Polly.CircuitBreaker.BrokenCircuitException ex)
            {
                await _ntfyService.SendNotification($"Polly Circuit Breaker error: {ex.Message}");
                _logger.LogError("Polly Circuit Breaker error: {Message}", ex.Message);
                return new HomeAssistantDTO { description = ex.Message };
            }
        }
    }
}
