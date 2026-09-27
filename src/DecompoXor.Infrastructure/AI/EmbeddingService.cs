using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;

using DecompoXor.Infrastructure.Abstractions.AI;
namespace DecompoXor.Infrastructure.AI;

public sealed class EmbeddingService : IEmbeddingGenerationService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public EmbeddingService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }

    public async Task<float[]> GetEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        var requestBody = new
        {
            model = _configuration["AI:EmbeddingModel"] ?? "all-MiniLM-L6-v2",
            input = text
        };
        var url = $"{_configuration["AI:BaseUrl"]}{_configuration["AI:EmbeddingPath"]}";
        var response = await _httpClient.PostAsJsonAsync(url, requestBody, cancellationToken);
        response.EnsureSuccessStatusCode();
        var data = await response.Content.ReadFromJsonAsync<EmbeddingResponse>(cancellationToken: cancellationToken);
        return data?.Embedding ?? Array.Empty<float>();
    }
}

public sealed class EmbeddingResponse
{
    [JsonPropertyName("embedding")]
    public float[] Embedding { get; set; } = Array.Empty<float>();
}
