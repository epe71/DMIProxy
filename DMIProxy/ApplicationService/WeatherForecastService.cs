using DMIProxy.Contract;
using DMIProxy.DomainService;

namespace DMIProxy.ApplicationService
{
    public class WeatherForecastService(
        IWebScrapeService webScrapeService,
        IRequestCache requestCache) : IWeatherForecastService
    {
        public async Task<ForecastMessageDTO> GetWeatherForecast(string stationId)
        {
            if (requestCache.GetTextForecast(stationId, out ForecastMessageDTO? dto) && dto != null)
            {
                return dto;
            }

            var forecast = await webScrapeService.GetWeatherForecast(stationId);
            dto = new ForecastMessageDTO()
            {
                Time = forecast.TimeStamp,
                Headline = forecast.Headline,
                Message = TrimForecastText(forecast.Forecast)
            };
            requestCache.SaveTextForecast(stationId, dto);

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
