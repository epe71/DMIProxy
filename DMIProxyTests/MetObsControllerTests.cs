using DMIProxy.ApplicationService;
using DMIProxy.Contract;
using DMIProxy.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace DMIProxyTests
{
    [TestClass]
    public class MetObsControllerTests
    {
        [TestMethod]
        public async Task GetRain_ReturnsExpectedRainDTO()
        {
            // Arrange
            var expectedRainDTO = new RainDTO
            {
                Rain1h = 0.5,
                RainToday = 5.2,
                RainThisMonth = 42.0,
                TimeStamp = DateTime.UtcNow,
                NumberReturned = 1
            };

            var metObsServiceMock = new Mock<IMetObsApplicationService>();
            metObsServiceMock
                .Setup(s => s.GetRain(It.IsAny<string>()))
                .ReturnsAsync(expectedRainDTO);

            var loggerMock = new Mock<ILogger<MetObsController>>();
            var edrServiceMock = new Mock<IEdrApplicationService>();
            var weatherForecastServiceMock = new Mock<IWeatherForecastService>();

            var controller = new MetObsController(
                loggerMock.Object,
                metObsServiceMock.Object,
                edrServiceMock.Object,
                weatherForecastServiceMock.Object);

            // Act
            var result = await controller.GetRain("06072");

            // Assert
            Assert.IsInstanceOfType(result, typeof(JsonResult));
            var jsonResult = result as JsonResult;
            Assert.IsNotNull(jsonResult);

            var actualRainDTO = jsonResult.Value as RainDTO;
            Assert.IsNotNull(actualRainDTO);
            Assert.AreEqual(expectedRainDTO.Rain1h, actualRainDTO.Rain1h);
            Assert.AreEqual(expectedRainDTO.RainToday, actualRainDTO.RainToday);
            Assert.AreEqual(expectedRainDTO.RainThisMonth, actualRainDTO.RainThisMonth);
            Assert.AreEqual(expectedRainDTO.NumberReturned, actualRainDTO.NumberReturned);
            Assert.AreEqual(expectedRainDTO.TimeStamp, actualRainDTO.TimeStamp);
        }


        [TestMethod]
        public async Task GetEdrForecast_ReturnsExpectedForecastDTO()
        {
            // Arrange
            var expectedForecastDTO = new HomeAssistantDTO
            {
                data = new List<PointDTO>
                {
                    new PointDTO { date = "2025-05-24 17:17:45", value = 20.5 },
                },
                description = "Test forecast description"
            };

            var edrServiceMock = new Mock<IEdrApplicationService>();
            edrServiceMock
                .Setup(s => s.GetEdrForecast(It.IsAny<string>()))
                .ReturnsAsync(expectedForecastDTO);

            var loggerMock = new Mock<ILogger<MetObsController>>();
            var metObsServiceMock = new Mock<IMetObsApplicationService>();
            var weatherForecastServiceMock = new Mock<IWeatherForecastService>();

            var controller = new MetObsController(
                loggerMock.Object,
                metObsServiceMock.Object,
                edrServiceMock.Object,
                weatherForecastServiceMock.Object);

            // Act
            var result = await controller.GetEdrForecast("sampleParameter");

            // Assert
            Assert.IsInstanceOfType(result, typeof(JsonResult));
            var jsonResult = result as JsonResult;
            Assert.IsNotNull(jsonResult);
            var actualForecastDTO = jsonResult.Value as HomeAssistantDTO;
            Assert.IsNotNull(actualForecastDTO);
            Assert.AreEqual(expectedForecastDTO.description, actualForecastDTO.description);
            Assert.AreEqual(expectedForecastDTO.data.Count, actualForecastDTO.data.Count);
        }


        [TestMethod]
        public async Task GetWeatherForecast_ReturnsExpectedForecast()
        {
            // Arrange
            var expectedForecast = new ForecastMessageDTO
            {
                Time = DateTime.UtcNow,
                Headline = "Sunny",
                Message = "Clear skies and warm temperatures."
            };

            var weatherForecastServiceMock = new Mock<IWeatherForecastService>();
            weatherForecastServiceMock
                .Setup(s => s.GetWeatherForecast(It.IsAny<string>()))
                .ReturnsAsync(expectedForecast);

            var loggerMock = new Mock<ILogger<MetObsController>>();
            var metObsServiceMock = new Mock<IMetObsApplicationService>();
            var edrServiceMock = new Mock<IEdrApplicationService>();

            var controller = new MetObsController(
                loggerMock.Object,
                metObsServiceMock.Object,
                edrServiceMock.Object,
                weatherForecastServiceMock.Object);

            // Act
            var result = await controller.GetWeatherForecast("station123");

            // Assert
            Assert.IsInstanceOfType(result, typeof(JsonResult));
            var jsonResult = result as JsonResult;
            Assert.IsNotNull(jsonResult);
            var forecast = jsonResult.Value as ForecastMessageDTO;
            Assert.IsNotNull(forecast);
            Assert.AreEqual(expectedForecast.Headline, forecast.Headline);
            Assert.AreEqual(expectedForecast.Message, forecast.Message);
            Assert.AreEqual(expectedForecast.Time, forecast.Time);
        }
    }
}