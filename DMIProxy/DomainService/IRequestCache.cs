using DMIProxy.Contract;
using Microsoft.Extensions.Caching.Memory;

namespace DMIProxy.DomainService
{
    public interface IRequestCache
    {
        bool GetRainDTO(string stationId, out RainDTO? rainDto);
        void SaveRainDTO(string stationId, RainDTO rainDTO);

        bool GetEdrForecastDTO(string forecastParameter, out HomeAssistantDTO? forecastDto);
        void SaveEdrForecastDTO(string forecastParameter, HomeAssistantDTO forecastDTO);

        bool GetTextForecast(string stationId, out ForecastMessageDTO? dto);
        void SaveTextForecast(string stationId, ForecastMessageDTO dto);

        bool GetEdrKeys(out Dictionary<string, DateTime>? keys);
        void SaveEdrKey(string key);

        MemoryCacheStatistics? CacheStatistics();
    }
}
