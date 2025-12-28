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
    private const string cacheKeyHeatingDegreeDaysAverage = "HeatingDegreeDaysAverage";
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
            observation = await climateDataService.GetParameterId(ParameterId.acc_heating_degree_days_17, 365);
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
            name = "Accumulated heating degree days",
            description = "Accumulated heating degree days with base 17°C for Denmark",
            data = dataPoints
        };

        logger.LogInformation("New ClimateData (heating degrees days) save in cache");
        requestCache.SaveClimateDataDTO("Denmark", cacheKeyHeatingDegreeDays, homeAssistantDTO);
        return homeAssistantDTO;
    }

    public async Task<HomeAssistantDTO> GetAverageHeatingDegreeDays(int numberOfYears)
    {
        var cacheKey = $"{cacheKeyHeatingDegreeDaysAverage}_{numberOfYears}";
        if (requestCache.GetClimateDataDTO("Denmark", cacheKey, out HomeAssistantDTO? cachedValue))
        {
            return cachedValue ?? throw new InvalidOperationException($"ClimateData for heatingDegreesDaysAverage could not be retrieved.");
        }

        DmiMetObsData observation;
        try
        {
            observation = await climateDataService.GetParameterId(ParameterId.acc_heating_degree_days_17, 365 * numberOfYears);
        }
        catch (Polly.CircuitBreaker.BrokenCircuitException ex)
        {
            logger.LogError("Polly Circuit Breaker error: {Message}", ex.Message);
            return new HomeAssistantDTO { description = ex.Message };
        }

        if (observation.features.Count == 0)
        {
            logger.LogError("No climate data returned for parameter = heatingDegreesDaysAverage");
            return new HomeAssistantDTO();
        }

        int thisYear = DateTime.Now.Year;
        var dataPoints = observation.features.GroupBy(f => f.properties.from.DayOfYear)
            .Select(g => new PointDTO
            {
                date = new DateTime(thisYear, 1, 1).AddDays(g.Key - 1).ToString("yyyy-MM-ddTHH:mm:ss"),
                value = Math.Round(g.Average(f => f.properties.value), 1)
            })
            .OrderByDescending(x => x.date)
            .ToList();

        var calculator = new SimpleMovingAverage(k: 7);
        var updatedDataPoints = new List<PointDTO>();
        foreach (PointDTO point in dataPoints)
        {
            var sma = calculator.Update(point.value);
            updatedDataPoints.Add(new PointDTO
            {
                date = point.date,
                value = Math.Round(sma, 1)
            });
        }

        var homeAssistantDTO = new HomeAssistantDTO
        {
            name = "average accumulated heating degree days",
            description = $"{numberOfYears} year average of accumulated heating degree days with base 17°C",
            data = updatedDataPoints
        };

        logger.LogInformation("New ClimateData (heating degrees days average) save in cache");
        requestCache.SaveClimateDataDTO("Denmark", cacheKey, homeAssistantDTO);
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
            observation = await climateDataService.GetParameterId(ParameterId.mean_temp, 100);
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
