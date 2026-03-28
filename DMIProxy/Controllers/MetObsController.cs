using DMIProxy.ApplicationService;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace DMIProxy.Controllers;

/// <summary>
/// Controller for retrieving meteorological observation data, weather forecasts and climate data from DMI Open Data service. 
/// The controller provides endpoints for fetching rain measurements, EDR forecasts, weather forecasts, and heating degree days data. 
/// </summary>
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
    /// Get rain mesaurement from the last hour, day and month for a given [stationId](https://www.dmi.dk/friedata/dokumentation/data/meteorological-observation-data-stations) 
    /// from DMI Open Data service via Meteorological Observation API.
    /// </summary>
    /// <param name="stationId" example="06072">
    /// id of the station to get information for.
    /// </param>
    /// <returns>
    /// An <see cref="IActionResult"/> containing rain statistics (1 hour, day, month) in JSON format.
    /// </returns>
    [HttpGet("Rain/{stationId}/")]
    public async Task<IActionResult> GetRain([RegularExpression(@"^\d{5}$")] string stationId)
    {
        var statisticsDTO = await metObsApplicationService.GetRain(stationId);
        logger.LogDebug($"Rain this month {statisticsDTO.RainThisMonth} at {stationId}");
        return new JsonResult(statisticsDTO);
    }

    /// <summary>
    /// Get forecast from DMI Open Data service via EDR api
    /// </summary>
    /// <param name="forecastParameter" example="total-precipitation">
    /// The parameter to get the forecast for. It is hardcode for the harmonie_dini_sf model and for city of Aarhus
    /// </param>
    /// <returns>An <see cref="IActionResult"/> containing the forecast data i Home Assistant format.</returns>
    [HttpGet("EDR/{forecastParameter}")]
    public async Task<IActionResult> GetEdrForecast([RegularExpression(@"^[a-z0-9-]+$")] string forecastParameter)
    {
        var forecastDTO = await edrApplicationService.GetEdrForecast(forecastParameter);
        return new JsonResult(forecastDTO);
    }

    /// <summary>
    /// Get the current weather forecast for Aarhus
    /// </summary>
    /// <param name="stationId" example="2624652">The station id to get the weather forecast for</param>
    /// <returns>An <see cref="IActionResult"/> with a danish text with the wheather forecast for today</returns>
    [HttpGet("WeatherForecast/{stationId}")]
    public async Task<IActionResult> GetWeatherForecast([RegularExpression(@"^\d{7}$")] string stationId)
    {
        var forecastDTO = await weatherForecastService.GetWeatherForecast(stationId);
        return new JsonResult(forecastDTO);
    }

    /// <summary>
    /// Retrieves the heating degree days data for Denmark.
    /// </summary>
    /// <returns>
    /// An <see cref="IActionResult"/> containing the heating degree days data for the specified station in JSON format.
    /// </returns>
    [HttpGet("ClimateData/HeatingDegreeDays")]
    public async Task<IActionResult> GetHeatingDegreeDays()
    {
        var heatingDegreeDaysDTO = await climateDataApplicationService.GetHeatingDegreeDays();
        return new JsonResult(heatingDegreeDaysDTO);
    }

    /// <summary>
    /// Retrieves the average heating degree days data for Denmark.
    /// </summary>
    /// <param name="numberOfYears" example="10">
    /// The number of years to use in average. Must be between 1 and 20 (inclusive)
    /// </param>
    /// <returns>
    /// An <see cref="IActionResult"/> containing the heating degree days data for the specified station in JSON format.
    /// </returns>
    [HttpGet("ClimateData/AverageHeatingDegreeDays/{numberOfYears:int}")]
    public async Task<IActionResult> GetAverageHeatingDegreeDays([FromRoute][Range(1, 20)] int numberOfYears)
    {
        var heatingDegreeDaysDTO = await climateDataApplicationService.GetAverageHeatingDegreeDays(numberOfYears);
        return new JsonResult(heatingDegreeDaysDTO);
    }

}