using DMIProxy.Contract;

namespace DMIProxy.ApplicationService
{
    public interface IWeatherForcastService
    {
        public Task<ForcastMessageDTO> GetWeatherForcast(string stationId);
    }
}
