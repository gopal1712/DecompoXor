using System.Net.Http.Json;
using System.Threading.Tasks;
using DecompoXor.Domain.Entities;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace DecompoXor.IntegrationTests;

public class DecomposeTests : IClassFixture<WebApplicationFactory<DecompoXor.Api.Program>>
{
    private readonly WebApplicationFactory<DecompoXor.Api.Program> _factory;

    public DecomposeTests(WebApplicationFactory<DecompoXor.Api.Program> factory)
    {
        _factory = factory;
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
