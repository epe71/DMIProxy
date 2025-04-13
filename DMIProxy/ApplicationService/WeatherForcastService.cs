using DMIProxy.Contract;
using DMIProxy.DomainService;

namespace DMIProxy.ApplicationService
{
    public class WeatherForcastService(IWebScrapeService webScrapeService, IRequestCache requestCache) : IWeatherForcastService
    {
        public async Task<ForcastMessageDTO> GetWeatherForcast(string stationId)
        {
            if (requestCache.GetTextForcast(stationId, out ForcastMessageDTO? dto) && dto != null)
            {
                return dto;
            }

            var forcast = await webScrapeService.GetWeatherForcast(stationId);
            dto = new ForcastMessageDTO()
            {
                Time = forcast.TimeStamp,
                Headline = forcast.Headline,
                Message = TrimForcastText(forcast.Forcast)
            };
            requestCache.SaveTextForcast(stationId, dto);

            return dto;
        }

        private string TrimForcastText(string text)
        {
            if (text == null)
            {
                return string.Empty;
            }

            text = text.Replace("Temp.", "Temperatur",StringComparison.InvariantCultureIgnoreCase);

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
