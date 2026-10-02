using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
// AI abstractions now live in DecompoXor.Infrastructure.Abstractions.AI
using DecompoXor.Infrastructure.Abstractions.AI;
using DecompoXor.Infrastructure.Abstractions.RAG;
// The AI implementations are in DecompoXor.Infrastructure.AI.
using DecompoXor.Infrastructure.AI;

namespace DecompoXor.Infrastructure.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Register real AI implementations only.
        services.AddScoped<GroqService>();
        services.AddScoped<EmbeddingService>();

        // Resolve IChatCompletionService and IEmbeddingGenerationService directly to the real services.
        services.AddScoped<IChatCompletionService, GroqService>();
        services.AddScoped<IEmbeddingGenerationService, EmbeddingService>();

        // Register RAG infrastructure
        services.AddHttpClient<EmbeddingService>();
        services.AddSingleton<VectorStore>(); // unchanged, still the vector store implementation
        services.AddScoped<IRagService, RagService>();
        services.AddScoped<RagService, RagService>(); // concrete implementation

        return services;
    }
}
