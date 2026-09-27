using System.Collections.Generic;
using System.Linq;

namespace DecompoXor.Infrastructure.AI;

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
            _documents.Add(new VectorDocument { Id = id, Content = content, Embedding = embedding });
        }
    }

    public List<VectorDocument> Search(float[] queryEmbedding, int topK = 3)
    {
        lock (_sync)
        {
            return _documents
                .OrderByDescending(d => CosineSimilarity(queryEmbedding, d.Embedding))
                .Take(topK)
                .ToList();
        }
    }

    private static float CosineSimilarity(float[] a, float[] b)
    {
        float dot = 0, normA = 0, normB = 0;
        for (int i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            normA += a[i] * a[i];
            normB += b[i] * b[i];
        }
        return dot / (float)(System.Math.Sqrt(normA) * System.Math.Sqrt(normB));
    }
}
