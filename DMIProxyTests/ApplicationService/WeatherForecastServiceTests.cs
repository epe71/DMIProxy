using DMIProxy.BusinessEntity;
using DMIProxy.DomainService;
using Moq;
using ZiggyCreatures.Caching.Fusion;
using ZiggyCreatures.Caching.Fusion.NullObjects;


namespace DMIProxy.ApplicationService.UnitTests;

[TestClass]
public class WeatherForecastServiceTests
{
    [TestMethod]
    public async Task GetWeatherForecast_ShouldReturnTrimmedForecast()
    {
        // Arrange
        var stationId = "12345";
        var forecast = new TextForecast
        {
            TimeStamp = DateTime.Now,
            Headline = "Test Forecast",
            Forecast = "Temp. 20°C. I aften bliver det klart."
        };
        var webScrapeServiceMock = new Mock<IWebScrapeService>();
        webScrapeServiceMock.Setup(s => s.GetWeatherForecast(stationId)).ReturnsAsync(forecast);

        var timeSpanCalculatorMock = new Mock<ITimeSpanCalculator>();
        timeSpanCalculatorMock.Setup(t => t.FixTime(It.IsAny<List<TimeOnly>>())).Returns(TimeSpan.FromHours(1));

        var nullCache = new NullFusionCache(new FusionCacheOptions());

        var service = new WeatherForecastService(webScrapeServiceMock.Object, timeSpanCalculatorMock.Object, nullCache);

        // Act
        var result = await service.GetWeatherForecast(stationId);

        // Assert
        Assert.AreEqual(forecast.TimeStamp, result.Time);
        Assert.AreEqual(forecast.Headline, result.Headline);
        Assert.AreEqual("Temperatur 20°C. ", result.Message);
    }
}