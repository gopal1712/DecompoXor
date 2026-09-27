namespace DecompoXor.Infrastructure.Abstractions.AI;

public interface IChatCompletionService
{
    Task<string> GenerateAsync(string prompt, CancellationToken cancellationToken = default);
}
