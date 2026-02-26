using DMIProxy.BusinessEntity.MetObs;
using DMIProxy.Contract;
using DMIProxy.DomainService;
using Moq;
using ZiggyCreatures.Caching.Fusion;
using ZiggyCreatures.Caching.Fusion.NullObjects;

namespace DMIProxy.ApplicationService.UnitTests;

[TestClass]
public class MetObsApplicationServiceTests
{
    [TestMethod]
    public async Task GetRain_ShouldReturnRainDTO()
    {
        // Arrange
        var stationId = "12345";
        var metObsData = new DmiMetObsData
        {
            timeStamp = DateTime.Now,
            features = new List<Feature>
            {
                new Feature()
                {
                    properties = new Properties
                    {
                        parameterId = "precip_past1h",
                        observed = DateTime.Now,
                        value = 2
                    }
                }
            },
            numberReturned = 1
        };
        
        var expectedRainDto = new RainDTO()
        {
            Rain1h = metObsData.Rain1h(),
            RainToday = metObsData.RainToday(),
            RainThisMonth = metObsData.RainThisMonth(),
            TimeStamp = metObsData.timeStamp,
            NumberReturned = metObsData.numberReturned
        };

        var metObsServiceMock = new Mock<IMetObsService>();
        metObsServiceMock.Setup(s => s.GetRain(stationId)).ReturnsAsync(metObsData);

        var timeSpanCalculatorMock = new Mock<ITimeSpanCalculator>();
        timeSpanCalculatorMock.Setup(t => t.AtTheTopOfTheHour(3)).Returns(TimeSpan.FromHours(3));

        var nullCache = new NullFusionCache(new FusionCacheOptions());
        var service = new MetObsApplicationService(metObsServiceMock.Object, timeSpanCalculatorMock.Object, nullCache);

        // Act
        var result = await service.GetRain(stationId);

        // Assert
        Assert.AreEqual(expectedRainDto.Rain1h, result.Rain1h);
        Assert.AreEqual(expectedRainDto.RainToday, result.RainToday);
        Assert.AreEqual(expectedRainDto.RainThisMonth, result.RainThisMonth);
        Assert.AreEqual(expectedRainDto.TimeStamp, result.TimeStamp);
        Assert.AreEqual(expectedRainDto.NumberReturned, result.NumberReturned);
    }

    [TestMethod]
    public async Task GetRain_GetNoData_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var stationId = "12345";
        
        var metObsServiceMock = new Mock<IMetObsService>();
        var timeSpanCalculatorMock = new Mock<ITimeSpanCalculator>();
        timeSpanCalculatorMock.Setup(t => t.AtTheTopOfTheHour(3)).Returns(TimeSpan.FromHours(3));

        var nullCache = new NullFusionCache(new FusionCacheOptions());
        var service = new MetObsApplicationService(metObsServiceMock.Object, timeSpanCalculatorMock.Object, nullCache);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await service.GetRain(stationId)
        );
        Assert.AreEqual("Failed to retrieve rain data.", exception.Message);
    }
}