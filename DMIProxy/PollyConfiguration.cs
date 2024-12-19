using Polly.Extensions.Http;
using Polly;

namespace DMIProxy;

public class PollyConfiguration
{
    public static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
    {
        // Retry op til 5 gange med exponential backoff
        return HttpPolicyExtensions
            .HandleTransientHttpError() // Fanger 5xx og timeout-fejl
            .WaitAndRetryAsync(
                5,  // Antal retries
                retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), // Exponential backoff
                (result, timeSpan, retryCount, context) =>
                {
                    Console.WriteLine($"Retry {retryCount} - Ventetid: {timeSpan.TotalSeconds} sekunder");
                });
    }

    public static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(2, TimeSpan.FromSeconds(600)); // Stop efter 2 fejl i træk, i 30 sek.
    }
}
