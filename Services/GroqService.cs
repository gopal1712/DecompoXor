using Newtonsoft.Json;
using Microsoft.Extensions.Logging;

namespace StoryDecomposer.Services;

public class GroqService : ILLMService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GroqService> _logger;
    private readonly IConfiguration _configuration;
    private readonly string _apiKey;

    public GroqService(HttpClient httpClient, ILogger<GroqService> logger, IConfiguration config)
    {
        _httpClient = httpClient;
        _logger = logger;
        _configuration = config;
        _apiKey = config["Groq:ApiKey"] ?? throw new InvalidOperationException("Groq:ApiKey is required when Groq is selected.");
    }

    public async Task<string> Generate(string prompt, string context = "")
    {
        var startTime = DateTime.UtcNow;

        try
        {
            var request = new
            {
                model = _configuration["Groq:Model"],
                messages = new[]
                {
                    new { role = "user", content = $"Context:\n{context}\n\nTask:\n{prompt}\n\nResponse:" }
                },
                temperature = _configuration.GetValue<double>("Groq:Temperature"),
                max_tokens = _configuration.GetValue<int>("Groq:MaxTokens")
            };

            var baseUrl = _configuration["Groq:BaseUrl"]!.TrimEnd('/');
            var httpRequest = new HttpRequestMessage(
                HttpMethod.Post,
                $"{baseUrl}{_configuration["Groq:ChatCompletionsPath"]}")
            {
                Content = new StringContent(
                    JsonConvert.SerializeObject(request),
                    System.Text.Encoding.UTF8,
                    "application/json")
            };

            httpRequest.Headers.Add("Authorization", $"Bearer {_apiKey}");

            var response = await _httpClient.SendAsync(httpRequest);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Groq error: {StatusCode}. Response: {ResponseBody}",
                    response.StatusCode,
                    responseBody);
                throw new Exception($"Groq error: {response.StatusCode}. {responseBody}");
            }

            var result = JsonConvert.DeserializeObject<GroqResponse>(responseBody);
            
            var elapsed = DateTime.UtcNow - startTime;
            _logger.LogInformation($"Groq response in {elapsed.TotalMilliseconds:F0}ms");
            
            return result?.Choices?.FirstOrDefault()?.Message?.Content ?? "{}";
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