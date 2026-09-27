namespace StoryDecomposer.RAG;

public sealed class VectorStore
{
    private readonly object _sync = new();
    private readonly List<VectorDocument> _documents = new();

    public bool HasDocuments
    {
        get
        {
            lock (_sync)
            {
                return _documents.Count > 0;
            }
        }
    }
    
    public class VectorDocument
    {
        public string Id { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public float[] Embedding { get; set; } = Array.Empty<float>();
    }
    
    public void AddDocument(string id, string content, float[] embedding)
    {
        lock (_sync)
        { 
            _documents.Add(new VectorDocument
            {
                Id = id,
                Content = content,
                Embedding = embedding
            });
        }
    }
    
    // Cosine similarity search
    public List<VectorDocument> Search(float[] queryEmbedding, int topK = 3)
    {
        lock (_sync)
        {
            return _documents
                .OrderByDescending(doc => CosineSimilarity(queryEmbedding, doc.Embedding))
                .Take(topK)
                .ToList();
        }
    }
    
    private float CosineSimilarity(float[] a, float[] b)
    {
        float dotProduct = 0;
        float normA = 0;
        float normB = 0;
        
        for (int i = 0; i < a.Length; i++)
        {
            dotProduct += a[i] * b[i];
            normA += a[i] * a[i];
            normB += b[i] * b[i];
        }
        
        return dotProduct / (float)(Math.Sqrt(normA) * Math.Sqrt(normB));
    }
}
