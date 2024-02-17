using DMIProxy.DomainService;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace DMIProxy.HealthCheck
{
    public class RequestCacheHealthCheck : IHealthCheck
    {
        private IRequestCache _requestCache;

        public RequestCacheHealthCheck(IRequestCache requestCache)
        {
            _requestCache = requestCache;
        }

        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            var statistics = _requestCache.CacheStatistics();
            if (statistics == null)
            {
                return Task.FromResult(HealthCheckResult.Degraded("No statistics data"));
            }

            var totalCalls = statistics.TotalHits + statistics.TotalMisses;
            double hitRatio = (double)statistics.TotalHits / totalCalls;
            var data = new Dictionary<string, object>
                {
                    { "Hit ratio", hitRatio.ToString("P1") },
                    { "Total hit", statistics.TotalHits },
                    { "Total miss", statistics.TotalMisses },
                    { "Current entry count", statistics.CurrentEntryCount }
                };
            if (statistics.CurrentEstimatedSize != null)
            {
                data.Add("Current estimated size", statistics.CurrentEstimatedSize);
            }

            if (statistics.CurrentEntryCount != 3)
            {
                return Task.FromResult(HealthCheckResult.Degraded("Cache load not completed", null, data));
            }
            if (statistics.CurrentEstimatedSize > 10)
            {
                return Task.FromResult(HealthCheckResult.Degraded("Cache to big", null, data));
            }

            if (totalCalls < 10)
            {
                return Task.FromResult(HealthCheckResult.Degraded("Cache is starting up", null, data));
            }

            if (hitRatio < 0.8)
            {
                return Task.FromResult(HealthCheckResult.Degraded("To low cache hit ratio", null, data));
            }

            return Task.FromResult(HealthCheckResult.Healthy("All good", data));
        }
    }
}
