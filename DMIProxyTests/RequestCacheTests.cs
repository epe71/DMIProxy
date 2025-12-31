using DMIProxy.BusinessEntity;
using DMIProxy.DomainService;
using DMIProxyTests.Builder;
using Microsoft.Extensions.Caching.Memory;

namespace DMIProxyTests;

[TestClass]
public class RequestCacheTests
{
    private RequestCache CreateRequestCache(out IMemoryCache memoryCache, out IDateTimeProvider dateTimeProvider)
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
        return new RequestCache(memoryCache, dateTimeProvider);
    }

    [TestMethod]
    public void SaveEdrKeys_TwoDistinctKeys_ShouldStoreBoth()
    {
        // Arrange
        var requestCache = CreateRequestCache(out _, out var dateTimeProvider);
        requestCache.EdrKeyUpdated("key1");
        requestCache.EdrKeyUpdated("key2");

        // Act
        requestCache.GetAllEdrKeys(out var keys);

        // Assert
        Assert.IsNotNull(keys);
        Assert.HasCount(2, keys);
    }

    [TestMethod]
    public void SaveEdrKeys_SameKeyTwice_ShouldUpdateKey()
    {
        // Arrange
        var requestCache = CreateRequestCache(out _, out var dateTimeProvider);
        requestCache.EdrKeyUpdated("key1");
        var firstTime = GetEdrKeyTime(requestCache, "key1");
        Task.Delay(10).Wait(); // ensure a small delay for update
        requestCache.EdrKeyUpdated("key1");

        // Act
        var secondTime = GetEdrKeyTime(requestCache, "key1");

        // Assert
        Assert.IsTrue(secondTime > firstTime);
    }

    private DateTime GetEdrKeyTime(RequestCache cache, string key)
    {
        cache.GetAllEdrKeys(out var keys);
        return keys != null && keys.TryGetValue(key, out var time) ? time : DateTime.MinValue;
    }

    [TestMethod]
    public void SaveEdrKeys_ConcurrentAccess_ShouldMaintainCorrectCount()
    {
        // Arrange
        var requestCache = CreateRequestCache(out _, out var dateTimeProvider);

        // Act
        Parallel.For(0, 50, i =>
        {
            requestCache.EdrKeyUpdated("concurrentKey" + (i % 5));
        });
        requestCache.GetAllEdrKeys(out var keys);

        // Assert: There should be exactly 5 distinct keys
        Assert.IsNotNull(keys);
        Assert.HasCount(5, keys);
    }

    [TestMethod]
    public void GetEdrKeysToUpdate_FirstCallWithNewKey_ReturnsEmptyString()
    {
        // Arrange
        var requestCache = CreateRequestCache(out _, out var dateTimeProvider);
        string newKey = "test-key";

        // Act
        var result = requestCache.GetEdrKeysToUpdate(newKey);

        // Assert
        Assert.IsEmpty(result);
        requestCache.GetAllEdrKeys(out var keys);
        Assert.IsNotNull(keys);
        Assert.IsTrue(keys.ContainsKey(newKey), "Key1 not found");
    }

    [TestMethod]
    public void GetEdrKeysToUpdate_SecondCallWithKey_ReturnsAllKeys()
    {
        // Arrange
        var requestCache = CreateRequestCache(out _, out var dateTimeProvider);

        requestCache.GetEdrKeysToUpdate("Key1");
        requestCache.GetEdrKeysToUpdate("Key2");

        // Act
        var result = requestCache.GetEdrKeysToUpdate("Key1");

        // Assert
        Assert.HasCount(2, result);
        requestCache.GetAllEdrKeys(out var keys);
        Assert.IsNotNull(keys);
        Assert.IsTrue(keys.ContainsKey("Key1"), "Key1 not found");
    }

    [TestMethod]
    public void GetEdrKeysToUpdate_AfterKeyExpired_ReturnsAllKeys()
    {
        // Arrange
        var requestCache = CreateRequestCache(out _, out var dateTimeProvider);

        requestCache.EdrKeyUpdated("Key1");
        for (int i = 1; i <= 8; i++)
        {
            requestCache.GetEdrKeysToUpdate("Key1");
        }

        // Act
        var result = requestCache.GetEdrKeysToUpdate("Key1");

        // Assert
        Assert.HasCount(1, result);
        requestCache.GetAllEdrKeys(out var keys);
        Assert.IsNotNull(keys);
        Assert.IsTrue(keys.ContainsKey("Key1"), "Key1 not found");
    }

    [TestMethod]
    public void GetEdrKeysToUpdate_SecondCallWithKeyAfterUpdate_ReturnsEmptyString()
    {
        // Arrange
        var requestCache = CreateRequestCache(out _, out var dateTimeProvider);

        requestCache.GetEdrKeysToUpdate("Key1");
        requestCache.EdrKeyUpdated("Key1");

        // Act
        var result = requestCache.GetEdrKeysToUpdate("Key1");

        // Assert
        Assert.IsEmpty(result);
        requestCache.GetAllEdrKeys(out var keys);
        Assert.IsNotNull(keys);
        Assert.IsTrue(keys.ContainsKey("Key1"), "Key1 not found");
    }
}