using System.Collections.Generic;

namespace DecompoXor.Domain.Entities;

public sealed class DecompositionResult
{
    public List<DomainTask> Tasks { get; set; } = new();
    public List<string> Questions { get; set; } = new();
    public List<string> Reasoning { get; set; } = new();
    public int EstimatedTotalStoryPoints { get; set; }
}