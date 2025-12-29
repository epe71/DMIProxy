using DMIProxy.Contract;
using Microsoft.Extensions.Caching.Memory;

namespace DMIProxy.DomainService
{
    public interface IRequestCache
    {
        bool GetEdrForecastDTO(string forecastParameter, out HomeAssistantDTO? forecastDto);
        void SaveEdrForecastDTO(string forecastParameter, HomeAssistantDTO forecastDTO);

        bool GetAllEdrKeys(out Dictionary<string, DateTime>? keys);
        List<string> GetEdrKeysToUpdate(string key);
        void EdrKeyUpdated(string key);

        MemoryCacheStatistics? CacheStatistics();
    }
}
