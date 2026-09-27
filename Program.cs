var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddHttpClient<StoryDecomposer.Services.OllamaService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(
        Math.Max(1, builder.Configuration.GetValue<int>("Ollama:TimeoutSeconds", 300)));
});
builder.Services.AddHttpClient<StoryDecomposer.RAG.EmbeddingService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(
        Math.Max(1, builder.Configuration.GetValue<int>("Ollama:TimeoutSeconds", 300)));
});
builder.Services.AddHttpClient<StoryDecomposer.Services.GroqService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(
        Math.Max(1, builder.Configuration.GetValue<int>("Groq:TimeoutSeconds", 30)));
});
builder.Services.AddScoped<StoryDecomposer.Services.ILLMService>(services =>
    builder.Configuration.GetValue<string>("LLM:Provider")?.Equals("Groq", StringComparison.OrdinalIgnoreCase) == true
        ? services.GetRequiredService<StoryDecomposer.Services.GroqService>()
        : services.GetRequiredService<StoryDecomposer.Services.OllamaService>());
builder.Services.AddSingleton<StoryDecomposer.RAG.VectorStore>();
builder.Services.AddSingleton<StoryDecomposer.Services.RAGService>();
builder.Services.AddScoped<StoryDecomposer.Services.StoryDecompositionService>();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.MapControllers();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
