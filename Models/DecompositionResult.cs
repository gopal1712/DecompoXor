namespace StoryDecomposer.Models;

public sealed class DecompositionResult
{
    public List<Task> Tasks { get; set; } = [];
    public List<string> Questions { get; set; } = [];
    public List<string> Reasoning { get; set; } = [];
    public int EstimatedTotalStoryPoints { get; set; }

}
