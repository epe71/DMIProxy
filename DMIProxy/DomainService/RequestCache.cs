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
    private const string TextForcastCacheKey = "TextForcast-";
    private static readonly object _rainForcastLock = new();
    private static readonly object _edrForcastLock = new();
    private static readonly object _textForcastLock = new();

    public bool GetRainDTO(string stationId, out RainDTO? rainDto)
    => TryGetFromCache(RainCacheKey + stationId, _rainForcastLock, out rainDto);

    public void SaveRainDTO(string stationId, RainDTO rainDTO)
    {
        var nextUpdate = 3;
        if (rainDTO.RainToday > 0) nextUpdate = 2;
        if (rainDTO.Rain1h > 0) nextUpdate = 1;
        var options = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(timeSpanCalculator.AtTheTopOfTheHour(nextUpdate))
            .SetPriority(CacheItemPriority.Normal);
        SaveToCache(RainCacheKey + stationId, rainDTO, options, _rainForcastLock);
    }

    public bool GetEdrForcastDTO(string forcastParameter, out HomeAssistantDTO? forcastDto)
        => TryGetFromCache(EdrCacheKey + forcastParameter, _edrForcastLock, out forcastDto);

    public void SaveEdrForcastDTO(string forcastParameter, HomeAssistantDTO forcastDTO)
    {
        var options = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(new TimeSpan(4, 0, 0))
            .SetPriority(CacheItemPriority.Normal);
        SaveToCache(EdrCacheKey + forcastParameter, forcastDTO, options, _edrForcastLock);
        SaveEdrKey(forcastParameter);
    }

    public bool GetTextForcast(string stationId, out ForcastMessageDTO? dto)
        => TryGetFromCache(TextForcastCacheKey + stationId, _textForcastLock, out dto);

    public void SaveTextForcast(string stationId, ForcastMessageDTO dto)
    {
        var updateTime = new List<TimeOnly> { new(6, 0), new(17, 0) };
        var options = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(timeSpanCalculator.FixTime(updateTime))
            .SetPriority(CacheItemPriority.Normal);
        SaveToCache(TextForcastCacheKey + stationId, dto, options, _textForcastLock);
    }

    public bool GetEdrKeys(out Dictionary<string, DateTime>? keys)
    {
        return cache.TryGetValue(EdrKeysKey, out keys);
    }

    public void SaveEdrKey(string key)
    {
        var options = new MemoryCacheEntryOptions()
         .SetSlidingExpiration(TimeSpan.FromHours(5))
         .SetPriority(CacheItemPriority.Normal);

        if (cache.TryGetValue(EdrKeysKey, out Dictionary<string, DateTime>? keys) && keys != null)
        {
            keys[key] = dateTimeProvider.UtcNow;
            cache.Remove(EdrKeysKey);
            cache.Set(EdrKeysKey, keys, options);
        }
        else
        {
            cache.Set(EdrKeysKey, new Dictionary<string, DateTime> { { key, dateTimeProvider.UtcNow } }, options);
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
            cache.Remove(key);
            cache.Set(key, value, options);
        }
    }
}
