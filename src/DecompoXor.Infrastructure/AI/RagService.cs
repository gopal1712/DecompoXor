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

    public async Task<string> GetContext(string query)
    {
        if (!_vectorStore.HasDocuments) return string.Empty;
        var queryEmbedding = await _embeddingService.GetEmbeddingAsync(query);
        var similar = _vectorStore.Search(queryEmbedding);
        return string.Join("\n\n", similar.Select(d => d.Content));
    }

    public async Task IndexDocument(string id, string content)
    {
        var embedding = await _embeddingService.GetEmbeddingAsync(content);
        _vectorStore.AddDocument(id, content, embedding);
    }
}
