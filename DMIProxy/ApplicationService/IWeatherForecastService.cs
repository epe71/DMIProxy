using DMIProxy.Contract;

namespace DMIProxy.ApplicationService
{
    public interface IWeatherForecastService
    {
        public Task<ForecastMessageDTO> GetWeatherForecast(string stationId);
    }
}
