using System.ComponentModel.DataAnnotations;
using DocFlowHub.Core.Identity;

namespace DocFlowHub.Core.Models.AI;

/// <summary>
/// User-specific AI configuration settings
/// </summary>
public class AISettings
{
    public int Id { get; set; }
    
    /// <summary>
    /// User ID this settings belongs to
    /// </summary>
    [Required]
    public string UserId { get; set; } = string.Empty;
    
    /// <summary>
    /// Navigation property to the user
    /// </summary>
    public virtual ApplicationUser User { get; set; } = null!;
    
    /// <summary>
    /// Preferred AI model for document processing
    /// </summary>
    public AIModel PreferredModel { get; set; } = AIModel.Gpt4oMini;
    
    /// <summary>
    /// Custom API key for OpenAI (if user wants to use their own)
    /// </summary>
    [StringLength(200)]
    public string? CustomApiKey { get; set; }
    
    /// <summary>
    /// Whether to use custom API key instead of system default
    /// </summary>
    public bool UseCustomApiKey { get; set; } = false;
    
    /// <summary>
    /// Whether AI summarization is enabled for this user
    /// </summary>
    public bool SummarizationEnabled { get; set; } = true;
    
    /// <summary>
    /// Whether version comparison AI features are enabled
    /// </summary>
    public bool VersionComparisonEnabled { get; set; } = true;
    
    /// <summary>
    /// Whether to automatically generate summaries on document upload
    /// </summary>
    public bool AutoSummarizeOnUpload { get; set; } = true;
    
    /// <summary>
    /// Quality vs Speed preference (0.0 = Speed, 1.0 = Quality)
    /// </summary>
    [Range(0.0, 1.0)]
    public double QualityPreference { get; set; } = 0.7;
    
    /// <summary>
    /// Maximum tokens to use for AI operations (budget control)
    /// </summary>
    [Range(100, 10000)]
    public int MaxTokensPerOperation { get; set; } = 2000;
    
    /// <summary>
    /// Sensitivity level for version comparison (0.0 = Less sensitive, 1.0 = More sensitive)
    /// </summary>
    [Range(0.0, 1.0)]
    public double ComparisonSensitivity { get; set; } = 0.5;
    
    /// <summary>
    /// Whether to cache AI responses for this user
    /// </summary>
    public bool EnableCaching { get; set; } = true;
    
    /// <summary>
    /// Cache duration in hours (1-168 = 1 hour to 1 week)
    /// </summary>
    [Range(1, 168)]
    public int CacheDurationHours { get; set; } = 24;
    
    /// <summary>
    /// When these settings were created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// When these settings were last updated
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
    
    /// <summary>
    /// Whether these settings are active
    /// </summary>
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Quality preference levels for easier UI selection
/// </summary>
public enum QualityLevel
{
    Speed = 0,
    Balanced = 1,
    Quality = 2
}

/// <summary>
/// Helper methods for AI settings
/// </summary>
public static class AISettingsHelper
{
    /// <summary>
    /// Convert quality preference to user-friendly display
    /// </summary>
    public static string GetQualityDisplayName(double qualityPreference) => qualityPreference switch
    {
        <= 0.3 => "Speed Optimized",
        <= 0.7 => "Balanced",
        _ => "Quality Optimized"
    };
    
    /// <summary>
    /// Get quality level enum from preference value
    /// </summary>
    public static QualityLevel GetQualityLevel(double qualityPreference) => qualityPreference switch
    {
        <= 0.3 => QualityLevel.Speed,
        <= 0.7 => QualityLevel.Balanced,
        _ => QualityLevel.Quality
    };
    
    /// <summary>
    /// Convert quality level to preference value
    /// </summary>
    public static double GetQualityPreference(QualityLevel level) => level switch
    {
        QualityLevel.Speed => 0.2,
        QualityLevel.Balanced => 0.5,
        QualityLevel.Quality => 0.9,
        _ => 0.5
    };
    
    /// <summary>
    /// Get default settings for a new user
    /// </summary>
    public static AISettings GetDefaultSettings(string userId) => new()
    {
        UserId = userId,
        PreferredModel = AIModel.Gpt4oMini,
        UseCustomApiKey = false,
        SummarizationEnabled = true,
        VersionComparisonEnabled = true,
        AutoSummarizeOnUpload = true,
        QualityPreference = 0.7,
        MaxTokensPerOperation = 2000,
        ComparisonSensitivity = 0.5,
        EnableCaching = true,
        CacheDurationHours = 24,
        IsActive = true
    };
    
    /// <summary>
    /// Validate AI settings for consistency
    /// </summary>
    public static bool IsValid(AISettings settings)
    {
        if (string.IsNullOrEmpty(settings.UserId)) return false;
        if (settings.QualityPreference < 0 || settings.QualityPreference > 1) return false;
        if (settings.ComparisonSensitivity < 0 || settings.ComparisonSensitivity > 1) return false;
        if (settings.MaxTokensPerOperation < 100 || settings.MaxTokensPerOperation > 10000) return false;
        if (settings.CacheDurationHours < 1 || settings.CacheDurationHours > 168) return false;
        
        return true;
    }
} 