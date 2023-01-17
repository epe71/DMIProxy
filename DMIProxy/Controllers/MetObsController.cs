using DMIProxy.Contract;
using DMIProxy.ApplicationService;
using Microsoft.AspNetCore.Mvc;

namespace DMIProxy.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class MetObsController : ControllerBase
    {
        private readonly ILogger<MetObsController> _logger;
        private readonly IMetObsApplicationService _applicationService;

        public MetObsController(
            ILogger<MetObsController> logger,
            IMetObsApplicationService nordPoolApplicationService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _applicationService = nordPoolApplicationService;
        }


        /// <summary>
        /// Get rain mesaurement from the last hour, day and month
        /// </summary>
        /// <param name="stationId" example="06072">id of the station to get information for</param>
        /// <returns></returns>
        [HttpGet(Name = "Rain/{stationId}")]
        public async Task<IActionResult> GetAsync(string stationId)
        {
            var statisticsDTO = await _applicationService.GetRain(stationId);
            return new JsonResult(statisticsDTO);
        }
    }
}
