using DMIProxy.ApplicationService;
using DMIProxy.HealthCheck;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using ZiggyCreatures.Caching.Fusion;

namespace DMIProxyTests.HealthCheck;

[TestClass]
public class EDRHealthCheckTests
{
    public TestContext TestContext { get; set; } = null!;

    [TestMethod]
    [DataRow("200,200,200,200,200", HealthStatus.Healthy)]
    [DataRow("200,200,200,200", HealthStatus.Degraded)]
    [DataRow("200,200,429,200,200", HealthStatus.Degraded)]
    [DataRow("429,200,500,200,200", HealthStatus.Unhealthy)]
    [DataRow("401,200,200,200,200", HealthStatus.Unhealthy)]
    [DataRow("201,200,200,200,200", HealthStatus.Unhealthy)]
    [DataRow("0,200,200,200,200", HealthStatus.Unhealthy)]
    public async Task CheckHealthAsync_ClassifiesStatusHistory(string values, HealthStatus expectedStatus)
    {
        ArgumentNullException.ThrowIfNull(values);
        var cache = new FusionCache(new FusionCacheOptions());
        var history = values.Split(',').Select(int.Parse).ToList();
        await cache.SetAsync(EdrStatusHistory.CacheKey, history, token: TestContext.CancellationToken);
        var healthCheck = new EDRHealthCheck(cache);

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext(), TestContext.CancellationToken);

        Assert.AreEqual(expectedStatus, result.Status);
        cache.Dispose();
    }

    [TestMethod]
    public async Task CheckHealthAsync_MissingHistory_IsDegraded()
    {
        var cache = new FusionCache(new FusionCacheOptions());
        var healthCheck = new EDRHealthCheck(cache);

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext(), TestContext.CancellationToken);

        Assert.AreEqual(HealthStatus.Degraded, result.Status);
        cache.Dispose();
    }
}
