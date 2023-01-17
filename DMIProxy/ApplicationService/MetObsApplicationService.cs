using DMIProxy.BusinessEntity;
using DMIProxy.Contract;
using DMIProxy.DomainService;

namespace DMIProxy.ApplicationService
{
    public class MetObsApplicationService : IMetObsApplicationService
    {
        private IMetObsService _service;

        public MetObsApplicationService(IMetObsService metObsService) 
        {
            _service = metObsService;
        }

        public async Task<RainDTO> GetRain(string stationId)
        {
            DmiResult result = await _service.GetRain(stationId);
            var rainDto = new RainDTO();
            rainDto.Rain1h = result.Rain1h();
            rainDto.RainToday = result.RainToday();
            rainDto.RainThisMonth = result.RainThisMonth();
            return rainDto;
        }
    }
}
