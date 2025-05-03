using Polly.Extensions.Http;
using Polly.Contrib.WaitAndRetry;
using Polly;

namespace DMIProxy;

public class PollyConfiguration
{
    public static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy(int retries = 2)
    {
        var delay = Backoff.DecorrelatedJitterBackoffV2(medianFirstRetryDelay: TimeSpan.FromMinutes(3), retryCount: retries);

        return HttpPolicyExtensions
            .HandleTransientHttpError() // Fanger 5xx og timeout-fejl
            .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.RequestTimeout)
            .WaitAndRetryAsync(delay);
    }

    public static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy(int retries = 1)
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            .CircuitBreakerAsync(
                retries,
                TimeSpan.FromSeconds(300),
                (outcome, timespan) => Console.WriteLine($"Circuit broken for {timespan.TotalSeconds} seconds."),
                () => Console.WriteLine("Circuit reset."),
                () => Console.WriteLine("Circuit in half-open state."));
    }

    public static IAsyncPolicy<HttpResponseMessage> GetRateLimitAndCircuitBreakerPolicy()
    {
        var rateLimiter = Policy
            .RateLimitAsync<HttpResponseMessage>(5, TimeSpan.FromMinutes(1)); 

        var circuitBreaker = HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: 1,
                durationOfBreak: TimeSpan.FromMinutes(3),
                onBreak: (outcome, timespan) => Console.WriteLine($"Circuit broken for {timespan.TotalSeconds} seconds."),
                onReset: () => Console.WriteLine("Circuit reset."),
                onHalfOpen: () => Console.WriteLine("Circuit in half-open state."));

        return Policy.WrapAsync(rateLimiter, circuitBreaker);
    }
}
