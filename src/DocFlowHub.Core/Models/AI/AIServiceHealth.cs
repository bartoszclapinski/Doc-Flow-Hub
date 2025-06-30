namespace DocFlowHub.Core.Models.AI;

/// <summary>
/// Represents the health status of AI service
/// </summary>
public class AIServiceHealth
{
    public bool IsHealthy { get; set; }
    public string Status { get; set; } = string.Empty;
    public TimeSpan ResponseTime { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CheckedAt { get; set; } = DateTime.UtcNow;
} 