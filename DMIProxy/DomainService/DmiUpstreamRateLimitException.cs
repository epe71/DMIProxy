using System.Net;

namespace DMIProxy.DomainService;

/// <summary>
/// Represents an HTTP 429 response received from DMI and preserves its Retry-After value.
/// </summary>
#pragma warning disable CA1032 // Implement standard exception constructors
public sealed class DmiUpstreamRateLimitException(TimeSpan? retryAfter)
#pragma warning restore CA1032 // Implement standard exception constructors
    : HttpRequestException("DMI returned HTTP 429 Too Many Requests.", null, HttpStatusCode.TooManyRequests)
{
    public TimeSpan? RetryAfter { get; } = retryAfter;
}

public static class DmiHttpResponseExtensions
{
    public static void EnsureDmiSuccessStatusCode(this HttpResponseMessage response)
    {
        ArgumentNullException.ThrowIfNull(response);

        if (response.StatusCode == HttpStatusCode.TooManyRequests)
        {
            var retryAfter = response.Headers.RetryAfter?.Delta;
            if (retryAfter is null && response.Headers.RetryAfter?.Date is DateTimeOffset retryDate)
            {
                retryAfter = retryDate - DateTimeOffset.UtcNow;
            }

            if (retryAfter < TimeSpan.Zero)
                retryAfter = TimeSpan.Zero;

            throw new DmiUpstreamRateLimitException(retryAfter);
        }

        response.EnsureSuccessStatusCode();
    }
}
