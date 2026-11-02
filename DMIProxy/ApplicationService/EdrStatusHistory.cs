using ZiggyCreatures.Caching.Fusion;

namespace DMIProxy.ApplicationService;

/// <summary>
/// Keeps the outcomes of the most recent EDR upstream requests for the EDR health check.
/// </summary>
public sealed class EdrStatusHistory(IFusionCache cache) : IDisposable
{
    public const string CacheKey = "EDR-Status";
    public const int Capacity = 5;

    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(24);
    private readonly SemaphoreSlim _writeLock = new(1, 1);

    public async Task AddAsync(int statusCode, CancellationToken cancellationToken = default)
    {
        await _writeLock.WaitAsync(cancellationToken);
        try
        {
            var cachedHistory = cache.TryGet<List<int>>(CacheKey, token: cancellationToken);
            var history = cachedHistory.HasValue && cachedHistory.Value is not null
                ? cachedHistory.Value.ToList()
                : [];

            if (history.Count >= Capacity)
                history.RemoveRange(0, history.Count - Capacity + 1);

            history.Add(statusCode);

            await cache.SetAsync(
                CacheKey,
                history,
                options => options.SetDuration(CacheDuration),
                token: cancellationToken);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    public void Dispose() => _writeLock.Dispose();
}
