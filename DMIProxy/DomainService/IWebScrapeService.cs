using DMIProxy.BusinessEntity;

namespace DMIProxy.DomainService
{
    public interface IWebScrapeService
    {
        Task<TextForecast> GetWeatherForecast(string stationId);
    }
}
