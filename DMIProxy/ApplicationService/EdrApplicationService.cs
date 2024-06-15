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

        public async Task<ForcastDTO> GetForcast()
        {
            if (!_requestCache.GetForcastDTO(out ForcastDTO? forcastDto))
            {
                forcastDto = await _service.GetForcast();
                if (forcastDto == null)
                {
                    throw new InvalidOperationException("Failed to retrive forcast data.");
                }
                _requestCache.SaveForcastDTO(forcastDto);
            }
            return forcastDto ?? throw new InvalidOperationException("Forcast data could not be retrieved.");
        }

        public async Task<HomeAssistantDTO> GetCloudForcast()
        {
            if (!_requestCache.GetCloudForcastDTO(out HomeAssistantDTO? forcastDto))
            {
                forcastDto = await _service.GetCloudForcast();
                if (forcastDto == null)
                {
                    throw new InvalidOperationException("Failed to retrive forcast data.");
                }
                _requestCache.SaveCloudForcastDTO(forcastDto);
            }
            return forcastDto ?? throw new InvalidOperationException("Forcast data could not be retrieved.");
        }

    }
}
