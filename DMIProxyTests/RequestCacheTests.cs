using DMIProxy.BusinessEntity;
using DMIProxy.Contract;
using DMIProxy.DomainService;
using DMIProxyTests.Builder;
using Microsoft.Extensions.Caching.Memory;

namespace DMIProxyTests;

[TestClass]
public class RequestCacheTests
{
    private RequestCache CreateRequestCache(out IMemoryCache memoryCache, out ITimeSpanCalculator timeSpanCalculator, out IDateTimeProvider dateTimeProvider)
    {
        memoryCache = new MemoryCache(new MemoryCacheOptions());
        var dateTimes = new List<DateTime>
        {
            new DateTime(2025, 3, 30, 7, 0, 20, DateTimeKind.Local),
            new DateTime(2025, 3, 30, 7, 1, 20, DateTimeKind.Local),
            new DateTime(2025, 3, 30, 7, 2, 20, DateTimeKind.Local),
            new DateTime(2025, 3, 30, 7, 3, 20, DateTimeKind.Local),
            new DateTime(2025, 3, 30, 7, 4, 20, DateTimeKind.Local),
            new DateTime(2025, 3, 30, 8, 5, 20, DateTimeKind.Local),
            new DateTime(2025, 3, 30, 9, 5, 20, DateTimeKind.Local),
            new DateTime(2025, 3, 30, 10, 5, 20, DateTimeKind.Local),
            new DateTime(2025, 3, 30, 11, 5, 20, DateTimeKind.Local),
            new DateTime(2025, 3, 30, 12, 5, 20, DateTimeKind.Local),
            new DateTime(2025, 3, 30, 13, 5, 20, DateTimeKind.Local),
            new DateTime(2025, 3, 30, 14, 5, 20, DateTimeKind.Local),
        };
        dateTimeProvider = new MockDateTimeProviderBuilder()
                .WithUtcTimeSequnce(dateTimes)
                .Build();
        timeSpanCalculator = new TimeSpanCalculator(dateTimeProvider);
        return new RequestCache(memoryCache, timeSpanCalculator, dateTimeProvider);
    }

    [TestMethod]
    public void SaveRainDTO_ShouldSaveRainDTO()
    {
        // Arrange
        var requestCache = CreateRequestCache(out _, out _, out var dateTimeProvider);
        var rainDTO = new RainDTO { Rain1h = 1, RainToday = 2 };

        // Act
        requestCache.SaveRainDTO("1234", rainDTO);
        requestCache.GetRainDTO("1234", out var cacheItem);

        // Assert
        Assert.AreEqual(rainDTO.Rain1h, cacheItem?.Rain1h);
        Assert.AreEqual(rainDTO.RainToday, cacheItem?.RainToday);
    }

    [TestMethod]
    public void SaveEdrKeys_TwoDistinctKeys_ShouldStoreBoth()
    {
        // Arrange
        var requestCache = CreateRequestCache(out _, out _, out var dateTimeProvider);
        requestCache.EdrKeyUpdated("key1");
        requestCache.EdrKeyUpdated("key2");

        // Act
        requestCache.GetAllEdrKeys(out var keys);

        // Assert
        Assert.IsNotNull(keys);
        Assert.AreEqual(2, keys.Count);
    }

    [TestMethod]
    public void SaveEdrKeys_SameKeyTwice_ShouldUpdateKey()
    {
        // Arrange
        var requestCache = CreateRequestCache(out _, out _, out var dateTimeProvider);
        requestCache.EdrKeyUpdated("key1");
        var firstTime = GetEdrKeyTime(requestCache, "key1");
        Task.Delay(10).Wait(); // ensure a small delay for update
        requestCache.EdrKeyUpdated("key1");

        // Act
        var secondTime = GetEdrKeyTime(requestCache, "key1");

        // Assert
        Assert.IsNotNull(firstTime);
        Assert.IsNotNull(secondTime);
        Assert.IsTrue(secondTime > firstTime);
    }

    private DateTime GetEdrKeyTime(RequestCache cache, string key)
    {
        cache.GetAllEdrKeys(out var keys);
        return keys != null && keys.TryGetValue(key, out var time) ? time : DateTime.MinValue;
    }

    [TestMethod]
    public void SaveTextForecast_ShouldSaveAndRetrieve()
    {
        // Arrange
        var requestCache = CreateRequestCache(out _, out _, out var dateTimeProvider);
        var dto = new ForecastMessageDTO { Time = DateTime.UtcNow, Headline = "headline", Message = "message" };
        requestCache.SaveTextForecast("station1", dto);

        // Act
        requestCache.GetTextForecast("station1", out var cached);

        // Assert
        Assert.IsNotNull(cached);
        Assert.AreEqual(dto.Headline, cached.Headline);
        Assert.AreEqual(dto.Message, cached.Message);
    }

