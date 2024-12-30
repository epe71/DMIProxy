using DMIProxy.Contract;
using DMIProxy.DomainService;

namespace DMIProxy.ApplicationService
{
    public class EdrApplicationService : IEdrApplicationService
    {
        private IEdrService _service;
        private IRequestCache _requestCache;

        public EdrApplicationService(IEdrService edrService, IRequestCache requestCache)
        {
            _service = edrService;
            _requestCache = requestCache;
        }

        public async Task<HomeAssistantDTO> GetEdrForcast(string forcastParameter)
        {
            if (!_requestCache.GetEdrForcastDTO(forcastParameter, out HomeAssistantDTO? forcastDto))
            {
                forcastDto = await _service.GetEdrForcast(forcastParameter);
                if (forcastDto == null)
                {
                    throw new InvalidOperationException($"Failed to retrive forcast data: {forcastParameter}");
                }
                _requestCache.SaveEdrForcastDTO(forcastParameter, forcastDto);
            }
            return forcastDto ?? throw new InvalidOperationException($"Forcast data could not be retrieved: {forcastParameter}");
        }
    }
}
