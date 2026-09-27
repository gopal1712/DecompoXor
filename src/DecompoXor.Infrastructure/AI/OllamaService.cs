using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using DecompoXor.Infrastructure.Abstractions.AI;

// Ollama AI service implementation.
namespace DecomposXor.Infrastructure.AI;

public sealed class OllamaService : IChatCompletionService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public OllamaService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }

    public async Task<string> GenerateAsync(string prompt, CancellationToken cancellationToken = default)
    {
        var request = new
        {
            model = _configuration["AI:Model"] ?? "llama3.1",
            prompt = prompt,
            stream = _configuration.GetValue<bool>("AI:Stream"),
            format = _configuration["AI:Format"],
            temperature = _configuration.GetValue<double>("AI:Temperature"),
            top_p = _configuration.GetValue<double>("AI:TopP"),
            keep_alive = _configuration["AI:KeepAlive"],
            options = new
            {
                num_predict = _configuration.GetValue<int>("AI:NumPredict"),
                top_k = _configuration.GetValue<int>("AI:TopK"),
                repeat_penalty = _configuration.GetValue<double>("AI:RepeatPenalty"),
                num_ctx = _configuration.GetValue<int>("AI:NumContext"),
                num_gpu = _configuration.GetValue<int>("AI:NumGpu"),
                think = _configuration.GetValue<bool>("AI:Think")
            }
        };

        var url = $"{_configuration["AI:BaseUrl"]}{_configuration["AI:GeneratePath"]}";
        var response = await _httpClient.PostAsJsonAsync(url, request, cancellationToken);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<GenerateResponse>(cancellationToken: cancellationToken);
        return result?.Response ?? string.Empty;
    }
}

public class GenerateResponse
{
    [JsonPropertyName("response")]
    public string Response { get; set; } = string.Empty;
}
