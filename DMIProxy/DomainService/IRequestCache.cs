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

        bool GetClimateDataDTO(string stationId, string parameterId, out HomeAssistantDTO? dto);
        void SaveClimateDataDTO(string stationId, string parameterId, HomeAssistantDTO dto);

        bool GetTextForecast(string stationId, out ForecastMessageDTO? dto);
        void SaveTextForecast(string stationId, ForecastMessageDTO dto);

        bool GetAllEdrKeys(out Dictionary<string, DateTime>? keys);
        List<string> GetEdrKeysToUpdate(string key);
        void EdrKeyUpdated(string key);

        MemoryCacheStatistics? CacheStatistics();
    }
}
