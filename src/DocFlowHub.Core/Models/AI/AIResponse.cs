namespace DocFlowHub.Core.Models.AI;

/// <summary>
/// Represents a response from AI service
/// </summary>
public class AIResponse
{
    public string Content { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int TokensUsed { get; set; }
    /// <summary>Prompt/input tokens, when the provider reports them (0 otherwise).</summary>
    public int InputTokens { get; set; }
    /// <summary>Completion/output tokens, when the provider reports them (0 otherwise).</summary>
    public int OutputTokens { get; set; }
    public TimeSpan ResponseTime { get; set; }
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
} 