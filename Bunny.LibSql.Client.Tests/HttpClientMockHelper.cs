using System;
using System.Net.Http;
using Moq;
using Moq.Protected;
using System.Threading;
using System.Threading.Tasks;

namespace Bunny.LibSql.Client.Tests
{
    public static class HttpClientMockHelper
    {
        public static HttpClient CreateMockedHttpClient(HttpResponseMessage responseMessage, Action<HttpRequestMessage>? requestCallback = null)
        {
            var handlerMock = new Mock<HttpMessageHandler>();

            handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .Callback<HttpRequestMessage, CancellationToken>((request, cancellationToken) =>
                {
                    requestCallback?.Invoke(request);
                })
                .ReturnsAsync(responseMessage);

            return new HttpClient(handlerMock.Object);
        }
    }
}
