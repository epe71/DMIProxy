using DMIProxy.Contract;
using DMIProxy.DomainService;
using System.Collections.ObjectModel;
using System.Globalization;
using ZiggyCreatures.Caching.Fusion;
using static DMIProxy.DomainService.IClimateDataService;

namespace DMIProxy.ApplicationService;

public class ClimateDataApplicationService(
    IClimateDataService climateDataService,
    ITimeSpanCalculator timeSpanCalculator,
    IFusionCache cache,
    ILogger<ClimateDataApplicationService> logger) : IClimateDataApplicationService
{
    private const string cacheKeyHeatingDegreeDays = "HeatingDegreeDays";
    private const string cacheKeyHeatingDegreeDaysAverage = "HeatingDegreeDaysAverage";
    private const string cacheKeyMeanTemp = "MeanTemp";

    public async Task<HomeAssistantDTO> GetHeatingDegreeDays()
    {
        var expirationTime = timeSpanCalculator.FixTime([new TimeOnly(12, 10)]);
        var observation = await cache.GetOrSetAsync<HomeAssistantDTO>(
            cacheKeyHeatingDegreeDays,
            async (_, _) => await GetHeatingDegreeDays_NoCache(),
            options => options.SetDuration(expirationTime)
        );

        return observation;
    }

    private async Task<HomeAssistantDTO> GetHeatingDegreeDays_NoCache()
    {
        var observation = await climateDataService.GetParameterId(ParameterId.acc_heating_degree_days_17, 365);
        if (observation.Features.Count == 0)
        {
            logger.LogError("No climate data returned for parameter = heatingDegreesDays");
            return new HomeAssistantDTO();
        }

        var dataPoints = observation.Features
            .Select(f => new PointDTO
            {
                Date = f.Properties.From.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture),
                Value = f.Properties.Value
            })
            .ToList();
        var homeAssistantDTO = new HomeAssistantDTO
        {
            Name = "Accumulated heating degree days",
            Description = "Accumulated heating degree days with base 17°C for Denmark",
            Data = new Collection<PointDTO>(dataPoints)
        };

        return homeAssistantDTO;
    }

    public async Task<HomeAssistantDTO> GetAverageHeatingDegreeDays(int numberOfYears)
    {
        if (numberOfYears < 1 || numberOfYears > 20)
        {
            throw new ArgumentOutOfRangeException(nameof(numberOfYears), "numberOfYears must be between 1 and 20 (inclusive).");
        }

        var cacheKey = $"{cacheKeyHeatingDegreeDaysAverage}_{numberOfYears}";
        var observation = await cache.GetOrSetAsync<HomeAssistantDTO>(
           cacheKey,
           async (_, _) => await GetAverageHeatingDegreeDays_NoCache(numberOfYears)
        );

        return observation;
    }

    private async Task<HomeAssistantDTO> GetAverageHeatingDegreeDays_NoCache(int numberOfYears)
    {
        var observation = await climateDataService.GetParameterId(ParameterId.acc_heating_degree_days_17, 365 * numberOfYears);
        if (observation.Features.Count == 0)
        {
            logger.LogError("No climate data returned for parameter = heatingDegreesDaysAverage");
            return new HomeAssistantDTO();
        }

        int thisYear = DateTime.Now.Year;
        var dataPoints = observation.Features.GroupBy(f => f.Properties.From.DayOfYear)
            .Select(g => new PointDTO
            {
                Date = new DateTime(thisYear, 1, 1).AddDays(g.Key - 1).ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture),
                Value = Math.Round(g.Average(f => f.Properties.Value), 1)
            })
            .OrderByDescending(x => x.Date)
            .ToList();

        var calculator = new SimpleMovingAverage(k: 7);
        var updatedDataPoints = new Collection<PointDTO>();
        foreach (PointDTO point in dataPoints)
        {
            var sma = calculator.Update(point.Value);
            updatedDataPoints.Add(new PointDTO
            {
                Date = point.Date,
                Value = Math.Round(sma, 1)
            });
        }

        var homeAssistantDTO = new HomeAssistantDTO
        {
            Name = "average accumulated heating degree days",
            Description = $"{numberOfYears} year average of accumulated heating degree days with base 17°C",
            Data = updatedDataPoints
        };

        return homeAssistantDTO;
    }

    public async Task<HomeAssistantDTO> GetMeanTemperature(string stationId)
    {
        var observation = await cache.GetOrSetAsync<HomeAssistantDTO>(
            cacheKeyMeanTemp,
            async (_, _) => await GetMeanTemperature_NoCache(stationId)
        );

        return observation;
    }

    private async Task<HomeAssistantDTO> GetMeanTemperature_NoCache(string stationId)
    {
        var observation = await climateDataService.GetParameterId(ParameterId.mean_temp, 100);
        if (observation.Features.Count == 0)
        {
            logger.LogError("No climate data returned for station = {StationId} and parameter = meanTemperatur", stationId);
            return new HomeAssistantDTO();
        }

        var dataPoints = observation.Features
            .Where(f => f.Properties.Validity == true)
            .Select(f => new PointDTO
            {
                Date = f.Properties.From.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture),
                Value = f.Properties.Value
            })
            .ToList();
        var homeAssistantDTO = new HomeAssistantDTO
        {
            Name = "Mean temperature",
            Description = $"Station id: {observation.Features.First().Properties.StationId}",
            Data = new Collection<PointDTO>(dataPoints)
        };

        return homeAssistantDTO;
    }
}
