using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using DecompoXor.Application.Abstractions.AI;
using DecompoXor.Application.Abstractions.RAG;
using DecompoXor.Infrastructure.AI;

namespace DecompoXor.Infrastructure.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Register concrete AI/LLM services (both implementations are added as HttpClient factories)
        services.AddHttpClient<OllamaService>();
        services.AddHttpClient<GroqService>();
        // Provider selection logic mirrors the original Program.cs behavior
        services.AddScoped<ILLMService>(svc =>
        {
            var cfg = svc.GetRequiredService<IConfiguration>();
            var useGroq = cfg.GetValue<string>("LLM:Provider")?.Equals("Groq", StringComparison.OrdinalIgnoreCase) == true;
            return useGroq ? svc.GetRequiredService<GroqService>() : svc.GetRequiredService<OllamaService>();
        });

        // Register RAG infrastructure
        services.AddHttpClient<EmbeddingService>();
        services.AddSingleton<VectorStore>();
        services.AddScoped<IRagService, RagService>();
        services.AddScoped<RagService, RagService>(); // concrete implementation

        return services;
    }
}
