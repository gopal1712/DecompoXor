using System.Net.Http.Headers;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

using DecompoXor.Infrastructure.Abstractions.AI;
namespace DecompoXor.Infrastructure.AI;

public sealed class EmbeddingService : IEmbeddingGenerationService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly string _apiKey;

    public EmbeddingService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        // Retrieve API key from environment variable set by the .env loader (Openrouter__ApiKey)
        _apiKey = Environment.GetEnvironmentVariable("Openrouter__ApiKey") ?? throw new InvalidOperationException("Openrouter:ApiKey is required when OpenRouter is selected.");
    }

    public async Task<float[]> GetEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        var model = _configuration["Openrouter:EmbeddingModel"] ?? "all-MiniLM-L6-v2";

        var requestBody = new
        {
            model,
            input = text
        };

        var baseUrl = _configuration["Openrouter:BaseUrl"]?.TrimEnd('/')
            ?? throw new InvalidOperationException("Openrouter:BaseUrl is required when OpenRouter is selected.");
        var embeddingPath = _configuration["Openrouter:EmbeddingPath"] ?? "/embeddings";
        var url = $"{baseUrl}{embeddingPath}";

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json")
        };

        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

        using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
         response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        var data = JsonConvert.DeserializeObject<EmbeddingResponse>(responseBody);
        return data?.GetEmbedding() ?? Array.Empty<float>();
    }
}

public sealed class EmbeddingResponse
{
    [JsonPropertyName("embedding")]
    public float[]? Embedding { get; set; }

    [JsonPropertyName("data")]
    public List<EmbeddingData>? Data { get; set; }

    public float[] GetEmbedding()
    {
        if (Embedding is { Length: > 0 })
        {
            return Embedding;
        }

        if (Data is not null)
        {
            foreach (var item in Data)
            {
                if (item.Embedding is { Length: > 0 })
                {
                    return item.Embedding;
                }
            }
        }

        return Array.Empty<float>();
    }
}

public sealed class EmbeddingData
{
    [JsonPropertyName("embedding")]
    public float[]? Embedding { get; set; }
}
