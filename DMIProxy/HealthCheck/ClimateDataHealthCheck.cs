using DMIProxy.Contract;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using ZiggyCreatures.Caching.Fusion;

namespace DMIProxy.HealthCheck;

public class ClimateDataHealthCheck(IFusionCache cache) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        HomeAssistantDTO cachedValue = cache.TryGet<HomeAssistantDTO>("HeatingDegreeDays");
        if (cachedValue == null)
        {
            return Task.FromResult(HealthCheckResult.Degraded("No Climate Data: HeatingDegreeDays", null, null));
        }

        return Task.FromResult(HealthCheckResult.Healthy("All good", null));
    }
}
