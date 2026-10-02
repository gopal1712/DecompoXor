using Newtonsoft.Json;
using Microsoft.Extensions.Configuration;
using DecompoXor.Infrastructure.Abstractions.AI;

namespace DecompoXor.Infrastructure.AI;

public sealed class GroqService : IChatCompletionService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly string _apiKey;

    public GroqService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        // Retrieve API key from environment variable set by the .env loader (Groq__ApiKey)
        _apiKey = Environment.GetEnvironmentVariable("Groq__ApiKey") ?? throw new InvalidOperationException("Groq:ApiKey is required when Groq is selected.");
    }

    public async Task<string> GenerateAsync(string prompt, CancellationToken cancellationToken = default)
    {
        var model = _configuration["Groq:Model"];
        var maxTokens = _configuration.GetValue("Groq:MaxTokens", 4096);

        var request = new
        {
            model,
            messages = new[]
            {
                new { role = "user", content = $"Task:\n{prompt}\n\nResponse:" }
            },
            temperature = _configuration.GetValue<double>("Groq:Temperature"),
            max_tokens = maxTokens
        };

        var baseUrl = _configuration["Groq:BaseUrl"]!.TrimEnd('/');
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}{_configuration["Groq:ChatCompletionsPath"]}")
        {
            Content = new StringContent(JsonConvert.SerializeObject(request), System.Text.Encoding.UTF8, "application/json")
        };
        httpRequest.Headers.Add("Authorization", $"Bearer {_apiKey}");
        using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Groq request failed with status code {(int)response.StatusCode}.",
                null,
                response.StatusCode);
        }

        var result = JsonConvert.DeserializeObject<GroqResponse>(responseBody);
        var choice = result?.Choices?.FirstOrDefault();
        if (string.Equals(choice?.FinishReason, "length", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Groq truncated its response at the {maxTokens}-token output limit. Increase Groq:MaxTokens or shorten the requested analysis.");
        }

        var content = choice?.Message?.Content;
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new InvalidOperationException("Groq returned an empty completion.");
        }

        return content;
    }
}

public class GroqResponse
{
    [JsonProperty("choices")]
    public List<GroqChoice>? Choices { get; set; }
}

public class GroqChoice
{
    [JsonProperty("finish_reason")]
    public string? FinishReason { get; set; }

    [JsonProperty("message")]
    public GroqMessage? Message { get; set; }
}

public class GroqMessage
{
    [JsonProperty("content")]
    public string? Content { get; set; }
}
