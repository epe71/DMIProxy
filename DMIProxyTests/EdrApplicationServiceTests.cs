using Moq;
using Microsoft.Extensions.Logging;
using DMIProxy.ApplicationService;
using DMIProxy.Contract;
using DMIProxy.DomainService;

namespace DMIProxyTests;

[TestClass]
public class EdrApplicationServiceTests
{
    [TestMethod]
    public async Task GetEdrForecast_ReturnsCachedForecast_IfPresentInCache()
    {
        // Arrange
        var forecastParameter = "test-parameter";
        var cachedForecast = new HomeAssistantDTO { description = "Cached forecast" };

        var edrServiceMock = new Mock<IEdrService>();
        var ntfyServiceMock = new Mock<INtfyService>();
        var requestCacheMock = new Mock<IRequestCache>();
        var loggerMock = new Mock<ILogger<EdrApplicationService>>();

        requestCacheMock
            .Setup(x => x.GetEdrForecastDTO(forecastParameter, out cachedForecast))
            .Returns(true);

        var service = new EdrApplicationService(
            edrServiceMock.Object,
            ntfyServiceMock.Object,
            requestCacheMock.Object,
            loggerMock.Object);

        // Act
        var result = await service.GetEdrForecast(forecastParameter);

        // Assert
        Assert.AreEqual(cachedForecast, result);
    }

    [TestMethod]
    public async Task GetEdrForecast_NoCacheEntry_ReturnsStartingServiceDto()
    {
        // Arrange
        var forecastParameter = "test-parameter";
        HomeAssistantDTO? outForecast = null;

        var edrServiceMock = new Mock<IEdrService>();
        var ntfyServiceMock = new Mock<INtfyService>();
        var requestCacheMock = new Mock<IRequestCache>();
        var loggerMock = new Mock<ILogger<EdrApplicationService>>();

        // Simulate cache miss
        requestCacheMock
            .Setup(x => x.GetEdrForecastDTO(forecastParameter, out outForecast))
            .Returns(false);

        // Simulate GetEdrKeysToUpdate returns empty string
        requestCacheMock
            .Setup(x => x.GetEdrKeysToUpdate(forecastParameter))
            .Returns(string.Empty);

        var service = new EdrApplicationService(
            edrServiceMock.Object,
            ntfyServiceMock.Object,
            requestCacheMock.Object,
            loggerMock.Object);

        // Act
        var result = await service.GetEdrForecast(forecastParameter);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("Starting service, no forecast parameter ready.", result.description);
    }

    [TestMethod]
    public async Task GetEdrForecast_WhenGetEdrKeysToUpdateReturnsParameter_CallsServiceAndReturnsResult()
    {
        // Arrange
        var forecastParameter = "test-parameter";
        HomeAssistantDTO? outForecast = null;
        var fetchedForecast = new HomeAssistantDTO { description = "Fetched forecast" };

        var edrServiceMock = new Mock<IEdrService>();
        var ntfyServiceMock = new Mock<INtfyService>();
        var requestCacheMock = new Mock<IRequestCache>();
        var loggerMock = new Mock<ILogger<EdrApplicationService>>();

        // Simulate cache miss
        requestCacheMock
            .Setup(x => x.GetEdrForecastDTO(forecastParameter, out outForecast))
            .Returns(false);

        // Simulate GetEdrKeysToUpdate returns the parameter itself
        requestCacheMock
            .Setup(x => x.GetEdrKeysToUpdate(forecastParameter))
            .Returns(forecastParameter);

        // Simulate service returns a forecast
        edrServiceMock
            .Setup(x => x.GetEdrForecast(forecastParameter))
            .ReturnsAsync(fetchedForecast);

        // Simulate notification service
        ntfyServiceMock
            .Setup(x => x.SendNotification(It.IsAny<string>()))
            .ReturnsAsync(true);

        var service = new EdrApplicationService(
            edrServiceMock.Object,
            ntfyServiceMock.Object,
            requestCacheMock.Object,
            loggerMock.Object);

        // Act
        var result = await service.GetEdrForecast(forecastParameter);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("Fetched forecast", result.description);
        requestCacheMock.Verify(x => x.SaveEdrForecastDTO(forecastParameter, fetchedForecast), Times.Once);
        edrServiceMock.Verify(x => x.GetEdrForecast(forecastParameter), Times.Once);
    }
}