using DMIProxy;
using Moq;
using Moq.Protected;
using Polly;
using Polly.CircuitBreaker;
using System.Net;

namespace DMIProxyTests;

[TestClass]
public class PollyPolicyTests
{
    [TestMethod]
    [DataRow(HttpStatusCode.RequestTimeout)]
    [DataRow(HttpStatusCode.InternalServerError)]
    [DataRow(HttpStatusCode.ServiceUnavailable)]
    public void RetryPolicy_ShouldRetry_OnStatusCode(HttpStatusCode simulatedHttpStatusCode)
    {
        // Arrange
        IAsyncPolicy<HttpResponseMessage> retryPolicy = PollyConfiguration.GetRetryPolicy(2);
        int retryCount = 0;

        // Mock HTTP handler to simulate responses
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected() // Access protected members
            .SetupSequence<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(simulatedHttpStatusCode)) // Retry 
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)); // Success

        var httpClient = new HttpClient(handlerMock.Object);

        // Act
        var result = retryPolicy.ExecuteAsync(async () =>
        {
            retryCount++;
            return await httpClient.GetAsync("http://example.com");
        });

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, result.Result.StatusCode, "The final response should be OK.");
        Assert.AreEqual(2, retryCount, "First call should fail then retry once before succeeding.");
    }

    [TestMethod]
    [DataRow(HttpStatusCode.TooManyRequests)]
    [DataRow(HttpStatusCode.Unauthorized)]
    public void RetryPolicy_NoRetry(HttpStatusCode simulatedHttpStatusCode)
    {
        // Arrange
        IAsyncPolicy<HttpResponseMessage> retryPolicy = PollyConfiguration.GetRetryPolicy(2);
        int retryCount = 0;

        // Mock HTTP handler to simulate responses
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected() // Access protected members
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(simulatedHttpStatusCode));

        var httpClient = new HttpClient(handlerMock.Object);

        // Act
        var result1 = retryPolicy.ExecuteAsync(async () =>
        {
            retryCount++;
            return await httpClient.GetAsync("http://example.com");
        });
        // Assert
        Assert.AreEqual(simulatedHttpStatusCode, result1.Result.StatusCode, "The final response should be OK.");
        Assert.AreEqual(1, retryCount, "The policy should retry once before succeeding.");
    }

    [TestMethod]
    [DataRow(HttpStatusCode.RequestTimeout)]
    [DataRow(HttpStatusCode.InternalServerError)]
    [DataRow(HttpStatusCode.TooManyRequests)]
    [DataRow(HttpStatusCode.ServiceUnavailable)]
    [DataRow(HttpStatusCode.Unauthorized)]
    public void CircuitBreakerPolicy_OpenCircuitAfter2Calls(HttpStatusCode simulatedHttpStatusCode)
    {
        // Arrange
        IAsyncPolicy<HttpResponseMessage> circuitBreakerPolicy = PollyConfiguration.GetCircuitBreakerPolicy(2);

        // Mock HTTP handler to simulate responses
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected() // Access protected members
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(simulatedHttpStatusCode));

        var httpClient = new HttpClient(handlerMock.Object);

        // Act & Assert
        // First failure
        var response1 = circuitBreakerPolicy.ExecuteAsync(() => httpClient.GetAsync("http://example.com"));
        Assert.AreEqual(simulatedHttpStatusCode, response1.Result.StatusCode, "First request should fail.");

        // Second failure
        var response2 = circuitBreakerPolicy.ExecuteAsync(() => httpClient.GetAsync("http://example.com"));
        Assert.AreEqual(simulatedHttpStatusCode, response2.Result.StatusCode, "Second request should fail.");

        // Circuit should now be open
        Assert.ThrowsExactlyAsync<BrokenCircuitException>(async () =>
        {
            await circuitBreakerPolicy.ExecuteAsync(() => httpClient.GetAsync("http://example.com"));
        });
    }
}
