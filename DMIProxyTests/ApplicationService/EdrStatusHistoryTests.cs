using DMIProxy.ApplicationService;
using ZiggyCreatures.Caching.Fusion;

namespace DMIProxyTests.ApplicationService;

[TestClass]
public class EdrStatusHistoryTests
{
    public TestContext TestContext { get; set; } = null!;

    [TestMethod]
    public async Task AddAsync_KeepsFiveNewestValuesInFifoOrder()
    {
        var cache = new FusionCache(new FusionCacheOptions());
        using var history = new EdrStatusHistory(cache);

        foreach (var status in new[] { 200, 200, 200, 200, 429 })
            await history.AddAsync(status, TestContext.CancellationToken);

        await history.AddAsync(200, TestContext.CancellationToken);
        var afterFourGoodStatuses = cache.TryGet<List<int>>(EdrStatusHistory.CacheKey, token: TestContext.CancellationToken);
        CollectionAssert.AreEqual(new[] { 200, 200, 200, 429, 200 }, afterFourGoodStatuses.Value);

        for (var i = 0; i < 4; i++)
            await history.AddAsync(200, TestContext.CancellationToken);

        var afterFiveGoodStatuses = cache.TryGet<List<int>>(EdrStatusHistory.CacheKey, token: TestContext.CancellationToken);
        CollectionAssert.AreEqual(new[] { 200, 200, 200, 200, 200 }, afterFiveGoodStatuses.Value);
        cache.Dispose();
    }

    [TestMethod]
    public async Task AddAsync_ConcurrentWrites_DoNotLoseHistoryEntries()
    {
        var cache = new FusionCache(new FusionCacheOptions());
        using var history = new EdrStatusHistory(cache);

        await Task.WhenAll(Enumerable.Range(200, 20).Select(status => history.AddAsync(status, TestContext.CancellationToken)));

        var cached = cache.TryGet<List<int>>(EdrStatusHistory.CacheKey, token: TestContext.CancellationToken);
        Assert.IsTrue(cached.HasValue);
        Assert.HasCount(EdrStatusHistory.Capacity, cached.Value!);
        Assert.HasCount(EdrStatusHistory.Capacity, cached.Value!.Distinct());
        cache.Dispose();
    }
}
