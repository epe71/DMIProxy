using DMIProxy.BusinessEntity;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using ZiggyCreatures.Caching.Fusion;

namespace DMIProxy.HealthCheck;

public class WeatherForcastHealthCheck(IFusionCache cache) : IHealthCheck
{
    private const string DefaultStationId = "2624652";

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        TextForecast cachedValue = cache.TryGet<TextForecast>($"TextForecast-{DefaultStationId}");
        if (cachedValue == null)
        {
            return Task.FromResult(HealthCheckResult.Degraded($"No Weather Forcast for station: {DefaultStationId}", null, null));
        }

        return Task.FromResult(HealthCheckResult.Healthy("All good", null));
    }

}
