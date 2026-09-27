namespace StoryDecomposer.RAG;

using System.Net.Http.Json;
using System.Text.Json.Serialization;

public sealed class EmbeddingService
{
     private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public EmbeddingService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }

    public async Task<float[]> GetEmbeddingAsync(string text)
    {
        var requestBody = new
        {
            model = _configuration["Ollama:EmbeddingModel"],
            input = text
        };

        var response = await _httpClient.PostAsJsonAsync(
            $"{_configuration["Ollama:BaseUrl"]}{_configuration["Ollama:EmbeddingPath"]}", requestBody);
        response.EnsureSuccessStatusCode();

        var responseData = await response.Content.ReadFromJsonAsync<EmbeddingResponse>();
        return responseData?.Embedding ?? Array.Empty<float>();
    }
}

public sealed class EmbeddingResponse
{
    [JsonPropertyName("embedding")]
    public float[] Embedding { get; set; } = Array.Empty<float>();
}
