using Newtonsoft.Json;
using DecompoXor.Infrastructure.Abstractions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using DecompoXor.Infrastructure.Abstractions.AI;

namespace DecompoXor.Infrastructure.AI;

public sealed class GroqService : IChatCompletionService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GroqService> _logger;
    private readonly IConfiguration _configuration;
    private readonly string _apiKey;

    public GroqService(HttpClient httpClient, ILogger<GroqService> logger, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _logger = logger;
        _configuration = configuration;
        _apiKey = configuration["Groq:ApiKey"] ?? throw new InvalidOperationException("Groq:ApiKey is required when Groq is selected.");
    }

    public async Task<string> GenerateAsync(string prompt, CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        try
        {
            var request = new
            {
                model = _configuration["Groq:Model"],
                messages = new[]
                {
                    new { role = "user", content = $"Task:\n{prompt}\n\nResponse:" }
                },
                temperature = _configuration.GetValue<double>("Groq:Temperature"),
                max_tokens = _configuration.GetValue<int>("Groq:MaxTokens")
            };

            var baseUrl = _configuration["Groq:BaseUrl"]!.TrimEnd('/');
            var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}{_configuration["Groq:ChatCompletionsPath"]}")
            {
                Content = new StringContent(JsonConvert.SerializeObject(request), System.Text.Encoding.UTF8, "application/json")
            };
            httpRequest.Headers.Add("Authorization", $"Bearer {_apiKey}");
            var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Groq error: {StatusCode}. Response: {ResponseBody}", response.StatusCode, responseBody);
                throw new Exception($"Groq error: {response.StatusCode}. {responseBody}");
            }
            var result = JsonConvert.DeserializeObject<GroqResponse>(responseBody);
            var elapsed = DateTime.UtcNow - startTime;
            _logger.LogInformation($"Groq response in {elapsed.TotalMilliseconds:F0}ms");
            return result?.Choices?.FirstOrDefault()?.Message?.Content ?? string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Groq Error: {ex.Message}");
            throw;
        }
    }
}

public class GroqResponse
{
    [JsonProperty("choices")]
    public List<GroqChoice>? Choices { get; set; }
}

public class GroqChoice
{
    [JsonProperty("message")]
    public GroqMessage? Message { get; set; }
}

public class GroqMessage
{
    [JsonProperty("content")]
    public string? Content { get; set; }
}
