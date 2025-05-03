using Polly.Extensions.Http;
using Polly.Contrib.WaitAndRetry;
using Polly;

namespace DMIProxy;

public class PollyConfiguration
{
    public static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy(int retries = 2)
    {
        var delay = Backoff.DecorrelatedJitterBackoffV2(medianFirstRetryDelay: TimeSpan.FromMinutes(5), retryCount: retries);

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
}
