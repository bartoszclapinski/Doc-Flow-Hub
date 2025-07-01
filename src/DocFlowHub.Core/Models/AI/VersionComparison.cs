namespace DocFlowHub.Core.Models.AI;

/// <summary>
/// Represents AI analysis of changes between document versions
/// </summary>
public class VersionComparison
{
    public int Id { get; set; }
    
    /// <summary>
    /// The document ID this comparison belongs to
    /// </summary>
    public int DocumentId { get; set; }
    
    /// <summary>
    /// Source version ID being compared from
    /// </summary>
    public int FromVersionId { get; set; }
    
    /// <summary>
    /// Target version ID being compared to
    /// </summary>
    public int ToVersionId { get; set; }
    
    /// <summary>
    /// AI-generated high-level summary of changes
    /// </summary>
    public string ChangeSummary { get; set; } = string.Empty;
    
    /// <summary>
    /// Detailed breakdown of specific changes
    /// </summary>
    public string? DetailedChanges { get; set; }
    
    /// <summary>
    /// Type of changes detected
    /// </summary>
    public ChangeType ChangeType { get; set; }
    
    /// <summary>
    /// Overall significance level of changes
    /// </summary>
    public ChangeSignificance Significance { get; set; }
    
    /// <summary>
    /// AI model used for generating this comparison
    /// </summary>
    public string AIModel { get; set; } = string.Empty;
    
    /// <summary>
    /// Confidence score of the AI analysis (0.0 to 1.0)
    /// </summary>
    public double? ConfidenceScore { get; set; }
    
    /// <summary>
    /// When this comparison was generated
    /// </summary>
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Processing time in milliseconds for performance tracking
    /// </summary>
    public int? ProcessingTimeMs { get; set; }
    
    /// <summary>
    /// Number of tokens used in AI processing
    /// </summary>
    public int? TokensUsed { get; set; }
}

/// <summary>
/// Represents the type of changes detected between versions
/// </summary>
public enum ChangeType
{
    /// <summary>
    /// No significant changes detected
    /// </summary>
    None = 0,
    
    /// <summary>
    /// Content additions only
    /// </summary>
    ContentAddition = 1,
    
    /// <summary>
    /// Content deletions only
    /// </summary>
    ContentDeletion = 2,
    
    /// <summary>
    /// Content modifications
    /// </summary>
    ContentModification = 4,
    
    /// <summary>
    /// Structural changes (formatting, organization)
    /// </summary>
    StructuralChange = 8,
    
    /// <summary>
    /// Semantic changes (meaning, intent)
    /// </summary>
    SemanticChange = 16,
    
    /// <summary>
    /// Combination of multiple change types
    /// </summary>
    Mixed = ContentAddition | ContentDeletion | ContentModification | StructuralChange | SemanticChange
}

/// <summary>
/// Represents the significance level of changes between versions
/// </summary>
public enum ChangeSignificance
{
    /// <summary>
    /// Minor changes - typos, formatting
    /// </summary>
    Low,
    
    /// <summary>
    /// Moderate changes - content updates, additions
    /// </summary>
    Medium,
    
    /// <summary>
    /// Major changes - significant content overhaul
    /// </summary>
    High,
    
    /// <summary>
    /// Critical changes - complete rewrite or major restructuring
    /// </summary>
    Critical
} 