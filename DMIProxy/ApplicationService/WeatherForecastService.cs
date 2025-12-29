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
                Message = TrimForecastText(forecast.Forecast)
            };

            return dto;
        }

        private string TrimForecastText(string text)
        {
            if (text == null)
            {
                return string.Empty;
            }

            text = text.Replace("Temp.", "Temperatur", StringComparison.InvariantCultureIgnoreCase);

            text = CutTextFrom(text, "I aften");
            text = CutTextFrom(text, "I nat");
            text = CutTextFrom(text, "I morgen");
            text = CutTextFrom(text, "Mandag");
            text = CutTextFrom(text, "Tirsdag");
            text = CutTextFrom(text, "Onsdag");
            text = CutTextFrom(text, "Torsdag");
            text = CutTextFrom(text, "Fredag");
            text = CutTextFrom(text, "Lørdag");
            text = CutTextFrom(text, "Søndag");

            return text;
        }

        private string CutTextFrom(string text, string cutAfter)
        {
            if (text.IndexOf(cutAfter) > 0)
            {
                text = text.Substring(0, text.IndexOf(cutAfter));
            }
            return text;
        }
    }
}
