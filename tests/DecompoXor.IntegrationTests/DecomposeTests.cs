using System.Net.Http.Json;
using System.Threading.Tasks;
using DecompoXor.Domain.Entities;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration; // Added for AddInMemoryCollection extension
using Xunit;

namespace DecompoXor.IntegrationTests;

public class DecomposeTests : IClassFixture<WebApplicationFactory<DecompoXor.Api.Program>>
{
    private readonly WebApplicationFactory<DecompoXor.Api.Program> _factory;

    public DecomposeTests(WebApplicationFactory<DecompoXor.Api.Program> factory)
    {
        // Ensure the mock AI provider is used during integration testing to avoid external service calls.
        _factory = factory.WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((context, config) =>
                config.AddInMemoryCollection(new System.Collections.Generic.Dictionary<string, string>
                {
                    { "AI:Provider", "mock" },
                    { "AI:BaseUrl", "http://localhost" }
                })));
    }

    [Fact]
    public async Task Decompose_ReturnsResult()
    {
        var client = _factory.CreateClient();
        var story = new Story { AcceptanceCriteria = "User can log in" };
        var response = await client.PostAsJsonAsync("/api/story/decompose", story);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<DecompositionResult>();
        Assert.NotNull(result);
        Assert.NotEmpty(result.Tasks);
    }
}
