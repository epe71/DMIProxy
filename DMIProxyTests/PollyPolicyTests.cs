using DMIProxy;
using Moq;
using Moq.Protected;
using Polly;
using System.Net;

namespace DMIProxyTests
{
    [TestClass]
    public sealed class PollyPolicyTests
    {
        [TestMethod]
        [DataRow(HttpStatusCode.RequestTimeout)]
        [DataRow(HttpStatusCode.TooManyRequests)]
        [DataRow(HttpStatusCode.InternalServerError)]
        [DataRow(HttpStatusCode.ServiceUnavailable)]
        public void RetryPolicy_ShouldRetry_OnStatusCode(HttpStatusCode simulatedHttpStatusCode)
        {
            // Arrange
            IAsyncPolicy<HttpResponseMessage> retryPolicy = PollyConfiguration.GetRetryPolicy();
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
            Assert.AreEqual(2, retryCount, "The policy should retry once before succeeding.");
        }
    }
}
