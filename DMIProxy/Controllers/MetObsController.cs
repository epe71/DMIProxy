using DMIProxy.ApplicationService;
using Microsoft.AspNetCore.Mvc;

namespace DMIProxy.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class MetObsController : ControllerBase
    {
        private readonly ILogger<MetObsController> _logger;
        private readonly IMetObsApplicationService _metObsApplicationService;
        private readonly IEdrApplicationService _edrApplicationService;
        private readonly IWeatherForcastService _weatherForcastService;

        public MetObsController(
            ILogger<MetObsController> logger,
            IMetObsApplicationService nordPoolApplicationService,
            IEdrApplicationService edrApplicationService,
            IWeatherForcastService weatherForcastService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _metObsApplicationService = nordPoolApplicationService;
            _edrApplicationService = edrApplicationService;
            _weatherForcastService = weatherForcastService;
        }

        /// <summary>
        /// Get rain mesaurement from the last hour, day and month
        /// </summary>
        /// <param name="stationId" example="06072">id of the station to get information for</param>
        /// <returns></returns>
        [HttpGet("Rain/{stationId}/")]
        public async Task<IActionResult> GetRain(string stationId)
        {
            var statisticsDTO = await _metObsApplicationService.GetRain(stationId);
            _logger.LogDebug($"Rain this month {statisticsDTO.RainThisMonth} at {stationId}");
            return new JsonResult(statisticsDTO);
        }

        /// <summary>
        /// Get forcast from DMI Open Data service via EDR api
        /// </summary>
        /// <param name="forcastParameter">The parameter to get the forecast for.</param>
        /// <returns>A JSON result containing the forecast data i Home Assistant format.</returns>
        [HttpGet("EDR/{forcastParameter}")]
        public async Task<IActionResult> GetEdrForcast(string forcastParameter)
        {
            var forcastDTO = await _edrApplicationService.GetEdrForcast(forcastParameter);
            return new JsonResult(forcastDTO);
        }

        /// <summary>
        /// Get the current weather forcast for Aarhus
        /// </summary>
        /// <param name="stationId" example="2624652">The station id to get the weather forcast for</param>
        /// <returns>A Danish text with the wheather forcast for today</returns>
        [HttpGet("WeatherForcast/{stationId}")]
        public async Task<IActionResult> GetWeatherForcast(string stationId)
        {
            var forcastDTO = await _weatherForcastService.GetWeatherForcast(stationId);
            return new JsonResult(forcastDTO);
        }
    }
}
