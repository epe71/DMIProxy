using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Globalization;
using DMIProxy.DomainService;

namespace DMIProxy;

public class DefaultExceptionHandler(ILogger<DefaultExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(exception);
        cancellationToken.ThrowIfCancellationRequested();

        if (exception is Polly.RateLimit.RateLimitRejectedException rateLimitException)
        {
            var retryAfterSeconds = Math.Max(1, (int)Math.Ceiling(rateLimitException.RetryAfter.TotalSeconds));
            logger.LogWarning(
                "Request rejected by DMIProxy's local rate limiter. RetryAfterSeconds={RetryAfterSeconds} Path={Path}",
                retryAfterSeconds,
                httpContext.Request.Path);

            httpContext.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
            httpContext.Response.Headers.RetryAfter = retryAfterSeconds.ToString(CultureInfo.InvariantCulture);
            await httpContext.Response.WriteAsJsonAsync(new ProblemDetails
            {
                Status = (int)HttpStatusCode.TooManyRequests,
                Type = nameof(Polly.RateLimit.RateLimitRejectedException),
                Title = "DMIProxy rate limit exceeded",
                Detail = $"Retry the request after {retryAfterSeconds} seconds.",
                Instance = $"{httpContext.Request.Method} {httpContext.Request.Path}"
            }, cancellationToken);

            return true;
        }

        if (exception is DmiUpstreamRateLimitException upstreamRateLimitException)
        {
            var retryAfterSeconds = upstreamRateLimitException.RetryAfter is { } retryAfter
                ? Math.Max(1, (int)Math.Ceiling(retryAfter.TotalSeconds))
                : (int?)null;

            logger.LogWarning(
                "DMI upstream rate limit reached. EventType={EventType} RetryAfterSeconds={RetryAfterSeconds} Path={Path}",
                "UpstreamDmiRateLimit",
                retryAfterSeconds,
                httpContext.Request.Path);

            httpContext.Response.StatusCode = (int)HttpStatusCode.ServiceUnavailable;
            if (retryAfterSeconds.HasValue)
            {
                httpContext.Response.Headers.RetryAfter = retryAfterSeconds.Value.ToString(CultureInfo.InvariantCulture);
            }

            await httpContext.Response.WriteAsJsonAsync(new ProblemDetails
            {
                Status = (int)HttpStatusCode.ServiceUnavailable,
                Type = nameof(DmiUpstreamRateLimitException),
                Title = "DMI temporarily rate limited this service",
                Detail = retryAfterSeconds.HasValue
                    ? $"Retry the request after {retryAfterSeconds.Value} seconds."
                    : "DMI did not provide a Retry-After value. Retry later.",
                Instance = $"{httpContext.Request.Method} {httpContext.Request.Path}"
            }, cancellationToken);

            return true;
        }

        logger.LogError(exception, "An unexpected error occurred");

        await httpContext.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = (int)HttpStatusCode.InternalServerError,
            Type = exception.GetType().Name,
            Title = "An unexpected error occurred",
            Detail = exception.Message,
            Instance = $"{httpContext.Request.Method} {httpContext.Request.Path}"
        }, cancellationToken);

        return true;
    }
}
