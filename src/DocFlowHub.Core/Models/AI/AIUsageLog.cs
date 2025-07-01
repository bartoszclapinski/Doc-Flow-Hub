using DocFlowHub.Core.Models.AI;

namespace DocFlowHub.Core.Models.AI;

/// <summary>
/// Represents a log entry for AI service usage tracking
/// Used for analytics, cost monitoring, and performance optimization
/// </summary>
public class AIUsageLog
{
    /// <summary>
    /// Unique identifier for the usage log entry
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// User ID who initiated the AI operation
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Type of AI operation performed (Summarization, VersionComparison, etc.)
    /// </summary>
    public string OperationType { get; set; } = string.Empty;

    /// <summary>
    /// AI model used for the operation
    /// </summary>
    public string AIModel { get; set; } = string.Empty;

    /// <summary>
    /// Number of tokens consumed by the operation
    /// </summary>
    public int TokensUsed { get; set; }

    /// <summary>
    /// Estimated cost of the operation in USD
    /// </summary>
    public decimal EstimatedCost { get; set; }

    /// <summary>
    /// Time taken for the operation to complete
    /// </summary>
    public TimeSpan ResponseTime { get; set; }

    /// <summary>
    /// Whether the operation was successful
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Error message if the operation failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// When the operation was initiated
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// ID of the document involved in the operation (if applicable)
    /// </summary>
    public int? DocumentId { get; set; }

    /// <summary>
    /// Size of the input content in characters
    /// </summary>
    public int InputSize { get; set; }

    /// <summary>
    /// Size of the output content in characters
    /// </summary>
    public int OutputSize { get; set; }

    /// <summary>
    /// Whether the response was served from cache
    /// </summary>
    public bool ServedFromCache { get; set; }

    /// <summary>
    /// IP address of the user (for usage analysis)
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// User agent string for tracking client types
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// Quality/temperature setting used for the operation
    /// </summary>
    public double? QualitySetting { get; set; }

    /// <summary>
    /// Additional metadata as JSON string
    /// </summary>
    public string? Metadata { get; set; }
} 