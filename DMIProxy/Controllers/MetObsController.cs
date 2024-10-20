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

        public MetObsController(
            ILogger<MetObsController> logger,
            IMetObsApplicationService nordPoolApplicationService,
            IEdrApplicationService edrApplicationService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _metObsApplicationService = nordPoolApplicationService;
            _edrApplicationService = edrApplicationService;
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
        /// Get 2 day forcast
        /// </summary>
        /// <returns></returns>
        [HttpGet("Forcast/")]
        public async Task<IActionResult> GetForcast()
        {
            var forcastDTO = await _edrApplicationService.GetForcast();
            return new JsonResult(forcastDTO);
        }

        /// <summary>
        /// Cloud transmittance forcast
        /// </summary>
        /// <returns></returns>
        [HttpGet("Forcast/Cloud")]
        public async Task<IActionResult> GetCloudForcast()
        {
            var forcastDTO = await _edrApplicationService.GetCloudForcast();
            return new JsonResult(forcastDTO);
        }

    }
}
