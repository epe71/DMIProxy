using DMIProxy.BusinessEntity;
using DMIProxy.Contract;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using ZiggyCreatures.Caching.Fusion;

namespace DMIProxy.HealthCheck;

public class MetObsHealthCheck(IFusionCache cache, IDateTimeProvider dateTimeProvider) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var stationId = cache.TryGet<string>("RainCache:StationId");
        if (!stationId.HasValue)
        {
            return Task.FromResult(HealthCheckResult.Degraded("No MetObs data", null, null));
        }

        var data = new Dictionary<string, object>()
            {
                { "Station id", stationId }
            };

        RainDTO cachedValue = cache.TryGet<RainDTO>($"Rain-{stationId}");
        if (cachedValue == null)
        {
            return Task.FromResult(HealthCheckResult.Degraded("No MetObs data", null, data));
        }

        data = new Dictionary<string, object>()
            {
                { "When was data last updated (zulu time zone)", cachedValue.TimeStamp },
                { "Number of mesaurement in response", cachedValue.NumberReturned },
                { "Station id", stationId }
            };

        if (cachedValue.TimeStamp < dateTimeProvider.UtcNow.AddDays(-1))
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("MetObs data is to old", null, data));
        }

        if (cachedValue.TimeStamp < dateTimeProvider.UtcNow.AddHours(-3))
        {
            return Task.FromResult(HealthCheckResult.Degraded("MetObs data delayed", null, data));
        }

        if (cachedValue.NumberReturned < 27*24)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("To few MetObs data points", null, data));
        }

        return Task.FromResult(HealthCheckResult.Healthy("All good", data));
    }
}
