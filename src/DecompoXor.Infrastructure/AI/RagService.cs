using DecompoXor.Infrastructure.Abstractions.RAG;
using DecompoXor.Infrastructure.Abstractions.AI;

namespace DecompoXor.Infrastructure.AI;

public sealed class RagService : IRagService
{
    private readonly IEmbeddingGenerationService _embeddingService;
    private readonly VectorStore _vectorStore;

    public RagService(IEmbeddingGenerationService embeddingService, VectorStore vectorStore)
    {
        _embeddingService = embeddingService;
        _vectorStore = vectorStore;
    }

    public async Task<string> GetContextAsync(string query, CancellationToken cancellationToken = default)
    {
        if (!_vectorStore.HasDocuments) return string.Empty;
        var queryEmbedding = await _embeddingService.GetEmbeddingAsync(query, cancellationToken);
        var similar = _vectorStore.Search(queryEmbedding);
        return string.Join("\n\n", similar.Select(d => d.Content));
    }

    public async Task IndexDocumentAsync(string id, string content, CancellationToken cancellationToken = default)
    {
        var embedding = await _embeddingService.GetEmbeddingAsync(content, cancellationToken);
        _vectorStore.AddDocument(id, content, embedding);
    }
}
