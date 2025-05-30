using DMIProxy.BusinessEntity;
using DMIProxy.DomainService;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace DMIProxy.HealthCheck;

public class EDRHealthCheck(IRequestCache requestCache, IDateTimeProvider dateTimeProvider) : IHealthCheck
{
    private IRequestCache _requestCache = requestCache;

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        if (!_requestCache.GetAllEdrKeys(out Dictionary<string, DateTime>? keys) || keys == null)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("No EDR keys in cache"));
        }

        var orderedKeys = keys.OrderBy(k => k.Value);
        var data = new Dictionary<string, object>();
        foreach (var key in orderedKeys)
        {
            data.Add(key.Key, key.Value.ToString("yyyy-MM-dd HH:mm:ss"));
        }

        if (keys.Count < 2)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("EDR cache load not completed", null, data));
        }

        var oldest = orderedKeys.FirstOrDefault();
        if ((dateTimeProvider.UtcNow - oldest.Value) > TimeSpan.FromHours(5))
        {
            return Task.FromResult(HealthCheckResult.Degraded("Old EDR values in cache", null, data));
        }

        return Task.FromResult(HealthCheckResult.Healthy("All good", data));
    }
}
