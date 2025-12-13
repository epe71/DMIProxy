using DMIProxy.BusinessEntity.MetObs;
using DMIProxy.Contract;
using DMIProxy.DomainService;

namespace DMIProxy.ApplicationService
{
    public class MetObsApplicationService(
        IMetObsService metObsService, 
        IRequestCache requestCache) : IMetObsApplicationService
    {
        public async Task<RainDTO> GetRain(string stationId)
        {
            if (!requestCache.GetRainDTO(stationId, out RainDTO? rainDto))
            {
                DmiMetObsData result = await metObsService.GetRain(stationId);
                if (result == null)
                {
                    // Handle the case where result is null
                    throw new InvalidOperationException("Failed to retrieve rain data.");
                }   
                rainDto = new RainDTO()
                {
                    Rain1h = result.Rain1h(),
                    RainToday = result.RainToday(),
                    RainThisMonth = result.RainThisMonth(),
                    TimeStamp = result.timeStamp,
                    NumberReturned = result.numberReturned
                };
                requestCache.SaveRainDTO(stationId, rainDto);
            }

            return rainDto ?? throw new InvalidOperationException("Rain data could not be retrieved.");
        }
    }
}
