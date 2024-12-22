using Polly.Extensions.Http;
using Polly;

namespace DMIProxy;

public class PollyConfiguration
{
    public static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy(int retries = 3)
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError() // Fanger 5xx og timeout-fejl
            .WaitAndRetryAsync(
                retries,
                retryAttempt => TimeSpan.FromMinutes(Math.Pow(2, retryAttempt)), // Exponential backoff
                (result, timeSpan, retryCount, context) =>
                {
                    Console.WriteLine($"Retry {retryCount} - Ventetid: {timeSpan.TotalSeconds} sekunder");
                });
    }

    public static IAsyncPolicy<HttpResponseMessage> GetRequestRetryPolicy(int retries = 3, double waitSeconds = 1)
    {
        return HttpPolicyExtensions
        .HandleTransientHttpError() // <- an extension method from Polly
        .WaitAndRetryAsync(
        retryCount: retries,
        sleepDurationProvider: retryCount => TimeSpan.FromSeconds(waitSeconds * retryCount));
    }

    public static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(2, TimeSpan.FromSeconds(600)); // Stop efter 2 fejl i træk, i 10 min.
    }
}
