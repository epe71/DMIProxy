using DMIProxy.ApplicationService;
using DMIProxy.Contract;
using DMIProxy.DomainService;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Net;
using System.Collections.ObjectModel;
using ZiggyCreatures.Caching.Fusion;

namespace DMIProxyTests.ApplicationService;

[TestClass]
public class EdrApplicationServiceStatusTests
{
    [TestMethod]
    public async Task GetEdrForecast_SuccessfulUpstreamCall_Records200()
    {
        // Arrange
        var cache = new FusionCache(new FusionCacheOptions());
        using var history = new EdrStatusHistory(cache);
        var edrService = new Mock<IEdrService>();
        edrService.Setup(service => service.GetEdrForecast(It.IsAny<Collection<string>>()))
            .ReturnsAsync(new EdrForecastResult(HttpStatusCode.OK,
                [new HomeAssistantDTO { Name = "wind-speed", Description = "Wind", Data = [] }]));
        var applicationService = new EdrApplicationService(edrService.Object, cache, history, NullLogger<EdrApplicationService>.Instance);

        // Act
        await applicationService.GetEdrForecast("wind-speed");

        // Assert
        var result = await cache.TryGetAsync<List<int>>(EdrStatusHistory.CacheKey, token: TestContext.CancellationToken);
        Assert.AreSequenceEqual([(int)HttpStatusCode.OK], result.Value);
        cache.Dispose();
    }

    [TestMethod]
    public async Task GetEdrForecast_RecordsTheActualSuccessfulHttpStatus()
    {
        // Arrange
        var cache = new FusionCache(new FusionCacheOptions());
        using var history = new EdrStatusHistory(cache);
        var edrService = new Mock<IEdrService>();
        edrService.Setup(service => service.GetEdrForecast(It.IsAny<Collection<string>>()))
            .ReturnsAsync(new EdrForecastResult(HttpStatusCode.Created,
                [new HomeAssistantDTO { Name = "temperature-2m", Description = "Temperature", Data = [] }]));
        var applicationService = new EdrApplicationService(edrService.Object, cache, history, NullLogger<EdrApplicationService>.Instance);

        // Act
        await applicationService.GetEdrForecast("temperature-2m");

        // Assert
        var result = await cache.TryGetAsync<List<int>>(EdrStatusHistory.CacheKey, token: TestContext.CancellationToken);
        Assert.AreSequenceEqual([(int)HttpStatusCode.Created], result.Value);
        cache.Dispose();
    }

    [TestMethod]
    public async Task GetEdrForecast_TemperatureRequest_CachesTemperatureAndRelativeHumidity()
    {
        // Arrange
        var cache = new FusionCache(new FusionCacheOptions());
        using var history = new EdrStatusHistory(cache);
        var edrService = new Mock<IEdrService>();
        edrService.Setup(service => service.GetEdrForecast(It.IsAny<Collection<string>>()))
            .ReturnsAsync(new EdrForecastResult(HttpStatusCode.OK,
            [
                new HomeAssistantDTO { Name = "temperature-2m", Description = "Temperature", Data = [] },
                new HomeAssistantDTO { Name = "relative-humidity-2m", Description = "Relative humidity", Data = [] }
            ]));
        var applicationService = new EdrApplicationService(edrService.Object, cache, history, NullLogger<EdrApplicationService>.Instance);

        // Act
        await applicationService.GetEdrForecast("temperature-2m");

        // Assert
        var temperature = await cache.TryGetAsync<HomeAssistantDTO>("EDR-temperature-2m", token: TestContext.CancellationToken);
        var humidity = await cache.TryGetAsync<HomeAssistantDTO>("EDR-relative-humidity-2m", token: TestContext.CancellationToken);
        Assert.IsTrue(temperature.HasValue);
        Assert.AreEqual("temperature-2m", temperature.Value.Name);
        Assert.IsTrue(humidity.HasValue);
        Assert.AreEqual("relative-humidity-2m", humidity.Value.Name);
        edrService.Verify(service => service.GetEdrForecast(It.Is<Collection<string>>(parameters =>
            parameters.Contains("temperature-2m") && parameters.Contains("relative-humidity-2m"))), Times.Once);
        cache.Dispose();
    }

    [TestMethod]
    public async Task GetEdrForecast_HttpAndTransportFailures_RecordTheirStatuses()
    {
        // Arrange
        var cache = new FusionCache(new FusionCacheOptions());
        using var history = new EdrStatusHistory(cache);
        var edrService = new Mock<IEdrService>();
        edrService.SetupSequence(service => service.GetEdrForecast(It.IsAny<Collection<string>>()))
            .ThrowsAsync(new DmiUpstreamRateLimitException(null))
            .ThrowsAsync(new HttpRequestException("Upstream error", null, HttpStatusCode.InternalServerError))
            .ThrowsAsync(new HttpRequestException("No response"));
        var applicationService = new EdrApplicationService(edrService.Object, cache, history, NullLogger<EdrApplicationService>.Instance);

        // Act
        await InvokeAndIgnoreUpstreamError(() => applicationService.GetEdrForecast("rate-limited"));
        await InvokeAndIgnoreUpstreamError(() => applicationService.GetEdrForecast("server-error"));
        await InvokeAndIgnoreUpstreamError(() => applicationService.GetEdrForecast("transport-error"));

        // Assert
        var result = await cache.TryGetAsync<List<int>>(EdrStatusHistory.CacheKey, token: TestContext.CancellationToken);
        Assert.AreSequenceEqual([429, 500, 0], result.Value);
        cache.Dispose();
    }

    private static async Task InvokeAndIgnoreUpstreamError(Func<Task<HomeAssistantDTO>> request)
    {
        try
        {
            await request();
        }
        catch (HttpRequestException)
        {
            // FusionCache may return its configured fallback instead of rethrowing the factory exception.
        }
    }

    public TestContext TestContext { get; set; }
}
