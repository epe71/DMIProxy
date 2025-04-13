using DMIProxy.BusinessEntity;

namespace DMIProxy.DomainService
{
    public interface IWebScrapeService
    {
        Task<TextForcast> GetWeatherForcast(string stationId);
    }
}
