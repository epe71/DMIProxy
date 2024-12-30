using Microsoft.Extensions.Caching.Memory;
using DMIProxy.Contract;

namespace DMIProxy.DomainService
{
    public class RequestCache : IRequestCache
    {
        private readonly IMemoryCache _cache;

        private const string rainCacheKey = "Rain";
        private const string forcastCacheKey = "Forcast";
        private const string cloudForcastCacheKey = "CloudForcast";

        public RequestCache(IMemoryCache memoryCache)
        {
            _cache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
        }

        public bool GetRainDTO(string stationId, out RainDTO? rainDto)
        {
            return _cache.TryGetValue(rainCacheKey + stationId, out rainDto);
        }

        public void SaveRainDTO(string stationId, RainDTO rainDTO)
        {
            var nextUpdate = 3;
            if (rainDTO.RainToday > 0) { nextUpdate = 2; }
            if (rainDTO.Rain1h > 0) { nextUpdate = 1; }

            var cacheEntryOptions = new MemoryCacheEntryOptions()
                       .SetAbsoluteExpiration(AbsoluteCacheExpirationTimeInHour(nextUpdate))
                       .SetPriority(CacheItemPriority.Normal);
            _cache.Remove(rainCacheKey + stationId);
            _cache.Set(rainCacheKey + stationId, rainDTO, cacheEntryOptions);
        }

        public bool GetEdrForcastDTO(string forcastParameter, out HomeAssistantDTO? forcastDto)
        {
            return _cache.TryGetValue("EDR-" + forcastParameter, out forcastDto);
        }

        public void SaveEdrForcastDTO(string forcastParameter, HomeAssistantDTO forcastDTO)
        {
            var cacheEntryOptions = new MemoryCacheEntryOptions()
                       .SetAbsoluteExpiration(AbsoluteCacheExpirationTimeInHour(4))
                       .SetPriority(CacheItemPriority.Normal);
            _cache.Remove("EDR-" + forcastParameter);
            _cache.Set("EDR-" + forcastParameter, forcastDTO, cacheEntryOptions);
        }

        private TimeSpan AbsoluteCacheExpirationTimeInHour(int hours)
        {
            var minutesToTopOfTheHour = 5 + 60 - DateTime.Now.Minute;
            int absoluteCacheExpirationMinutes = (hours-1) * 60 + minutesToTopOfTheHour;
            return TimeSpan.FromMinutes(absoluteCacheExpirationMinutes);
        }

        public MemoryCacheStatistics? CacheStatistics()
        {
            return _cache.GetCurrentStatistics();
        }
    }
}
