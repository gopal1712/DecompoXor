using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace StoryDecomposer.Services;

public sealed class OllamaService : ILLMService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    
    public OllamaService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }
    
    public async Task<string> Generate(string prompt, string context = "")
    {
        var fullPrompt = prompt;
        
        var request = new
        {
            model = _configuration["Ollama:Model"],
            prompt = fullPrompt,
            stream = _configuration.GetValue<bool>("Ollama:Stream"),
            format = _configuration["Ollama:Format"],
            temperature = _configuration.GetValue<double>("Ollama:Temperature"),
            top_p = _configuration.GetValue<double>("Ollama:TopP"),
            keep_alive = _configuration["Ollama:KeepAlive"],
            options = new
            {
                num_predict = _configuration.GetValue<int>("Ollama:NumPredict"),
                top_k = _configuration.GetValue<int>("Ollama:TopK"),
                repeat_penalty = _configuration.GetValue<double>("Ollama:RepeatPenalty"),
                num_ctx = _configuration.GetValue<int>("Ollama:NumContext"),
                num_gpu = _configuration.GetValue<int>("Ollama:NumGpu"),
                think = _configuration.GetValue<bool>("Ollama:Think")            }
        };
        
        var response = await _httpClient.PostAsJsonAsync(
            $"{_configuration["Ollama:BaseUrl"]}{_configuration["Ollama:GeneratePath"]}",
            request);
        
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<GenerateResponse>();
        return result?.Response ?? string.Empty;
    }
}

public class GenerateResponse
{
    [JsonPropertyName("response")]
    public string Response { get; set; } = string.Empty;
}
