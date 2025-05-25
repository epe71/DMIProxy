using Polly;
using Polly.Contrib.WaitAndRetry;
using Polly.Extensions.Http;
using System.Net;

namespace DMIProxy;

public class PollyConfiguration
{
    public static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy(int retries = 1, int BackoffTimeInMinutes = 3)
    {
        var delay = Backoff.DecorrelatedJitterBackoffV2(medianFirstRetryDelay: TimeSpan.FromMinutes(BackoffTimeInMinutes), retryCount: retries);

        return HttpPolicyExtensions
            .HandleTransientHttpError() // Catch 5xx and timeout-errors
            .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.RequestTimeout)
            .WaitAndRetryAsync(delay);
    }

    public static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy(
        int handledEventsAllowedBeforeBreaking = 1,
        TimeSpan? durationOfBreak = null,
        ILogger? logger = null)
    {
        // Default break duration of 2 minutes if not provided.
        durationOfBreak ??= TimeSpan.FromMinutes(2);

        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking,
                durationOfBreak.Value,
                (outcome, timespan) =>
                {
                    // Logging structured data instead of writing directly to the console.
                    logger?.LogWarning("Circuit broken for {Duration} minutes due to {Reason}.",
                        timespan.TotalMinutes,
                        outcome.Exception?.Message ?? outcome.Result.StatusCode.ToString());
                },
                () =>
                {
                    logger?.LogInformation("Circuit reset.");
                },
                () =>
                {
                    logger?.LogInformation("Circuit in half-open state.");
                });
    }

    public static IAsyncPolicy<HttpResponseMessage> GetRateLimitAndCircuitBreakerPolicy(
           int maxCallPerMinute = 5,
           int handledEventsAllowedBeforeBreaking = 1,
           TimeSpan? durationOfBreak = null,
           ILogger? logger = null)
    {
        // Default break duration of 2 minutes if not provided.
        durationOfBreak ??= TimeSpan.FromMinutes(2);

        var rateLimiter = Policy
            .RateLimitAsync<HttpResponseMessage>(
                maxCallPerMinute,
                TimeSpan.FromMinutes(1));

        var circuitBreaker = HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(msg => msg.StatusCode == HttpStatusCode.TooManyRequests)
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking,
                durationOfBreak.Value,
                (outcome, timespan) =>
                {
                    logger?.LogWarning("Circuit broken for {Duration} minutes due to {Reason}.",
                        timespan.TotalMinutes,
                        outcome.Exception?.Message ?? outcome.Result.StatusCode.ToString());
                },
                () =>
                {
                    logger?.LogInformation("Circuit reset.");
                },
                () =>
                {
                    logger?.LogInformation("Circuit in half-open state.");
                });

        return Policy.WrapAsync(rateLimiter, circuitBreaker);
    }
}
