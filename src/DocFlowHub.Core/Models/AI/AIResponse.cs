namespace DocFlowHub.Core.Models.AI;

/// <summary>
/// Represents a response from AI service
/// </summary>
public class AIResponse
{
    public string Content { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int TokensUsed { get; set; }
    public TimeSpan ResponseTime { get; set; }
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
} 