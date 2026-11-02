using DMIProxy.ApplicationService;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using ZiggyCreatures.Caching.Fusion;

namespace DMIProxy.HealthCheck;

public class EDRHealthCheck(IFusionCache cache) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var cachedValue = cache.TryGet<List<int>>(EdrStatusHistory.CacheKey, token: cancellationToken);
        var history = cachedValue.HasValue ? cachedValue.Value : null;
        var data = new Dictionary<string, object>
        {
            { "EDR status history", history ?? [] }
        };

        // Any status other than 200 or 429 is a failure, even if the history is incomplete.
        if (history is not null && history.Any(status => status != 200 && status != 429))
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("EDR returned a failing status", null, data));
        }

        if (history is null || history.Count < EdrStatusHistory.Capacity)
        {
            return Task.FromResult(HealthCheckResult.Degraded("EDR status history is missing or incomplete", null, data));
        }

        if (history.Contains(429))
        {
            return Task.FromResult(HealthCheckResult.Degraded("EDR returned HTTP 429", null, data));
        }

        return Task.FromResult(HealthCheckResult.Healthy("All good", data));
    }
}
