namespace DecompoXor.Infrastructure.Abstractions.AI;

public interface IEmbeddingGenerationService
{
    Task<float[]> GetEmbeddingAsync(string text, CancellationToken cancellationToken = default);
}
