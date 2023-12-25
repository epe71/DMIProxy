using DMIProxy.BusinessEntity.MetObs;
using DMIProxy.Contract;
using DMIProxy.DomainService;

namespace DMIProxy.ApplicationService
{
    public class MetObsApplicationService : IMetObsApplicationService
    {
        private IMetObsService _service;
        private IRequestCache _requestCache;

        public MetObsApplicationService(IMetObsService metObsService, IRequestCache requestCache) 
        {
            _service = metObsService;
            _requestCache = requestCache;
        }

        public async Task<RainDTO> GetRain(string stationId)
        {
            if (!_requestCache.GetRainDTO(stationId, out var rainDto))
            {
                DmiMetObsData result = await _service.GetRain(stationId);
                rainDto = new RainDTO()
                {
                    Rain1h = result.Rain1h(),
                    RainToday = result.RainToday(),
                    RainThisMonth = result.RainThisMonth()
                };
                _requestCache.SaveRainDTO(stationId, rainDto);
            }
            return rainDto;
        }
    }
}
