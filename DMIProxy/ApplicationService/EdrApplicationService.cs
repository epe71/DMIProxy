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

        public async Task<HomeAssistantDTO> GetEdrForcast(string forcastParameter)
        {
            if (!_requestCache.GetEdrForcastDTO(forcastParameter, out HomeAssistantDTO? forcastDto))
            {
                try
                {
                    forcastDto = await _service.GetEdrForcast(forcastParameter);
                    if (forcastDto == null)
                    {
                        throw new InvalidOperationException($"Failed to retrive forcast data: {forcastParameter}");
                    }
                    _requestCache.SaveEdrForcastDTO(forcastParameter, forcastDto);
                }
                catch (Polly.CircuitBreaker.BrokenCircuitException ex)
                {
                    _logger.LogError("Polly Circuit Breaker error: {Message}", ex.Message);
                    return new HomeAssistantDTO { description = ex.Message };
                }
            }
            return forcastDto ?? throw new InvalidOperationException($"Forcast data could not be retrieved: {forcastParameter}");
        }
    }
}
