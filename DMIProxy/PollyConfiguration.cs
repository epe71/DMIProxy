using Polly.Extensions.Http;
using Polly.Contrib.WaitAndRetry;
using Polly;

namespace DMIProxy;

public class PollyConfiguration
{
    public static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy(int retries = 1)
    {
        int BackoffTimeInMinutes = 3; // Time to wait before retrying
        var delay = Backoff.DecorrelatedJitterBackoffV2(medianFirstRetryDelay: TimeSpan.FromMinutes(BackoffTimeInMinutes), retryCount: retries);

        return HttpPolicyExtensions
            .HandleTransientHttpError() // Fanger 5xx og timeout-fejl
            .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.RequestTimeout)
            .WaitAndRetryAsync(delay);
    }

    public static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy(int retries = 1)
    {
        int BreakDurationInMinutes = 2; // Duration of the break in minutes

        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            .CircuitBreakerAsync(
                retries,
                TimeSpan.FromMinutes(BreakDurationInMinutes),
                (outcome, timespan) => Console.WriteLine($"Circuit broken for {timespan.TotalMinutes} minutes."),
                () => Console.WriteLine("Circuit reset."),
                () => Console.WriteLine("Circuit in half-open state."));
    }

    public static IAsyncPolicy<HttpResponseMessage> GetRateLimitAndCircuitBreakerPolicy()
    {
        int MaxCallPerMinute = 5; // Max calls per minute
        int BreakDurationInMinutes = 2; // Duration of the break in minutes

        var rateLimiter = Policy
            .RateLimitAsync<HttpResponseMessage>(MaxCallPerMinute, TimeSpan.FromMinutes(1)); 

        var circuitBreaker = HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: 1,
                durationOfBreak: TimeSpan.FromMinutes(BreakDurationInMinutes),
                onBreak: (outcome, timespan) => Console.WriteLine($"Circuit broken for {timespan.TotalMinutes} minutes."),
                onReset: () => Console.WriteLine("Circuit reset."),
                onHalfOpen: () => Console.WriteLine("Circuit in half-open state."));

        return Policy.WrapAsync(rateLimiter, circuitBreaker);
    }
}
