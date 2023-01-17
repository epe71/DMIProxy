using DMIProxy.Contract;

namespace DMIProxy.ApplicationService
{
    public interface IMetObsApplicationService
    {
        public Task<RainDTO> GetRain(string stationId);
    }
}
