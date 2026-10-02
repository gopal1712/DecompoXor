using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using DecompoXor.Infrastructure.AI;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace DecompoXor.UnitTests;

public class EmbeddingServiceTests
{
    [Fact]
    public async Task GetEmbeddingAsync_SendsBearerTokenAndReturnsEmbedding()
    {
        var handler = new StubHttpMessageHandler((request, _) =>
        {
            Assert.Equal("Bearer test-key", request.Headers.Authorization?.ToString());
            Assert.Equal("https://openrouter.ai/api/v1/embeddings", request.RequestUri!.ToString());

            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"data\":[{\"embedding\":[1.0,2.0,3.0]}]}")
            };

            return Task.FromResult(response);
        });

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Openrouter:ApiKey"] = "test-key",
                ["Openrouter:BaseUrl"] = "https://openrouter.ai/api/v1",
                ["Openrouter:EmbeddingPath"] = "/embeddings",
                ["Openrouter:EmbeddingModel"] = "sentence-transformers/all-MiniLM-L6-v2"
            })
            .Build();

        var service = new EmbeddingService(new HttpClient(handler), configuration);

        var result = await service.GetEmbeddingAsync("hello world");

        Assert.Equal(new[] { 1f, 2f, 3f }, result);
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _handler;

        public StubHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
        {
            _handler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => _handler(request, cancellationToken);
    }
}
