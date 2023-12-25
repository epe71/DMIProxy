using Microsoft.Extensions.Caching.Memory;
using DMIProxy.Contract;

namespace DMIProxy.DomainService
{
    public class RequestCache : IRequestCache
    {
        private IMemoryCache _cache;

        private const string rainCacheKey = "Rain";
        private const string forcastCacheKey = "Forcast";

        private const int slidingCacheExpirationMinutes = 15;

        public RequestCache(IMemoryCache memoryCache)
        {
            _cache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
        }

        public void ClearCache()
        {
            _cache.Remove(rainCacheKey);
            _cache.Remove(forcastCacheKey);
        }

        public bool GetRainDTO(string stationId, out RainDTO? rainDto)
        {
            return _cache.TryGetValue(rainCacheKey + stationId, out rainDto);
        }

        public void SaveRainDTO(string stationId, RainDTO rainDTO)
        {
            var nextUpdate = 4;
            if (rainDTO.RainToday > 0) { nextUpdate = 2; }
            if (rainDTO.Rain1h > 0) { nextUpdate = 1; }

            var cacheEntryOptions = new MemoryCacheEntryOptions()
                       .SetSlidingExpiration(TimeSpan.FromMinutes(slidingCacheExpirationMinutes))
                       .SetAbsoluteExpiration(AbsoluteCacheExpirationTimeInHour(nextUpdate))
                       .SetPriority(CacheItemPriority.Normal);
            _cache.Set(rainCacheKey + stationId, rainDTO, cacheEntryOptions);
        }

        public bool GetForcastDTO(out ForcastDTO? forcastDto)
        {
            return _cache.TryGetValue(forcastCacheKey, out forcastDto);
        }

        public void SaveForcastDTO(ForcastDTO forcastDTO)
        {
            var timeOut = forcastDTO.StartTime.AddHours(6).AddMinutes(1) - DateTime.UtcNow;

            var cacheEntryOptions = new MemoryCacheEntryOptions()
                       .SetAbsoluteExpiration(timeOut)
                       .SetPriority(CacheItemPriority.Normal);
            _cache.Set(forcastCacheKey, forcastDTO, cacheEntryOptions);
        }

        private TimeSpan AbsoluteCacheExpirationTimeInHour(int hours)
        {
            var minutesToTopOfTheHour = 5 + 60 - DateTime.Now.Minute;
            int absoluteCacheExpirationMinutes = (hours-1) * 60 + minutesToTopOfTheHour;
            return TimeSpan.FromMinutes(absoluteCacheExpirationMinutes);
        }
    }
}
