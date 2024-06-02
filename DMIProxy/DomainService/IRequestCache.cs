using DMIProxy.Contract;
using Microsoft.Extensions.Caching.Memory;

namespace DMIProxy.DomainService
{
    public interface IRequestCache
    {
        bool GetRainDTO(string stationId, out RainDTO? rainDto);
        void SaveRainDTO(string stationId, RainDTO rainDTO);

        bool GetForcastDTO(out ForcastDTO? forcastDto);
        void SaveForcastDTO(ForcastDTO forcastDTO);

        MemoryCacheStatistics? CacheStatistics();
    }
}
