using Microsoft.Extensions.Caching.Memory;
using DMIProxy.Contract;
using DMIProxy.BusinessEntity;

namespace DMIProxy.DomainService;

public class RequestCache(IMemoryCache cache, ITimeSpanCalculator timeSpanCalculator, IDateTimeProvider dateTimeProvider) : IRequestCache
{
    private const string rainCacheKey = "Rain-";
    private const string edrCacheKey = "EDR-";
    private const string edrKeysKey = "EdrKeys";
    private const string textForcastCacheKey = "TextForcast-";

    public bool GetRainDTO(string stationId, out RainDTO? rainDto)
    {
        return cache.TryGetValue(rainCacheKey + stationId, out rainDto);
    }

    public void SaveRainDTO(string stationId, RainDTO rainDTO)
    {
        var nextUpdate = 3;
        if (rainDTO.RainToday > 0) { nextUpdate = 2; }
        if (rainDTO.Rain1h > 0) { nextUpdate = 1; }

        var cacheEntryOptions = new MemoryCacheEntryOptions()
                   .SetAbsoluteExpiration(timeSpanCalculator.AtTheTopOfTheHour(nextUpdate))
                   .SetPriority(CacheItemPriority.Normal);
        cache.Remove(rainCacheKey + stationId);
        cache.Set(rainCacheKey + stationId, rainDTO, cacheEntryOptions);
    }

    public bool GetEdrForcastDTO(string forcastParameter, out HomeAssistantDTO? forcastDto)
    {
        return cache.TryGetValue(edrCacheKey + forcastParameter, out forcastDto);
    }

    public void SaveEdrForcastDTO(string forcastParameter, HomeAssistantDTO forcastDTO)
    {
        var cacheEntryOptions = new MemoryCacheEntryOptions()
                   .SetAbsoluteExpiration(timeSpanCalculator.AtTheTopOfTheHour(4))
                   .SetPriority(CacheItemPriority.Normal);
        cache.Remove(edrCacheKey + forcastParameter);
        cache.Set(edrCacheKey + forcastParameter, forcastDTO, cacheEntryOptions);
        SaveEdrKey(forcastParameter);
    }

    public bool GetTextForcast(string stationId, out ForcastMessageDTO? dto)
    {
        return cache.TryGetValue(textForcastCacheKey + stationId, out dto);
    }

    public void SaveTextForcast(string stationId, ForcastMessageDTO dto)
    {
        var updateTime = new List<TimeOnly> 
        { 
            new(6, 0), 
            new(17, 0)   
        };
        var cacheEntryOptions = new MemoryCacheEntryOptions()
                   .SetAbsoluteExpiration(timeSpanCalculator.FixTime(updateTime))
                   .SetPriority(CacheItemPriority.Normal);
        cache.Remove(textForcastCacheKey + stationId);
        cache.Set(textForcastCacheKey + stationId, dto, cacheEntryOptions);
    }

    public bool GetEdrKeys(out Dictionary<string, DateTime>? keys)
    {
        return cache.TryGetValue(edrKeysKey, out keys);
    }

    public void SaveEdrKey(string key)
    {
        var cacheEntryOptions = new MemoryCacheEntryOptions()
            .SetSlidingExpiration(TimeSpan.FromHours(5))
            .SetPriority(CacheItemPriority.Normal);

        if (cache.TryGetValue(edrKeysKey, out Dictionary<string, DateTime>? keys) && keys != null)
        {
            keys.Add(key, dateTimeProvider.UtcNow);
            cache.Remove(edrKeysKey);
            cache.Set(edrKeysKey, keys, cacheEntryOptions);
        }
        else
        {
            var newKeys = new Dictionary<string, DateTime>
            {
                { key, dateTimeProvider.UtcNow }
            };
            cache.Set(edrKeysKey, newKeys, cacheEntryOptions);
        }
    }

    public MemoryCacheStatistics? CacheStatistics()
    {
        return cache.GetCurrentStatistics();
    }
}
