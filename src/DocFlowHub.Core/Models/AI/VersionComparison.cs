namespace DocFlowHub.Core.Models.AI;

/// <summary>
/// Represents AI analysis of changes between document versions
/// </summary>
public class VersionComparison
{
    public int Id { get; set; }
    public int FromVersionId { get; set; }
    public int ToVersionId { get; set; }
    public string ChangeSummary { get; set; } = string.Empty;
    public string? DetailedChanges { get; set; }
    public ChangeSignificance Significance { get; set; }
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Represents the significance level of changes between versions
/// </summary>
public enum ChangeSignificance
{
    Low,
    Medium,
    High
} 