namespace DecompoXor.Domain.Entities;

public sealed class DomainTask
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string AreaOfChange { get; set; } = string.Empty;
}