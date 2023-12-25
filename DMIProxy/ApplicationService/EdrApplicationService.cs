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
            if (!_requestCache.GetForcastDTO(out var forcastDto))
            {
                forcastDto = await _service.GetForcast();
                _requestCache.SaveForcastDTO(forcastDto);
            }
            return forcastDto;
        }
    }
}
