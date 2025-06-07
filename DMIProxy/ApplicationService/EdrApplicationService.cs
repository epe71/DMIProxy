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
            string keysToUpdate = _requestCache.GetEdrKeysToUpdate(forecastParameter);
            if (string.IsNullOrEmpty(keysToUpdate))
            {
                _logger.LogInformation("Initialize cache, no update for: {ForecastParameter}", forecastParameter);
                return new HomeAssistantDTO { description = "Starting service, no forecast parameter ready." };
            }

            try
            {
                var forecastDto = await _service.GetEdrForecast(forecastParameter);
                if (forecastDto == null)
                {
                    string errorMsg = $"Failed to retrieve forecast data: {forecastParameter}";
                    await _ntfyService.SendNotification(errorMsg);
                    throw new InvalidOperationException(errorMsg);
                }

                await _ntfyService.SendNotification($"Forecast data retrieved successfully for: {forecastParameter}");
                _requestCache.SaveEdrForecastDTO(forecastParameter, forecastDto);
                return forecastDto;
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
