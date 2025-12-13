using DMIProxy.BusinessEntity;
using DMIProxy.DomainService;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace DMIProxy.HealthCheck;

public class MetObsHealthCheck(IRequestCache requestCache, IDateTimeProvider dateTimeProvider) : IHealthCheck
{
    private IRequestCache _requestCache = requestCache;

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var stationId = "06072";
        var data = new Dictionary<string, object>()
            {
                { "Station id", stationId }
            };

        _requestCache.GetRainDTO(stationId, out var rainDto);
        if (rainDto == null)
        {
            return Task.FromResult(HealthCheckResult.Degraded("No MetObs data", null, data));
        }

        data = new Dictionary<string, object>()
            {
                { "When was data last updated (zulu time zone)", rainDto.TimeStamp },
                { "Number of mesaurement in response", rainDto.NumberReturned },
                { "Station id", stationId }
            };

        if (rainDto.TimeStamp < dateTimeProvider.UtcNow.AddDays(-1))
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("MetObs data is to old", null, data));
        }

        if (rainDto.TimeStamp < dateTimeProvider.UtcNow.AddHours(-3))
        {
            return Task.FromResult(HealthCheckResult.Degraded("MetObs data delayed", null, data));
        }

        if (rainDto.NumberReturned < 27*24)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("To few MetObs data points", null, data));
        }

        return Task.FromResult(HealthCheckResult.Healthy("All good", data));
    }
}
