using Microsoft.Extensions.Caching.Memory;
using DMIProxy.Contract;

namespace DMIProxy.DomainService
{
    public class RequestCache : IRequestCache
    {
        private IMemoryCache _cache;

        private const string rainCacheKey = "Rain";

        private const int slidingCacheExpirationMinutes = 5;
        private const int absoluteCacheExpirationHours = 6;

        public RequestCache(IMemoryCache memoryCache)
        {
            _cache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
        }

        public void ClearCache()
        {
            _cache.Remove(rainCacheKey);
        }

        public bool GetRainDTO(string stationId, out RainDTO? rainDto)
        {
            return _cache.TryGetValue(rainCacheKey + stationId, out rainDto);
        }

        public void SaveRainDTO(string stationId, RainDTO rainDTO)
        {
            var cacheEntryOptions = new MemoryCacheEntryOptions()
                       .SetSlidingExpiration(TimeSpan.FromMinutes(slidingCacheExpirationMinutes))
                       .SetAbsoluteExpiration(TimeSpan.FromHours(absoluteCacheExpirationHours))
                       .SetPriority(CacheItemPriority.Normal);
            _cache.Set(rainCacheKey + stationId, rainDTO, cacheEntryOptions);
        }

    }
}
