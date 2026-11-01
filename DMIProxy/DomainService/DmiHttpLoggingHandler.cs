using System.Diagnostics;
using System.Net;

namespace DMIProxy.DomainService;

/// <summary>
/// Logs each physical request sent to DMI, including retries performed by Polly.
/// </summary>
public sealed class DmiHttpLoggingHandler(ILogger<DmiHttpLoggingHandler> logger) : DelegatingHandler
{
    private const string DmiHost = "opendataapi.dmi.dk";
    private static readonly string InstanceId =
        Environment.GetEnvironmentVariable("DMI_PROXY_INSTANCE_ID") ?? Environment.MachineName;

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!IsDmiRequest(request.RequestUri))
            return await base.SendAsync(request, cancellationToken);

        var startedAt = Stopwatch.GetTimestamp();
        var requestId = Guid.NewGuid().ToString("N");
        var correlationId = Activity.Current?.TraceId.ToString();
        if (string.IsNullOrWhiteSpace(correlationId) &&
            request.Headers.TryGetValues("X-Correlation-ID", out var values))
        {
            correlationId = values.FirstOrDefault();
        }
        correlationId ??= requestId;
        var apiType = GetApiType(request.RequestUri!);

        try
        {
            var response = await base.SendAsync(request, cancellationToken);
            var elapsedMs = Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds;
            var retryAfterHeader = response.Headers.RetryAfter;
            double? retryAfterMs = retryAfterHeader?.Delta?.TotalMilliseconds;
            if (retryAfterMs is null && retryAfterHeader?.Date is DateTimeOffset retryDate)
            {
                retryAfterMs = Math.Max(0, (retryDate - DateTimeOffset.UtcNow).TotalMilliseconds);
            }

            var logLevel = (int)response.StatusCode >= 500 || response.StatusCode == HttpStatusCode.TooManyRequests
                ? LogLevel.Warning
                : LogLevel.Information;

            if (retryAfterMs.HasValue && retryAfterHeader?.Date is DateTimeOffset retryAfterDateUtc)
            {
                logger.Log(logLevel,
                    "DMI HTTP request completed. EventType={EventType} ApiType={ApiType} Method={Method} StatusCode={StatusCode} DurationMs={DurationMs} RetryAfterMs={RetryAfterMs} RetryAfterDateUtc={RetryAfterDateUtc} CorrelationId={CorrelationId} DmiRequestId={DmiRequestId} InstanceId={InstanceId}",
                    "DmiHttpResponse",
                    apiType,
                    request.Method.Method,
                    (int)response.StatusCode,
                    elapsedMs,
                    retryAfterMs.Value,
                    retryAfterDateUtc,
                    correlationId,
                    requestId,
                    InstanceId);
            }
            else if (retryAfterMs.HasValue)
            {
                logger.Log(logLevel,
                    "DMI HTTP request completed. EventType={EventType} ApiType={ApiType} Method={Method} StatusCode={StatusCode} DurationMs={DurationMs} RetryAfterMs={RetryAfterMs} CorrelationId={CorrelationId} DmiRequestId={DmiRequestId} InstanceId={InstanceId}",
                    "DmiHttpResponse",
                    apiType,
                    request.Method.Method,
                    (int)response.StatusCode,
                    elapsedMs,
                    retryAfterMs.Value,
                    correlationId,
                    requestId,
                    InstanceId);
            }
            else
            {
                logger.Log(logLevel,
                    "DMI HTTP request completed. EventType={EventType} ApiType={ApiType} Method={Method} StatusCode={StatusCode} DurationMs={DurationMs} CorrelationId={CorrelationId} DmiRequestId={DmiRequestId} InstanceId={InstanceId}",
                    "DmiHttpResponse",
                    apiType,
                    request.Method.Method,
                    (int)response.StatusCode,
                    elapsedMs,
                    correlationId,
                    requestId,
                    InstanceId);
            }

            return response;
        }
        catch (Exception exception)
        {
            logger.LogError(exception,
                "DMI HTTP request failed without a response. EventType={EventType} ApiType={ApiType} Method={Method} DurationMs={DurationMs} CorrelationId={CorrelationId} DmiRequestId={DmiRequestId} ExceptionType={ExceptionType} InstanceId={InstanceId}",
                "DmiTransportFailure",
                apiType,
                request.Method.Method,
                Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds,
                correlationId,
                requestId,
                exception.GetType().Name,
                InstanceId);
            throw;
        }
    }

    private static bool IsDmiRequest(Uri? requestUri) =>
        requestUri is not null && requestUri.IsAbsoluteUri &&
        string.Equals(requestUri.Host, DmiHost, StringComparison.OrdinalIgnoreCase);

    private static string GetApiType(Uri requestUri)
    {
        var segments = requestUri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        return segments.Length >= 2 ? segments[1] : "unknown";
    }
}

/// <summary>
/// Distinguishes requests rejected by DMIProxy's local Polly rate limiter from DMI HTTP 429 responses.
/// </summary>
public sealed class DmiLocalRateLimitLoggingHandler(ILogger<DmiLocalRateLimitLoggingHandler> logger) : DelegatingHandler
{
    private static readonly string InstanceId =
        Environment.GetEnvironmentVariable("DMI_PROXY_INSTANCE_ID") ?? Environment.MachineName;

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        try
        {
            return await base.SendAsync(request, cancellationToken);
        }
        catch (Polly.RateLimit.RateLimitRejectedException exception)
        {
            if (request.RequestUri is { IsAbsoluteUri: true } uri &&
                string.Equals(uri.Host, "opendataapi.dmi.dk", StringComparison.OrdinalIgnoreCase))
            {
                var correlationId = Activity.Current?.TraceId.ToString()
                    ?? (request.Headers.TryGetValues("X-Correlation-ID", out var values)
                        ? values.FirstOrDefault()
                        : null)
                    ?? Guid.NewGuid().ToString("N");
                var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
                var apiType = segments.Length >= 2 ? segments[1] : "unknown";

                logger.LogWarning(
                    "DMI request rejected by DMIProxy local rate limiter. EventType={EventType} ApiType={ApiType} Method={Method} RetryAfterMs={RetryAfterMs} CorrelationId={CorrelationId} InstanceId={InstanceId}",
                    "LocalRateLimitRejection",
                    apiType,
                    request.Method.Method,
                    exception.RetryAfter.TotalMilliseconds,
                    correlationId,
                    InstanceId);
            }

            throw;
        }
    }
}
