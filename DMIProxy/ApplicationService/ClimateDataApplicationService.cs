using DMIProxy.BusinessEntity.MetObs;
using DMIProxy.Contract;
using DMIProxy.DomainService;
using static DMIProxy.DomainService.IClimateDataService;

namespace DMIProxy.ApplicationService;

public class ClimateDataApplicationService(
    IClimateDataService climateDataService,
    IRequestCache requestCache,
    ILogger<ClimateDataApplicationService> logger) : IClimateDataApplicationService
{
    private const string cacheKeyHeatingDegreeDays = "HeatingDegreeDays";
    private const string cacheKeyMeanTemp = "MeanTemp";

    public async Task<HomeAssistantDTO> GetHeatingDegreeDays()
    {
        // Try to get from cache first
        if (requestCache.GetClimateDataDTO("Denmark", cacheKeyHeatingDegreeDays, out HomeAssistantDTO? cachedValue))
        {
            return cachedValue ?? throw new InvalidOperationException($"ClimateData for heatingDegreesDays could not be retrieved.");
        }

        DmiMetObsData observation;
        try
        {
            observation = await climateDataService.GetParameterId(ParameterId.acc_heating_degree_days_17);
        }
        catch (Polly.CircuitBreaker.BrokenCircuitException ex)
        {
            logger.LogError("Polly Circuit Breaker error: {Message}", ex.Message);
            return new HomeAssistantDTO { description = ex.Message };
        }

        if (observation.features.Count == 0)
        {
            logger.LogError("No climate data returned for parameter = heatingDegreesDays");
            return new HomeAssistantDTO();
        }

        var dataPoints = observation.features
            .Select(f => new PointDTO
            {
                date = f.properties.from.ToString("yyyy-MM-ddTHH:mm:ss"),
                value = f.properties.value
            })
            .ToList();
        var homeAssistantDTO = new HomeAssistantDTO
        {
            name = "Accumulated heating degree days with base 17°C",
            description = $"Station id: {observation.features.First().properties.stationId}",
            data = dataPoints
        };

        logger.LogInformation("New ClimateData (heating degrees days) save in cache");
        requestCache.SaveClimateDataDTO("Denmark", cacheKeyHeatingDegreeDays, homeAssistantDTO);
        return homeAssistantDTO;
    }

    public async Task<HomeAssistantDTO> GetMeanTemperature(string stationId)
    {
        // Try to get from cache first
        if (requestCache.GetClimateDataDTO(stationId, cacheKeyMeanTemp, out HomeAssistantDTO? cachedValue))
        {
            return cachedValue ?? throw new InvalidOperationException($"ClimateData for mean temperature could not be retrieved. StationId={stationId}");
        }

        DmiMetObsData observation;
        try
        { 
            observation = await climateDataService.GetParameterId(ParameterId.mean_temp);
        }
        catch (Polly.CircuitBreaker.BrokenCircuitException ex)
        {
            logger.LogError("Polly Circuit Breaker error: {Message}", ex.Message);
            return new HomeAssistantDTO { description = ex.Message };
        }

        if (observation.features.Count == 0)
        {
            logger.LogError("No climate data returned for station = {StationId} and parameter = meanTemperatur", stationId);
            return new HomeAssistantDTO();
        }

        var dataPoints = observation.features
            .Where(f => f.properties.validity == true)
            .Select(f => new PointDTO
            {
                date = f.properties.from.ToString("yyyy-MM-ddTHH:mm:ss"),
                value = f.properties.value
            })
            .ToList();
        var homeAssistantDTO = new HomeAssistantDTO
        {
            name = "Mean temperature",
            description = $"Station id: {observation.features.First().properties.stationId}",
            data = dataPoints
        };

        logger.LogInformation("New ClimateData (mean temperatur) for station {StationId} save in cache", stationId);
        requestCache.SaveClimateDataDTO(stationId, cacheKeyMeanTemp, homeAssistantDTO);

        return homeAssistantDTO;
    }
}
