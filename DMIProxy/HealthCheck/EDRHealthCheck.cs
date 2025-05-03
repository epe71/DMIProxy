using DMIProxy.BusinessEntity;
using DMIProxy.DomainService;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace DMIProxy.HealthCheck;

public class EDRHealthCheck(IRequestCache requestCache, IDateTimeProvider dateTimeProvider) : IHealthCheck
{
    private IRequestCache _requestCache = requestCache;

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        if (!_requestCache.GetEdrKeys(out Dictionary<string, DateTime>? keys) || keys == null)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("No keys in cache"));
        }

        var oldest = keys.OrderBy(x => x.Value).FirstOrDefault();
        var newest = keys.OrderByDescending(x => x.Value).FirstOrDefault();
        var data = new Dictionary<string, object>
                {
                    { "EdrKey count", keys.Count },
                    { "Oldest key", oldest },
                    { "Newest key", newest }
                };

        if (keys.Count < 2)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("Cache load not completed", null, data));
        }

        if ((dateTimeProvider.Now - oldest.Value) > TimeSpan.FromHours(5))
        {
            return Task.FromResult(HealthCheckResult.Degraded("Old values in cache", null, data));
        }

        return Task.FromResult(HealthCheckResult.Healthy("All good", data));
    }
}
