using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DecompoXor.Infrastructure.AI;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace DecompoXor.UnitTests;

public class GroqServiceTests
{
    [Fact]
    public async Task GenerateAsync_WhenGroqStopsAtTokenLimit_ThrowsClearError()
    {
        var responseBody = """
            {"choices":[{"finish_reason":"length","message":{"content":"{\\\"tasks\\\":["}}]}
            """;
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseBody, Encoding.UTF8, "application/json")
        });
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Groq:ApiKey"] = "test-key",
                ["Groq:Model"] = "test-model",
                ["Groq:BaseUrl"] = "https://example.test",
                ["Groq:ChatCompletionsPath"] = "/chat/completions",
                ["Groq:MaxTokens"] = "1024"
            })
            .Build();
        var service = new GroqService(
            new HttpClient(handler),
            configuration);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.GenerateAsync("prompt"));

        Assert.Contains("truncated its response", exception.Message);
        Assert.Contains("1024-token output limit", exception.Message);
    }

    private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
            => Task.FromResult(handler(request));
    }
}