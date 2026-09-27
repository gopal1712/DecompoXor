using StoryDecomposer.Models;

namespace StoryDecomposer.Services;

public interface ILLMService
{
    Task<string> Generate(string prompt, string context = "");

    
}
