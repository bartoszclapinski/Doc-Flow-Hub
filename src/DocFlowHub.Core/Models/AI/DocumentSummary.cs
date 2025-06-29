namespace DocFlowHub.Core.Models.AI;

/// <summary>
/// Represents an AI-generated document summary
/// </summary>
public class DocumentSummary
{
    public int Id { get; set; }
    public int DocumentId { get; set; }
    public string Summary { get; set; } = string.Empty;
    public string? KeyPoints { get; set; }
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public string AIModel { get; set; } = string.Empty;
    public double? ConfidenceScore { get; set; }
} 