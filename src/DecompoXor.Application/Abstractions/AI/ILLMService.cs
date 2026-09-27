namespace DecompoXor.Application.Abstractions.AI;

public interface ILLMService
{
    Task<string> Generate(string prompt, string context = "");
}
