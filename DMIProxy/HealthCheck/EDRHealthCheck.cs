using DMIProxy.Contract;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using ZiggyCreatures.Caching.Fusion;

namespace DMIProxy.HealthCheck;

public class EDRHealthCheck(IFusionCache cache) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var cachedValue = cache.TryGet<HomeAssistantDTO>("EDR-wind-speed");
        if (cachedValue.HasValue == false)
        {
            var data = new Dictionary<string, object>
            {
                { "EDR-wind-speed is not in cache", false }
            };
            return Task.FromResult(HealthCheckResult.Unhealthy("No EDR keys in cache", null, data));
        }

        return Task.FromResult(HealthCheckResult.Healthy("All good", null));
    }
}
