using StoryDecomposer.RAG;

namespace StoryDecomposer.Services;

public class RAGService
{
    private readonly EmbeddingService _embeddingService;
    private readonly VectorStore _vectorStore;
    
    public RAGService(EmbeddingService embeddingService, VectorStore vectorStore)
    {
        _embeddingService = embeddingService;
        _vectorStore = vectorStore;
    }
    
    public async System.Threading.Tasks.Task<string> GetContext(string query)
    {
        if (!_vectorStore.HasDocuments)
        {
            return string.Empty;
        }

        // Get embedding for query
        var queryEmbedding = await _embeddingService.GetEmbeddingAsync(query);
        
        // Search similar documents
        var similarDocs = _vectorStore.Search(queryEmbedding);
        
        // Build context from search results
        return string.Join("\n\n", similarDocs.Select(d => d.Content));
    }
    
    public async System.Threading.Tasks.Task IndexDocument(string id, string content)
    {
        var embedding = await _embeddingService.GetEmbeddingAsync(content);
        _vectorStore.AddDocument(id, content, embedding);
    }
}
