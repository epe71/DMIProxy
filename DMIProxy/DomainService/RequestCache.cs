using Microsoft.Extensions.Caching.Memory;
using DMIProxy.Contract;
using DMIProxy.BusinessEntity;

namespace DMIProxy.DomainService;

public class RequestCache(
    IMemoryCache cache, 
    ITimeSpanCalculator timeSpanCalculator, 
    IDateTimeProvider dateTimeProvider) : IRequestCache
{
    private const string RainCacheKey = "Rain-";
    private const string EdrCacheKey = "EDR-";
    private const string EdrKeysKey = "EdrKeys";
    private const string TextForecastCacheKey = "TextForecast-";
    private static readonly object _rainLock = new();
    private static readonly object _edrLock = new();
    private static readonly object _textLock = new();
    private static readonly object _edrKeysLock = new();

    public TimeSpan edrKeyTimeOut = new TimeSpan(4, 0, 0);

    public bool GetRainDTO(string stationId, out RainDTO? rainDto)
    => TryGetFromCache(RainCacheKey + stationId, _rainLock, out rainDto);

    public void SaveRainDTO(string stationId, RainDTO rainDTO)
    {
        var nextUpdate = 3;
        if (rainDTO.RainToday > 0) nextUpdate = 2;
        if (rainDTO.Rain1h > 0) nextUpdate = 1;
        var options = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(timeSpanCalculator.AtTheTopOfTheHour(nextUpdate))
            .SetPriority(CacheItemPriority.Normal);
        SaveToCache(RainCacheKey + stationId, rainDTO, options, _rainLock);
    }

    public bool GetEdrForecastDTO(string forecastParameter, out HomeAssistantDTO? forecastDto)
        => TryGetFromCache(EdrCacheKey + forecastParameter, _edrLock, out forecastDto);

    public void SaveEdrForecastDTO(string forecastParameter, HomeAssistantDTO forecastDTO)
    {
        var options = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(TimeSpan.FromHours(4))
            .SetPriority(CacheItemPriority.Normal);
        SaveToCache(EdrCacheKey + forecastParameter, forecastDTO, options, _edrLock);
        EdrKeyUpdated(forecastParameter);
    }

    public bool GetTextForecast(string stationId, out ForecastMessageDTO? dto)
        => TryGetFromCache(TextForecastCacheKey + stationId, _textLock, out dto);

    public void SaveTextForecast(string stationId, ForecastMessageDTO dto)
    {
        var updateTime = new List<TimeOnly> { new(6, 0), new(17, 0) };
        var options = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(timeSpanCalculator.FixTime(updateTime))
            .SetPriority(CacheItemPriority.Normal);
        SaveToCache(TextForecastCacheKey + stationId, dto, options, _textLock);
    }

    public bool GetAllEdrKeys(out Dictionary<string, DateTime>? keys)
    => TryGetFromCache(EdrKeysKey, _edrKeysLock, out keys);

    public List<string> GetEdrKeysToUpdate(string key)
    {
        var cacheEntryOptions = new MemoryCacheEntryOptions()
            .SetSlidingExpiration(TimeSpan.FromHours(5))
            .SetPriority(CacheItemPriority.Normal);

        // Helper to update the cache with the current keys dictionary
        void UpdateCache(Dictionary<string, DateTime> edrKeys) =>
            cache.Set(EdrKeysKey, edrKeys, cacheEntryOptions);

        lock (_edrKeysLock)
        {
            if (!cache.TryGetValue(EdrKeysKey, out Dictionary<string, DateTime>? edrKeys) || edrKeys == null)
            {
                // Initialize the cache if it doesn't exist
                UpdateCache(new Dictionary<string, DateTime>
                {
                    [key] = dateTimeProvider.UtcNow - edrKeyTimeOut
                });
                return [];
            }

            var keyFound = edrKeys.TryGetValue(key, out DateTime lastUpdated);
            if (!keyFound)
            {
                // Add keys to list and nothing to update
                edrKeys[key] = dateTimeProvider.UtcNow - edrKeyTimeOut;
                UpdateCache(edrKeys);
                return [];
            }

            if (keyFound && (dateTimeProvider.UtcNow - lastUpdated > edrKeyTimeOut))
            {
                // Keys need update, update some and extend expiration by 2 minute
                var keysToUpdate = edrKeys.OrderBy(k => k.Value).Take(10);
                foreach (var k in keysToUpdate.Select(k => k.Key).ToList())
                    edrKeys[k] = edrKeys[k].AddMinutes(2);

                UpdateCache(edrKeys);
                return keysToUpdate.Select(k => k.Key).ToList();
            }

            return [];
        }
    }

    public void EdrKeyUpdated(string key)
    {
        var cacheEntryOptions = new MemoryCacheEntryOptions()
         .SetSlidingExpiration(edrKeyTimeOut)
         .SetPriority(CacheItemPriority.Normal);

        lock (_edrKeysLock)
        {
            if (cache.TryGetValue(EdrKeysKey, out Dictionary<string, DateTime>? keys) && keys != null)
            {
                keys[key] = dateTimeProvider.UtcNow;
                cache.Set(EdrKeysKey, keys, cacheEntryOptions);
            }
            else
            {
                cache.Set(EdrKeysKey, new Dictionary<string, DateTime> { { key, dateTimeProvider.UtcNow } }, cacheEntryOptions);
            }
        }
    }

    public MemoryCacheStatistics? CacheStatistics()
        => cache.GetCurrentStatistics();

    private bool TryGetFromCache<T>(string key, object lockObj, out T? value)
    {
        lock (lockObj)
        {
            return cache.TryGetValue(key, out value);
        }
    }

    private void SaveToCache<T>(string key, T value, MemoryCacheEntryOptions options, object lockObj)
    {
        lock (lockObj)
        {
            cache.Set(key, value, options);
        }
    }
}