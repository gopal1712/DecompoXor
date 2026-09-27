using System.Threading.Tasks;

namespace DecompoXor.Application.Abstractions.RAG;

public interface IRagService
{
    Task<string> GetContext(string query);
    Task IndexDocument(string id, string content);
}