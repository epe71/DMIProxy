using DMIProxy.BusinessEntity;
using DMIProxy.Contract;
using DMIProxy.DomainService;
using ZiggyCreatures.Caching.Fusion;

namespace DMIProxy.ApplicationService
{
    public class WeatherForecastService(
        IWebScrapeService webScrapeService,
        ITimeSpanCalculator timeSpanCalculator,
        IFusionCache cache) : IWeatherForecastService
    {
        public async Task<ForecastMessageDTO> GetWeatherForecast(string stationId)
        {
            var updateTime = new List<TimeOnly> { new(6, 0), new(10, 0), new(18, 0) };
            var expirationTime = timeSpanCalculator.FixTime(updateTime);

            var forecast = await cache.GetOrSetAsync<TextForecast>(
                $"TextForecast-{stationId}",
                async (_, _) => await webScrapeService.GetWeatherForecast(stationId),
                options => options.SetDuration(expirationTime)
            );

            var dto = new ForecastMessageDTO()
            {
                Time = forecast.TimeStamp,
                Headline = forecast.Headline,
                Message = TrimForecast.TrimText(forecast.Forecast)
            };

            return dto;
        }
    }
}
