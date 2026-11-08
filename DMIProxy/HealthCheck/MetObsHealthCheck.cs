using DMIProxy.BusinessEntity;
using DMIProxy.Contract;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using ZiggyCreatures.Caching.Fusion;

namespace DMIProxy.HealthCheck;

public class MetObsHealthCheck(IFusionCache cache, IDateTimeProvider dateTimeProvider) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var stationId = await cache.TryGetAsync<string>("RainCache:StationId", token: cancellationToken);
        if (!stationId.HasValue)
        {
            return await Task.FromResult(HealthCheckResult.Degraded("No MetObs data", null, null));
        }

        var data = new Dictionary<string, object>()
            {
                { "Station id", stationId }
            };

        RainDTO cachedValue = await cache.TryGetAsync<RainDTO>($"Rain-{stationId}", token: cancellationToken);
        if (cachedValue == null)
        {
            return await Task.FromResult(HealthCheckResult.Degraded("No MetObs data", null, data));
        }

        data = new Dictionary<string, object>()
            {
                { "When was data last updated (zulu time zone)", cachedValue.TimeStamp },
                { "Number of mesaurement in response", cachedValue.NumberReturned },
                { "Station id", stationId }
            };

        if (cachedValue.TimeStamp < dateTimeProvider.UtcNow.AddDays(-1))
        {
            return await Task.FromResult(HealthCheckResult.Unhealthy("MetObs data is to old", null, data));
        }

        if (cachedValue.TimeStamp < dateTimeProvider.UtcNow.AddHours(-3))
        {
            return await Task.FromResult(HealthCheckResult.Degraded("MetObs data delayed", null, data));
        }

        if (cachedValue.NumberReturned < 27 * 24)
        {
            return await Task.FromResult(HealthCheckResult.Unhealthy("To few MetObs data points", null, data));
        }

        return await Task.FromResult(HealthCheckResult.Healthy("All good", data));
    }
}
