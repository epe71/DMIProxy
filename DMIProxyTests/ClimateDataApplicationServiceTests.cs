using DMIProxy.ApplicationService;
using DMIProxy.BusinessEntity.MetObs;
using DMIProxy.DomainService;
using DMIProxyTests.Builder;
using Microsoft.Extensions.Logging;
using Moq;
using ZiggyCreatures.Caching.Fusion;

namespace DMIProxyTests;

[TestClass]
public class ClimateDataApplicationServiceTests
{
    [TestMethod]
    public async Task GetHeatingDegreeDays_SunshineCase()
    {
        // Arrange
        var expectedData = new DmiMetObsData
        {
            features = new List<Feature>
            {
                new Feature
                {
                    properties = new Properties
                    {
                        validity = true,
                        noValuesInCalculation = 24,
                        from = DateTime.Now,
                        value = 1.0,
                    }
                }
            },
        };
        var climateDataServiceMock = new Mock<IClimateDataService>();
        climateDataServiceMock
            .Setup(s => s.GetParameterId(IClimateDataService.ParameterId.acc_heating_degree_days_17, It.IsAny<int>()))
            .ReturnsAsync(expectedData);
        var timeSpanCalculator = new TimeSpanCalculator(new MockDateTimeProviderBuilder().Build());
        var fusionCache = new FusionCache(new FusionCacheOptions());
        var loggerMock = new Mock<ILogger<ClimateDataApplicationService>>();

        var service = new ClimateDataApplicationService(climateDataServiceMock.Object, timeSpanCalculator, fusionCache, loggerMock.Object);

        // Act
        var result = await service.GetHeatingDegreeDays();

        // Assert
        Assert.AreEqual(expectedData.features.First().properties.value, result.data.First().value);
        Assert.AreEqual(expectedData.features.First().properties.from.ToString("yyyy-MM-ddTHH:mm:ss"), result.data.First().date);
    }
}
