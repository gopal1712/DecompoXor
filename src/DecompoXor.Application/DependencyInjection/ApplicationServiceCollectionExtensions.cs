using Microsoft.Extensions.DependencyInjection;
using DecompoXor.Infrastructure.Abstractions.RAG;
using DecompoXor.Application.Features.StoryDecomposition;

namespace DecompoXor.Application.DependencyInjection;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Register application services only (concrete implementations are provided by Infrastructure)
        services.AddScoped<StoryDecompositionService>();
        return services;
    }
}
