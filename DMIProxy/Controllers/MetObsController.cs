using DMIProxy.ApplicationService;
using Microsoft.AspNetCore.Mvc;

namespace DMIProxy.Controllers;

[ApiController]
[Route("[controller]")]
public class MetObsController(
        ILogger<MetObsController> logger,
        IMetObsApplicationService metObsApplicationService,
        IEdrApplicationService edrApplicationService,
        IWeatherForecastService weatherForecastService,
        IClimateDataApplicationService climateDataApplicationService) : ControllerBase
{

    /// <summary>
    /// Get rain mesaurement from the last hour, day and month
    /// </summary>
    /// <param name="stationId" example="06072">id of the station to get information for</param>
    /// <returns></returns>
    [HttpGet("Rain/{stationId}/")]
    public async Task<IActionResult> GetRain(string stationId)
    {
        var statisticsDTO = await metObsApplicationService.GetRain(stationId);
        logger.LogDebug($"Rain this month {statisticsDTO.RainThisMonth} at {stationId}");
        return new JsonResult(statisticsDTO);
    }

    /// <summary>
    /// Get forecast from DMI Open Data service via EDR api
    /// </summary>
    /// <param name="forecastParameter">The parameter to get the forecast for.</param>
    /// <returns>A JSON result containing the forecast data i Home Assistant format.</returns>
    [HttpGet("EDR/{forecastParameter}")]
    public async Task<IActionResult> GetEdrForecast(string forecastParameter)
    {
        var forecastDTO = await edrApplicationService.GetEdrForecast(forecastParameter);
        return new JsonResult(forecastDTO);
    }

    /// <summary>
    /// Get the current weather forecast for Aarhus
    /// </summary>
    /// <param name="stationId" example="2624652">The station id to get the weather forecast for</param>
    /// <returns>A Danish text with the wheather forecast for today</returns>
    [HttpGet("WeatherForecast/{stationId}")]
    public async Task<IActionResult> GetWeatherForecast(string stationId)
    {
        var forecastDTO = await weatherForecastService.GetWeatherForecast(stationId);
        return new JsonResult(forecastDTO);
    }

    /// <summary>
    /// Retrieves the heating degree days data for Denmark.
    /// </summary>
    /// <returns>An <see cref="IActionResult"/> containing the heating degree days data for the specified station in JSON format.</returns>
    [HttpGet("ClimateData/HeatingDegreeDays")]
    public async Task<IActionResult> GetHeatingDegreeDays()
    {
        var heatingDegreeDaysDTO = await climateDataApplicationService.GetHeatingDegreeDays();
        return new JsonResult(heatingDegreeDaysDTO);
    }
}