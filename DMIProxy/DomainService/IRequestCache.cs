using DMIProxy.Contract;
using Microsoft.Extensions.Caching.Memory;

namespace DMIProxy.DomainService
{
    public interface IRequestCache
    {
        bool GetRainDTO(string stationId, out RainDTO? rainDto);
        void SaveRainDTO(string stationId, RainDTO rainDTO);

        bool GetEdrForcastDTO(string forcastParameter, out HomeAssistantDTO? forcastDto);
        void SaveEdrForcastDTO(string forcastParameter, HomeAssistantDTO forcastDTO);

        bool GetTextForcast(string stationId, out ForcastMessageDTO? dto);
        void SaveTextForcast(string stationId, ForcastMessageDTO dto);

        bool GetEdrKeys(out Dictionary<string, DateTime>? keys);
        void SaveEdrKey(string key);

        MemoryCacheStatistics? CacheStatistics();
    }
}
