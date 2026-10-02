using System.Threading.Tasks;

namespace DecompoXor.Infrastructure.Abstractions.RAG;

public interface IRagService
{
    Task<string> GetContextAsync(string query, CancellationToken cancellationToken = default);
    Task IndexDocumentAsync(string id, string content, CancellationToken cancellationToken = default);
}
