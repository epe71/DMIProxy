using DMIProxy.Contract;
using DMIProxy.DomainService;

namespace DMIProxy.ApplicationService
{
    public class EdrApplicationService : IEdrApplicationService
    {
        private IEdrService _service;
        private IRequestCache _requestCache;
        private readonly ILogger<EdrApplicationService> _logger;

        public EdrApplicationService(IEdrService edrService, IRequestCache requestCache, ILogger<EdrApplicationService> logger)
        {
            _service = edrService;
            _requestCache = requestCache;
            _logger = logger;
        }

        public async Task<HomeAssistantDTO> GetEdrForecast(string forecastParameter)
        {
            if (!_requestCache.GetEdrForecastDTO(forecastParameter, out HomeAssistantDTO? forecastDto))
            {
                try
                {
                    forecastDto = await _service.GetEdrForecast(forecastParameter);
                    if (forecastDto == null)
                    {
                        throw new InvalidOperationException($"Failed to retrive forecast data: {forecastParameter}");
                    }
                    _requestCache.SaveEdrForecastDTO(forecastParameter, forecastDto);
                }
                catch (Polly.CircuitBreaker.BrokenCircuitException ex)
                {
                    _logger.LogError("Polly Circuit Breaker error: {Message}", ex.Message);
                    return new HomeAssistantDTO { description = ex.Message };
                }
            }
            return forecastDto ?? throw new InvalidOperationException($"Forecast data could not be retrieved: {forecastParameter}");
        }
    }
}
