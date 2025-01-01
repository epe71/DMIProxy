using Polly.Extensions.Http;
using Polly;

namespace DMIProxy;

public class PollyConfiguration
{
    public static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy(int retries = 2)
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError() // Fanger 5xx og timeout-fejl
            .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.RequestTimeout)
            .WaitAndRetryAsync(
                retries,
                retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), // Exponential backoff
                (result, timeSpan, retryCount, context) =>
                {
                    Console.WriteLine($"Retry {retryCount} - Delay before next call: {timeSpan.TotalSeconds} seconds");
                });
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
}