    [TestMethod]
    public void SaveEdrForecastDTO_ShouldSaveAndRetrieve()
    {
        // Arrange
        var requestCache = CreateRequestCache(out _, out _, out var dateTimeProvider);
        var dto = new HomeAssistantDTO { data = new List<PointDTO>(), description = "desc" };
        requestCache.SaveEdrForecastDTO("param1", dto);

        // Act
        requestCache.GetEdrForecastDTO("param1", out var cached);

        // Assert
        Assert.IsNotNull(cached);
        Assert.AreEqual(dto.description, cached.description);
    }

    [TestMethod]
    public void GetRainDTO_ShouldReturnFalseIfNotFound()
    {
        // Arrange
        var requestCache = CreateRequestCache(out _, out _, out var dateTimeProvider);

        // Act
        var found = requestCache.GetRainDTO("notfound", out var dto);

        // Assert
        Assert.IsFalse(found);
        Assert.IsNull(dto);
    }

    [TestMethod]
    public void SaveEdrKeys_ConcurrentAccess_ShouldMaintainCorrectCount()
    {
        // Arrange
        var requestCache = CreateRequestCache(out _, out _, out var dateTimeProvider);

        // Act
        Parallel.For(0, 50, i =>
        {
            requestCache.EdrKeyUpdated("concurrentKey" + (i % 5));
        });
        requestCache.GetAllEdrKeys(out var keys);

        // Assert: There should be exactly 5 distinct keys
        Assert.IsNotNull(keys);
        Assert.AreEqual(5, keys.Count);
    }

    [TestMethod]
    public void GetEdrKeysToUpdate_FirstCallWithNewKey_ReturnsEmptyString()
    {
        // Arrange
        var requestCache = CreateRequestCache(out _, out _, out var dateTimeProvider);
        string newKey = "test-key";

        // Act
        var result = requestCache.GetEdrKeysToUpdate(newKey);

        // Assert
        Assert.AreEqual(0, result.Count);
        requestCache.GetAllEdrKeys(out var keys);
        Assert.IsNotNull(keys);
        Assert.IsTrue(keys.ContainsKey(newKey), "Key1 not found");
    }

    [TestMethod]
    public void GetEdrKeysToUpdate_SecondCallWithKey_ReturnsAllKeys()
    {
        // Arrange
        var requestCache = CreateRequestCache(out _, out _, out var dateTimeProvider);

        requestCache.GetEdrKeysToUpdate("Key1");
        requestCache.GetEdrKeysToUpdate("Key2");

        // Act
        var result = requestCache.GetEdrKeysToUpdate("Key1");

        // Assert
        Assert.AreEqual(2, result.Count);
        requestCache.GetAllEdrKeys(out var keys);
        Assert.IsNotNull(keys);
        Assert.IsTrue(keys.ContainsKey("Key1"), "Key1 not found");
    }

    [TestMethod]

    public void GetEdrKeysToUpdate_AfterKeyExpired_ReturnsAllKeys()
    {
        // Arrange
        var requestCache = CreateRequestCache(out _, out _, out var dateTimeProvider);

        requestCache.EdrKeyUpdated("Key1");
        for (int i = 1; i <= 8; i++)
        {
            requestCache.GetEdrKeysToUpdate("Key1");
        }

        // Act
        var result = requestCache.GetEdrKeysToUpdate("Key1");

        // Assert
        Assert.AreEqual(1, result.Count);
        requestCache.GetAllEdrKeys(out var keys);
        Assert.IsNotNull(keys);
        Assert.IsTrue(keys.ContainsKey("Key1"), "Key1 not found");
    }

    [TestMethod]
    public void GetEdrKeysToUpdate_SecondCallWithKeyAfterUpdate_ReturnsEmptyString()
    {
        // Arrange
        var requestCache = CreateRequestCache(out _, out _, out var dateTimeProvider);

        requestCache.GetEdrKeysToUpdate("Key1");
        requestCache.EdrKeyUpdated("Key1");

        // Act
        var result = requestCache.GetEdrKeysToUpdate("Key1");

        // Assert
        Assert.AreEqual(0, result.Count);
        requestCache.GetAllEdrKeys(out var keys);
        Assert.IsNotNull(keys);
        Assert.IsTrue(keys.ContainsKey("Key1"), "Key1 not found");
    }

    [TestMethod]
    public void Concurrent_SaveAndGet_RainDTO_ShouldWorkCorrectly()
    {
        // Arrange
        var requestCache = CreateRequestCache(out _, out _, out var dateTimeProvider);

        // Act
        Parallel.For(0, 100, i =>
        {
            // Each thread saves a RainDTO with values based on iteration index.
            var rainDTO = new RainDTO { Rain1h = i % 5, RainToday = i % 3 };
            requestCache.SaveRainDTO("concurrent", rainDTO);
        });
        requestCache.GetRainDTO("concurrent", out var result);

        // Assert
        Assert.IsNotNull(result);
        // Verify that the resulting values fall within the expected ranges.
        Assert.IsTrue(result.Rain1h >= 0 && result.Rain1h <= 4, "Rain1h");
        Assert.IsTrue(result.RainToday >= 0 && result.RainToday <= 2, "RainToday");
    }
}
