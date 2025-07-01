using DocFlowHub.Core.Models.AI;
using DocFlowHub.Core.Models.Common;

namespace DocFlowHub.Core.Services.Interfaces;

/// <summary>
/// Service for managing user-specific AI configuration settings
/// </summary>
public interface IAISettingsService
{
    /// <summary>
    /// Get AI settings for a user, creating default settings if none exist
    /// </summary>
    Task<ServiceResult<AISettings>> GetUserSettingsAsync(string userId);
    
    /// <summary>
    /// Update user's AI settings
    /// </summary>
    Task<ServiceResult<AISettings>> UpdateUserSettingsAsync(string userId, AISettings settings);
    
    /// <summary>
    /// Create default AI settings for a new user
    /// </summary>
    Task<ServiceResult<AISettings>> CreateDefaultSettingsAsync(string userId);
    
    /// <summary>
    /// Reset user's settings to default values
    /// </summary>
    Task<ServiceResult<AISettings>> ResetToDefaultsAsync(string userId);
    
    /// <summary>
    /// Validate a custom API key by testing connectivity
    /// </summary>
    Task<ServiceResult<bool>> ValidateApiKeyAsync(string apiKey);
    
    /// <summary>
    /// Get available AI models for user selection
    /// </summary>
    Task<ServiceResult<List<AIModelInfo>>> GetAvailableModelsAsync();
    
    /// <summary>
    /// Get effective AI settings for operations (considers custom API key, preferences, etc.)
    /// </summary>
    Task<ServiceResult<EffectiveAISettings>> GetEffectiveSettingsAsync(string userId);
    
    /// <summary>
    /// Check if AI features are enabled for a user
    /// </summary>
    Task<ServiceResult<bool>> AreAIFeaturesEnabledAsync(string userId);
    
    /// <summary>
    /// Update specific setting without replacing entire settings object
    /// </summary>
    Task<ServiceResult> UpdateSettingAsync(string userId, string settingName, object value);
    
    /// <summary>
    /// Deactivate user's AI settings (soft delete)
    /// </summary>
    Task<ServiceResult> DeactivateSettingsAsync(string userId);
}

/// <summary>
/// Information about available AI models for UI display
/// </summary>
public class AIModelInfo
{
    public AIModel Model { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string CostLevel { get; set; } = string.Empty;
    public bool IsRecommended { get; set; }
    public int MaxTokens { get; set; }
}

/// <summary>
/// Effective settings after processing user preferences and system defaults
/// </summary>
public class EffectiveAISettings
{
    public AIModel ModelToUse { get; set; }
    public string? ApiKeyToUse { get; set; }
    public bool SummarizationEnabled { get; set; }
    public bool VersionComparisonEnabled { get; set; }
    public bool AutoSummarizeOnUpload { get; set; }
    public int MaxTokens { get; set; }
    public double QualityPreference { get; set; }
    public double ComparisonSensitivity { get; set; }
    public bool CachingEnabled { get; set; }
    public TimeSpan CacheDuration { get; set; }
    public string SettingsSource { get; set; } = string.Empty; // "User" or "Default"
} 