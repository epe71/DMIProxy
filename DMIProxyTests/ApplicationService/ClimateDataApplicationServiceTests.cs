using DMIProxy.ApplicationService;
using DMIProxy.BusinessEntity.MetObs;
using DMIProxy.DomainService;
using DMIProxyTests.Builder;
using Microsoft.Extensions.Logging;
using Moq;
using System.Globalization;
using System.Collections.ObjectModel;
using ZiggyCreatures.Caching.Fusion;

namespace DMIProxyTests.ApplicationService;

[TestClass]
public class ClimateDataApplicationServiceTests
{
    [TestMethod]
    public async Task GetHeatingDegreeDays_SunshineCase()
    {
        // Arrange
        var expectedData = new DmiMetObsData
        {
            Features =
            [
                new Feature
                {
                    Properties = new Properties
                    {
                        Validity = true,
                        NoValuesInCalculation = 24,
                        From = DateTime.Now,
                        Value = 1.0,
                    }
                }
            ],
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
        Assert.AreEqual(expectedData.Features.First().Properties.Value, result.Data.First().Value);
        Assert.AreEqual(expectedData.Features.First().Properties.From.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture), result.Data.First().Date);
        fusionCache.Dispose();
    }

    [TestMethod]
    public async Task GetAverageHeatingDegreeDays_ReturnsAveragedSmoothedData()
    {
        // Arrange
        var numberOfYears = 2;

        // Prepare features spanning two years for two day-of-year groups
        var features = new Collection<Feature>
        {
            new() { Properties = new Properties { From = new DateTime(2023,1,1), Value = 10 } },
            new() { Properties = new Properties { From = new DateTime(2024,1,1), Value = 14 } },
            new() { Properties = new Properties { From = new DateTime(2023,1,2), Value = 20 } },
            new() { Properties = new Properties { From = new DateTime(2024,1,2), Value = 22 } }
        };

        var observation = new DmiMetObsData
        {
            Features = features
        };

        var climateDataServiceMock = new Mock<IClimateDataService>();
        climateDataServiceMock
            .Setup(s => s.GetParameterId(IClimateDataService.ParameterId.acc_heating_degree_days_17, It.IsAny<int>()))
            .ReturnsAsync(observation);

        var timeSpanCalculator = new TimeSpanCalculator(new MockDateTimeProviderBuilder().Build());
        var fusionCache = new FusionCache(new FusionCacheOptions());
        var loggerMock = new Mock<ILogger<ClimateDataApplicationService>>();

        var service = new ClimateDataApplicationService(climateDataServiceMock.Object, timeSpanCalculator, fusionCache, loggerMock.Object);

        // Act
        var result = await service.GetAverageHeatingDegreeDays(numberOfYears);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("average accumulated heating degree days", result.Name);
        Assert.IsTrue(result.Description.Contains($"{numberOfYears} year average", StringComparison.OrdinalIgnoreCase));

        // There should be two grouped days (day 1 and day 2)
        Assert.HasCount(2, result.Data);

        // Compute expected grouped averages
        var thisYear = DateTime.Now.Year;
        var day1Date = new DateTime(thisYear, 1, 1).ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture);
        var day2Date = new DateTime(thisYear, 1, 2).ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture);

        // Grouping produces averages: day1 -> (10+14)/2 = 12.0, day2 -> (20+22)/2 = 21.0
        var avgDay1 = 12.0;
        var avgDay2 = 21.0;

        // The service orders by descending date, so first entry corresponds to day2
        // Apply the same SimpleMovingAverage logic (k=7) as in the service to compute expected smoothed values
        var sma = new SimpleMovingAverage(7);
        var expectedSmoothedDay2 = Math.Round(sma.Update(avgDay2), 1);
        var expectedSmoothedDay1 = Math.Round(sma.Update(avgDay1), 1);

        Assert.AreEqual(day2Date, result.Data[0].Date);
        Assert.AreEqual(expectedSmoothedDay2, result.Data[0].Value);

        Assert.AreEqual(day1Date, result.Data[1].Date);
        Assert.AreEqual(expectedSmoothedDay1, result.Data[1].Value);
        fusionCache.Dispose();
    }

    [TestMethod]
    public async Task GetAverageHeatingDegreeDays_EmptyFeatures_ReturnsEmptyDTO()
    {
        // Arrange
        var observation = new DmiMetObsData { Features = [] };
        var climateDataServiceMock = new Mock<IClimateDataService>();
        climateDataServiceMock
            .Setup(s => s.GetParameterId(IClimateDataService.ParameterId.acc_heating_degree_days_17, It.IsAny<int>()))
            .ReturnsAsync(observation);

        var timeSpanCalculator = new TimeSpanCalculator(new MockDateTimeProviderBuilder().Build());
        var fusionCache = new FusionCache(new FusionCacheOptions());
        var loggerMock = new Mock<ILogger<ClimateDataApplicationService>>();

        var service = new ClimateDataApplicationService(climateDataServiceMock.Object, timeSpanCalculator, fusionCache, loggerMock.Object);

        // Act
        var result = await service.GetAverageHeatingDegreeDays(1);

        // Assert
        Assert.IsNotNull(result);
        // When no features are returned the service returns a new HomeAssistantDTO with null properties
        Assert.IsNull(result.Data);
        Assert.IsNull(result.Name);
        Assert.IsNull(result.Description);
        fusionCache.Dispose();
    }

    [TestMethod]
    public async Task GetAverageHeatingDegreeDays_SingleYear_WorksAsExpected()
    {
        // Arrange
        var numberOfYears = 1;
        var features = new Collection<Feature>
        {
            new() { Properties = new Properties { From = new DateTime(2023,1,1), Value = 5 } },
            new() { Properties = new Properties { From = new DateTime(2023,1,2), Value = 7 } }
        };
        var observation = new DmiMetObsData { Features = features };

        var climateDataServiceMock = new Mock<IClimateDataService>();
        climateDataServiceMock
            .Setup(s => s.GetParameterId(IClimateDataService.ParameterId.acc_heating_degree_days_17, It.IsAny<int>()))
            .ReturnsAsync(observation);

        var timeSpanCalculator = new TimeSpanCalculator(new MockDateTimeProviderBuilder().Build());
        var fusionCache = new FusionCache(new FusionCacheOptions());
        var loggerMock = new Mock<ILogger<ClimateDataApplicationService>>();

        var service = new ClimateDataApplicationService(climateDataServiceMock.Object, timeSpanCalculator, fusionCache, loggerMock.Object);

        // Act
        var result = await service.GetAverageHeatingDegreeDays(numberOfYears);

        // Assert
        Assert.IsNotNull(result);
        Assert.HasCount(2, result.Data);

        var thisYear = DateTime.Now.Year;
        var day1Date = new DateTime(thisYear, 1, 1).ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture);
        var day2Date = new DateTime(thisYear, 1, 2).ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture);

        var avgDay1 = 5.0;
        var avgDay2 = 7.0;

        var sma = new SimpleMovingAverage(7);
        var expectedSmoothedDay2 = Math.Round(sma.Update(avgDay2), 1);
        var expectedSmoothedDay1 = Math.Round(sma.Update(avgDay1), 1);

        Assert.AreEqual(day2Date, result.Data[0].Date);
        Assert.AreEqual(expectedSmoothedDay2, result.Data[0].Value);
        Assert.AreEqual(day1Date, result.Data[1].Date);
        Assert.AreEqual(expectedSmoothedDay1, result.Data[1].Value);
        fusionCache.Dispose();
    }

    [TestMethod]
    public async Task GetAverageHeatingDegreeDays_Cache_PreventsDuplicateServiceCall()
    {
        // Arrange
        var numberOfYears = 1;
        var features = new Collection<Feature>
        {
            new() { Properties = new Properties { From = DateTime.Now, Value = 3 } }
        };
        var observation = new DmiMetObsData { Features = features };

        var climateDataServiceMock = new Mock<IClimateDataService>();
        climateDataServiceMock
            .Setup(s => s.GetParameterId(IClimateDataService.ParameterId.acc_heating_degree_days_17, It.IsAny<int>()))
            .ReturnsAsync(observation);

        var timeSpanCalculator = new TimeSpanCalculator(new MockDateTimeProviderBuilder().Build());
        var fusionCache = new FusionCache(new FusionCacheOptions());
        var loggerMock = new Mock<ILogger<ClimateDataApplicationService>>();

        var service = new ClimateDataApplicationService(climateDataServiceMock.Object, timeSpanCalculator, fusionCache, loggerMock.Object);

        // Act - call twice
        var first = await service.GetAverageHeatingDegreeDays(numberOfYears);
        var second = await service.GetAverageHeatingDegreeDays(numberOfYears);

        // Assert - underlying climate service should have been called only once due to caching
        climateDataServiceMock.Verify(s => s.GetParameterId(IClimateDataService.ParameterId.acc_heating_degree_days_17, It.IsAny<int>()), Times.Once);
        Assert.IsNotNull(first);
        Assert.IsNotNull(second);
        fusionCache.Dispose();
    }

    [TestMethod]
    public async Task GetAverageHeatingDegreeDays_WhenClimateDataServiceThrows_ExceptionPropagates()
    {
        // Arrange
        var climateDataServiceMock = new Mock<IClimateDataService>();
        climateDataServiceMock
            .Setup(s => s.GetParameterId(IClimateDataService.ParameterId.acc_heating_degree_days_17, It.IsAny<int>()))
            .ThrowsAsync(new InvalidOperationException("Service failure"));

        var timeSpanCalculator = new TimeSpanCalculator(new MockDateTimeProviderBuilder().Build());
        var fusionCache = new FusionCache(new FusionCacheOptions());
        var loggerMock = new Mock<ILogger<ClimateDataApplicationService>>();

        var service = new ClimateDataApplicationService(climateDataServiceMock.Object, timeSpanCalculator, fusionCache, loggerMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await service.GetAverageHeatingDegreeDays(1));
        fusionCache.Dispose();
    }

    [TestMethod]
    public async Task GetAverageHeatingDegreeDays_InvalidNumberOfYears_PropagatesArgumentOutOfRange()
    {
        // Arrange
        var climateDataServiceMock = new Mock<IClimateDataService>();
        // Domain service will throw if the requested limit is invalid (e.g., non-positive)
        climateDataServiceMock
            .Setup(s => s.GetParameterId(IClimateDataService.ParameterId.acc_heating_degree_days_17, It.Is<int>(n => n <= 0)))
            .ThrowsAsync(new ArgumentOutOfRangeException("limit"));

        var timeSpanCalculator = new TimeSpanCalculator(new MockDateTimeProviderBuilder().Build());
        var fusionCache = new FusionCache(new FusionCacheOptions());
        var loggerMock = new Mock<ILogger<ClimateDataApplicationService>>();

        var service = new ClimateDataApplicationService(climateDataServiceMock.Object, timeSpanCalculator, fusionCache, loggerMock.Object);

        // Act & Assert: passing 0 years should lead to ArgumentOutOfRangeException from the domain service
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () => await service.GetAverageHeatingDegreeDays(0));
        fusionCache.Dispose();
    }

    [TestMethod]
    public async Task GetAverageHeatingDegreeDays_NumberOfYearsExceedsLimit_PropagatesArgumentOutOfRange()
    {
        // Arrange
        var climateDataServiceMock = new Mock<IClimateDataService>();
        // Domain service will throw if the requested limit is invalid (e.g., greater than 20)
        climateDataServiceMock
            .Setup(s => s.GetParameterId(IClimateDataService.ParameterId.acc_heating_degree_days_17, It.Is<int>(n => n > 20)))
            .ThrowsAsync(new ArgumentOutOfRangeException("limit"));
        var timeSpanCalculator = new TimeSpanCalculator(new MockDateTimeProviderBuilder().Build());
        var fusionCache = new FusionCache(new FusionCacheOptions());
        var loggerMock = new Mock<ILogger<ClimateDataApplicationService>>();
        var service = new ClimateDataApplicationService(climateDataServiceMock.Object, timeSpanCalculator, fusionCache, loggerMock.Object);

        // Act & Assert: passing 21 years should lead to ArgumentOutOfRangeException from the domain service
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () => await service.GetAverageHeatingDegreeDays(21));
        fusionCache.Dispose();
    }

    [TestMethod]
    public async Task GetAverageHeatingDegreeDays_EmptyFeatures_LogsError()
    {
        // Arrange
        var observation = new DmiMetObsData { Features = [] };
        var climateDataServiceMock = new Mock<IClimateDataService>();
        climateDataServiceMock
            .Setup(s => s.GetParameterId(IClimateDataService.ParameterId.acc_heating_degree_days_17, It.IsAny<int>()))
            .ReturnsAsync(observation);
        var timeSpanCalculator = new TimeSpanCalculator(new MockDateTimeProviderBuilder().Build());
        var fusionCache = new FusionCache(new FusionCacheOptions());
        var loggerMock = new Mock<ILogger<ClimateDataApplicationService>>();
        var service = new ClimateDataApplicationService(climateDataServiceMock.Object, timeSpanCalculator, fusionCache, loggerMock.Object);

        // Act
        var result = await service.GetHeatingDegreeDays();

        // Assert
        loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v != null && (v.ToString() ?? string.Empty).Contains("No climate data returned for parameter = heatingDegreesDays")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
        fusionCache.Dispose();
    }

    [TestMethod]
    public async Task GetMeanTemperature()
    {
        // Arrange
        var expectedData = new DmiMetObsData
        {
            Features =
            [
                new Feature
                {
                    Properties = new Properties
                    {
                        Validity = true,
                        NoValuesInCalculation = 24,
                        From = DateTime.Now,
                        Value = 15.0,
                    }
                }
            ],
        };
        var climateDataServiceMock = new Mock<IClimateDataService>();
        climateDataServiceMock
            .Setup(s => s.GetParameterId(IClimateDataService.ParameterId.mean_temp, It.IsAny<int>()))
            .ReturnsAsync(expectedData);
        var timeSpanCalculator = new TimeSpanCalculator(new MockDateTimeProviderBuilder().Build());
        var fusionCache = new FusionCache(new FusionCacheOptions());
        var loggerMock = new Mock<ILogger<ClimateDataApplicationService>>();
        var service = new ClimateDataApplicationService(climateDataServiceMock.Object, timeSpanCalculator, fusionCache, loggerMock.Object);
        var stationId = "06039";

        // Act
        var result = await service.GetMeanTemperature(stationId);

        // Assert
        Assert.AreEqual(expectedData.Features.First().Properties.Value, result.Data.First().Value);
        Assert.AreEqual(expectedData.Features.First().Properties.From.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture), result.Data.First().Date);
        fusionCache.Dispose();
    }

    [TestMethod]
    public async Task GetMeanTemperature_EmptyFeatures_ReturnsEmptyDTO()
    {
        // Arrange
        var observation = new DmiMetObsData { Features = [] };
        var climateDataServiceMock = new Mock<IClimateDataService>();
        climateDataServiceMock
            .Setup(s => s.GetParameterId(IClimateDataService.ParameterId.mean_temp, It.IsAny<int>()))
            .ReturnsAsync(observation);
        var timeSpanCalculator = new TimeSpanCalculator(new MockDateTimeProviderBuilder().Build());
        var fusionCache = new FusionCache(new FusionCacheOptions());
        var loggerMock = new Mock<ILogger<ClimateDataApplicationService>>();
        var service = new ClimateDataApplicationService(climateDataServiceMock.Object, timeSpanCalculator, fusionCache, loggerMock.Object);
        var stationId = "06039";

        // Act
        var result = await service.GetMeanTemperature(stationId);

        // Assert
        Assert.IsNotNull(result);
        // When no features are returned the service returns a new HomeAssistantDTO with null properties
        Assert.IsNull(result.Data);
        Assert.IsNull(result.Name);
        Assert.IsNull(result.Description);
        fusionCache.Dispose();
    }

    [TestMethod]
    public async Task GetMeanTemperature_WhenClimateDataServiceThrows_ExceptionPropagates()
    {
        // Arrange
        var climateDataServiceMock = new Mock<IClimateDataService>();
        climateDataServiceMock
            .Setup(s => s.GetParameterId(IClimateDataService.ParameterId.mean_temp, It.IsAny<int>()))
            .ThrowsAsync(new InvalidOperationException("Service failure"));
        var timeSpanCalculator = new TimeSpanCalculator(new MockDateTimeProviderBuilder().Build());
        var fusionCache = new FusionCache(new FusionCacheOptions());
        var loggerMock = new Mock<ILogger<ClimateDataApplicationService>>();
        var service = new ClimateDataApplicationService(climateDataServiceMock.Object, timeSpanCalculator, fusionCache, loggerMock.Object);
        var stationId = "06039";

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await service.GetMeanTemperature(stationId));
        fusionCache.Dispose();
    }

    [TestMethod]
    public async Task GetMeanTemperature_Cache_PreventsDuplicateServiceCall()
    {
        // Arrange
        var expectedData = new DmiMetObsData
        {
            Features =
            [
                new Feature
                {
                    Properties = new Properties
                    {
                        Validity = true,
                        NoValuesInCalculation = 24,
                        From = DateTime.Now,
                        Value = 15.0,
                    }
                }
            ],
        };
        var climateDataServiceMock = new Mock<IClimateDataService>();
        climateDataServiceMock
            .Setup(s => s.GetParameterId(IClimateDataService.ParameterId.mean_temp, It.IsAny<int>()))
            .ReturnsAsync(expectedData);
        var timeSpanCalculator = new TimeSpanCalculator(new MockDateTimeProviderBuilder().Build());
        var fusionCache = new FusionCache(new FusionCacheOptions());
        var loggerMock = new Mock<ILogger<ClimateDataApplicationService>>();
        var service = new ClimateDataApplicationService(climateDataServiceMock.Object, timeSpanCalculator, fusionCache, loggerMock.Object);
        var stationId = "06039";

        // Act - call twice
        var first = await service.GetMeanTemperature(stationId);
        var second = await service.GetMeanTemperature(stationId);

        // Assert - underlying climate service should have been called only once due to caching
        climateDataServiceMock.Verify(s => s.GetParameterId(IClimateDataService.ParameterId.mean_temp, It.IsAny<int>()), Times.Once);
        Assert.IsNotNull(first);
        Assert.IsNotNull(second);
        fusionCache.Dispose();
    }

    [TestMethod]
    public async Task GetMeanTemperature_EmptyFeatures_LogsError()
    {
        // Arrange
        var observation = new DmiMetObsData { Features = [] };
        var climateDataServiceMock = new Mock<IClimateDataService>();
        climateDataServiceMock
            .Setup(s => s.GetParameterId(IClimateDataService.ParameterId.mean_temp, It.IsAny<int>()))
            .ReturnsAsync(observation);
        var timeSpanCalculator = new TimeSpanCalculator(new MockDateTimeProviderBuilder().Build());
        var fusionCache = new FusionCache(new FusionCacheOptions());
        var loggerMock = new Mock<ILogger<ClimateDataApplicationService>>();
        var service = new ClimateDataApplicationService(climateDataServiceMock.Object, timeSpanCalculator, fusionCache, loggerMock.Object);
        var stationId = "06039";

        // Act
        var result = await service.GetMeanTemperature(stationId);

        // Assert
        loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v != null && (v.ToString() ?? string.Empty).Contains("No climate data returned for station =")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
        fusionCache.Dispose();
    }

    [TestMethod]
    public async Task GetMeanTemperature_InvalidStationId_PropagatesArgumentException()
    {
        // Arrange
        var climateDataServiceMock = new Mock<IClimateDataService>();
        climateDataServiceMock
            .Setup(s => s.GetParameterId(IClimateDataService.ParameterId.mean_temp, It.IsAny<int>()))
            .ThrowsAsync(new ArgumentException("Invalid station ID"));
        var timeSpanCalculator = new TimeSpanCalculator(new MockDateTimeProviderBuilder().Build());
        var fusionCache = new FusionCache(new FusionCacheOptions());
        var loggerMock = new Mock<ILogger<ClimateDataApplicationService>>();
        var service = new ClimateDataApplicationService(climateDataServiceMock.Object, timeSpanCalculator, fusionCache, loggerMock.Object);
        var invalidStationId = "invalid";

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () => await service.GetMeanTemperature(invalidStationId));
        fusionCache.Dispose();
    }
}