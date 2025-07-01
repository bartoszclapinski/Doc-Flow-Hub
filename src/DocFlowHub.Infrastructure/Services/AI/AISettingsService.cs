using DocFlowHub.Core.Models.AI;
using DocFlowHub.Core.Models.Common;
using DocFlowHub.Core.Services.Interfaces;
using DocFlowHub.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;

namespace DocFlowHub.Infrastructure.Services.AI;

/// <summary>
/// Service for managing user-specific AI configuration settings
/// </summary>
public class AISettingsService : IAISettingsService
{
    private readonly ApplicationDbContext _context;
    private readonly IAIService _aiService;
    private readonly ILogger<AISettingsService> _logger;
    private readonly IMemoryCache _memoryCache;
    
    // Cache keys and expiration times
    private static readonly TimeSpan SettingsCacheExpiration = TimeSpan.FromHours(6);
    private static readonly TimeSpan ModelsCacheExpiration = TimeSpan.FromDays(1);
    
    public AISettingsService(
        ApplicationDbContext context,
        IAIService aiService,
        ILogger<AISettingsService> logger,
        IMemoryCache memoryCache)
    {
        _context = context;
        _aiService = aiService;
        _logger = logger;
        _memoryCache = memoryCache;
    }

    /// <summary>
    /// Get AI settings for a user, creating default settings if none exist
    /// </summary>
    public async Task<ServiceResult<AISettings>> GetUserSettingsAsync(string userId)
    {
        try
        {
            if (string.IsNullOrEmpty(userId))
            {
                return ServiceResult<AISettings>.Failure("User ID is required");
            }

            // Check cache first
            var cacheKey = GetSettingsCacheKey(userId);
            if (_memoryCache.TryGetValue(cacheKey, out AISettings? cachedSettings) && cachedSettings != null)
            {
                _logger.LogDebug("Retrieved AI settings from cache for user: {UserId}", userId);
                return ServiceResult<AISettings>.Success(cachedSettings);
            }

            // Try to get existing settings from database
            var settings = await _context.AISettings
                .FirstOrDefaultAsync(s => s.UserId == userId && s.IsActive);

            if (settings == null)
            {
                // Create default settings for new user
                _logger.LogInformation("No AI settings found for user {UserId}, creating defaults", userId);
                var defaultResult = await CreateDefaultSettingsAsync(userId);
                if (!defaultResult.Succeeded)
                {
                    return ServiceResult<AISettings>.Failure(defaultResult.Error ?? "Failed to create default settings");
                }
                settings = defaultResult.Data!;
            }

            // Cache the result
            _memoryCache.Set(cacheKey, settings, SettingsCacheExpiration);
            _logger.LogDebug("Cached AI settings for user: {UserId}", userId);

            return ServiceResult<AISettings>.Success(settings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving AI settings for user: {UserId}", userId);
            return ServiceResult<AISettings>.Failure("Failed to retrieve AI settings");
        }
    }

    /// <summary>
    /// Update user's AI settings
    /// </summary>
    public async Task<ServiceResult<AISettings>> UpdateUserSettingsAsync(string userId, AISettings settings)
    {
        try
        {
            if (string.IsNullOrEmpty(userId))
            {
                return ServiceResult<AISettings>.Failure("User ID is required");
            }

            if (settings == null)
            {
                return ServiceResult<AISettings>.Failure("Settings object is required");
            }

            // Validate settings
            if (!AISettingsHelper.IsValid(settings))
            {
                return ServiceResult<AISettings>.Failure("Invalid settings values provided");
            }

            _logger.LogInformation("Updating AI settings for user: {UserId}", userId);

            // Get existing settings or create new
            var existingSettings = await _context.AISettings
                .FirstOrDefaultAsync(s => s.UserId == userId && s.IsActive);

            if (existingSettings == null)
            {
                // Create new settings record
                settings.Id = 0; // Ensure new record
                settings.UserId = userId;
                settings.CreatedAt = DateTime.UtcNow;
                settings.UpdatedAt = DateTime.UtcNow;
                settings.IsActive = true;

                _context.AISettings.Add(settings);
                _logger.LogDebug("Creating new AI settings for user: {UserId}", userId);
            }
            else
            {
                // Update existing settings
                existingSettings.PreferredModel = settings.PreferredModel;
                existingSettings.CustomApiKey = settings.CustomApiKey;
                existingSettings.UseCustomApiKey = settings.UseCustomApiKey;
                existingSettings.SummarizationEnabled = settings.SummarizationEnabled;
                existingSettings.VersionComparisonEnabled = settings.VersionComparisonEnabled;
                existingSettings.AutoSummarizeOnUpload = settings.AutoSummarizeOnUpload;
                existingSettings.QualityPreference = settings.QualityPreference;
                existingSettings.MaxTokensPerOperation = settings.MaxTokensPerOperation;
                existingSettings.ComparisonSensitivity = settings.ComparisonSensitivity;
                existingSettings.EnableCaching = settings.EnableCaching;
                existingSettings.CacheDurationHours = settings.CacheDurationHours;
                existingSettings.UpdatedAt = DateTime.UtcNow;

                settings = existingSettings; // Return the updated entity
                _logger.LogDebug("Updated existing AI settings for user: {UserId}", userId);
            }

            await _context.SaveChangesAsync();

            // Invalidate cache
            InvalidateSettingsCache(userId);
            
            // Cache the updated settings
            var cacheKey = GetSettingsCacheKey(userId);
            _memoryCache.Set(cacheKey, settings, SettingsCacheExpiration);

            _logger.LogInformation("Successfully updated AI settings for user: {UserId}", userId);
            return ServiceResult<AISettings>.Success(settings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating AI settings for user: {UserId}", userId);
            return ServiceResult<AISettings>.Failure("Failed to update AI settings");
        }
    }

    /// <summary>
    /// Create default AI settings for a new user
    /// </summary>
    public async Task<ServiceResult<AISettings>> CreateDefaultSettingsAsync(string userId)
    {
        try
        {
            if (string.IsNullOrEmpty(userId))
            {
                return ServiceResult<AISettings>.Failure("User ID is required");
            }

            _logger.LogInformation("Creating default AI settings for user: {UserId}", userId);

            // Check if settings already exist
            var existingSettings = await _context.AISettings
                .FirstOrDefaultAsync(s => s.UserId == userId);

            if (existingSettings != null)
            {
                _logger.LogWarning("AI settings already exist for user: {UserId}", userId);
                return ServiceResult<AISettings>.Success(existingSettings);
            }

            // Create default settings
            var defaultSettings = AISettingsHelper.GetDefaultSettings(userId);
            
            _context.AISettings.Add(defaultSettings);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Successfully created default AI settings for user: {UserId}", userId);
            return ServiceResult<AISettings>.Success(defaultSettings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating default AI settings for user: {UserId}", userId);
            return ServiceResult<AISettings>.Failure("Failed to create default AI settings");
        }
    }

    /// <summary>
    /// Reset user's settings to default values
    /// </summary>
    public async Task<ServiceResult<AISettings>> ResetToDefaultsAsync(string userId)
    {
        try
        {
            if (string.IsNullOrEmpty(userId))
            {
                return ServiceResult<AISettings>.Failure("User ID is required");
            }

            _logger.LogInformation("Resetting AI settings to defaults for user: {UserId}", userId);

            // Get default settings
            var defaultSettings = AISettingsHelper.GetDefaultSettings(userId);
            
            // Update using the standard update method
            var result = await UpdateUserSettingsAsync(userId, defaultSettings);
            
            if (result.Succeeded)
            {
                _logger.LogInformation("Successfully reset AI settings to defaults for user: {UserId}", userId);
            }
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting AI settings for user: {UserId}", userId);
            return ServiceResult<AISettings>.Failure("Failed to reset AI settings");
        }
    }

    /// <summary>
    /// Validate a custom API key by testing connectivity
    /// </summary>
    public async Task<ServiceResult<bool>> ValidateApiKeyAsync(string apiKey)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                return ServiceResult<bool>.Failure("API key is required");
            }

            _logger.LogInformation("Validating custom API key");

            // Test the API key by checking service health
            var healthResult = await _aiService.GetHealthAsync();
            
            if (healthResult.IsHealthy)
            {
                _logger.LogInformation("API key validation successful");
                return ServiceResult<bool>.Success(true);
            }
            else
            {
                _logger.LogWarning("API key validation failed: {Error}", healthResult.ErrorMessage);
                return ServiceResult<bool>.Success(false);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating API key");
            return ServiceResult<bool>.Failure("Failed to validate API key");
        }
    }

    /// <summary>
    /// Get available AI models for user selection
    /// </summary>
    public async Task<ServiceResult<List<AIModelInfo>>> GetAvailableModelsAsync()
    {
        try
        {
            // Check cache first
            const string cacheKey = "available_ai_models";
            if (_memoryCache.TryGetValue(cacheKey, out List<AIModelInfo>? cachedModels) && cachedModels != null)
            {
                _logger.LogDebug("Retrieved available AI models from cache");
                return ServiceResult<List<AIModelInfo>>.Success(cachedModels);
            }

            _logger.LogDebug("Building available AI models list");

            var models = new List<AIModelInfo>();
            
            foreach (var model in AIModelHelper.GetAllModels())
            {
                models.Add(new AIModelInfo
                {
                    Model = model,
                    DisplayName = model.ToDisplayName(),
                    Description = model.GetCostDescription(),
                    CostLevel = GetCostLevel(model),
                    IsRecommended = model == AIModel.Gpt4oMini,
                    MaxTokens = GetMaxTokens(model)
                });
            }

            // Cache the result
            _memoryCache.Set(cacheKey, models, ModelsCacheExpiration);
            _logger.LogDebug("Cached available AI models");

            return ServiceResult<List<AIModelInfo>>.Success(models);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving available AI models");
            return ServiceResult<List<AIModelInfo>>.Failure("Failed to retrieve available models");
        }
    }

    /// <summary>
    /// Get effective AI settings for operations
    /// </summary>
    public async Task<ServiceResult<EffectiveAISettings>> GetEffectiveSettingsAsync(string userId)
    {
        try
        {
            var settingsResult = await GetUserSettingsAsync(userId);
            if (!settingsResult.Succeeded)
            {
                return ServiceResult<EffectiveAISettings>.Failure(settingsResult.Error);
            }

            var settings = settingsResult.Data!;
            
            var effectiveSettings = new EffectiveAISettings
            {
                ModelToUse = settings.PreferredModel,
                ApiKeyToUse = settings.UseCustomApiKey ? settings.CustomApiKey : null,
                SummarizationEnabled = settings.SummarizationEnabled,
                VersionComparisonEnabled = settings.VersionComparisonEnabled,
                AutoSummarizeOnUpload = settings.AutoSummarizeOnUpload,
                MaxTokens = settings.MaxTokensPerOperation,
                QualityPreference = settings.QualityPreference,
                ComparisonSensitivity = settings.ComparisonSensitivity,
                CachingEnabled = settings.EnableCaching,
                CacheDuration = TimeSpan.FromHours(settings.CacheDurationHours),
                SettingsSource = "User"
            };

            return ServiceResult<EffectiveAISettings>.Success(effectiveSettings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting effective AI settings for user: {UserId}", userId);
            return ServiceResult<EffectiveAISettings>.Failure("Failed to get effective settings");
        }
    }

    /// <summary>
    /// Check if AI features are enabled for a user
    /// </summary>
    public async Task<ServiceResult<bool>> AreAIFeaturesEnabledAsync(string userId)
    {
        try
        {
            var settingsResult = await GetUserSettingsAsync(userId);
            if (!settingsResult.Succeeded)
            {
                return ServiceResult<bool>.Failure(settingsResult.Error);
            }

            var settings = settingsResult.Data!;
            var enabled = settings.IsActive && (settings.SummarizationEnabled || settings.VersionComparisonEnabled);
            
            return ServiceResult<bool>.Success(enabled);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking AI features status for user: {UserId}", userId);
            return ServiceResult<bool>.Failure("Failed to check AI features status");
        }
    }

    /// <summary>
    /// Update specific setting without replacing entire settings object
    /// </summary>
    public async Task<ServiceResult> UpdateSettingAsync(string userId, string settingName, object value)
    {
        try
        {
            var settingsResult = await GetUserSettingsAsync(userId);
            if (!settingsResult.Succeeded)
            {
                return ServiceResult.Failure(settingsResult.Error);
            }

            var settings = settingsResult.Data!;
            
            // Update specific property based on setting name
            switch (settingName.ToLowerInvariant())
            {
                case "preferredmodel":
                    if (value is AIModel model) settings.PreferredModel = model;
                    break;
                case "summarizationenabled":
                    if (value is bool summarizationEnabled) settings.SummarizationEnabled = summarizationEnabled;
                    break;
                case "versioncomparisonenabled":
                    if (value is bool versionEnabled) settings.VersionComparisonEnabled = versionEnabled;
                    break;
                case "autosummarizeonupload":
                    if (value is bool autoSummarize) settings.AutoSummarizeOnUpload = autoSummarize;
                    break;
                case "qualitypreference":
                    if (value is double quality) settings.QualityPreference = quality;
                    break;
                case "maxtokensperoperation":
                    if (value is int maxTokens) settings.MaxTokensPerOperation = maxTokens;
                    break;
                default:
                    return ServiceResult.Failure($"Unknown setting: {settingName}");
            }

            var updateResult = await UpdateUserSettingsAsync(userId, settings);
            return updateResult.Succeeded ? ServiceResult.Success() : ServiceResult.Failure(updateResult.Error);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating setting {SettingName} for user: {UserId}", settingName, userId);
            return ServiceResult.Failure("Failed to update setting");
        }
    }

    /// <summary>
    /// Deactivate user's AI settings (soft delete)
    /// </summary>
    public async Task<ServiceResult> DeactivateSettingsAsync(string userId)
    {
        try
        {
            var settings = await _context.AISettings
                .FirstOrDefaultAsync(s => s.UserId == userId && s.IsActive);

            if (settings == null)
            {
                return ServiceResult.Success(); // Already deactivated or doesn't exist
            }

            settings.IsActive = false;
            settings.UpdatedAt = DateTime.UtcNow;
            
            await _context.SaveChangesAsync();
            
            // Invalidate cache
            InvalidateSettingsCache(userId);
            
            _logger.LogInformation("Deactivated AI settings for user: {UserId}", userId);
            return ServiceResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating AI settings for user: {UserId}", userId);
            return ServiceResult.Failure("Failed to deactivate AI settings");
        }
    }

    // Private helper methods

    private static string GetSettingsCacheKey(string userId) => $"ai_settings_{userId}";

    private void InvalidateSettingsCache(string userId)
    {
        var cacheKey = GetSettingsCacheKey(userId);
        _memoryCache.Remove(cacheKey);
        _logger.LogDebug("Invalidated AI settings cache for user: {UserId}", userId);
    }

    private static string GetCostLevel(AIModel model) => model switch
    {
        AIModel.Gpt4oMini => "Low",
        AIModel.Gpt4o => "Medium",
        AIModel.Gpt41Mini => "Medium",
        AIModel.Gpt41 => "High",
        _ => "Medium"
    };

    private static int GetMaxTokens(AIModel model) => model switch
    {
        AIModel.Gpt4oMini => 16000,
        AIModel.Gpt4o => 8000,
        AIModel.Gpt41Mini => 16000,
        AIModel.Gpt41 => 8000,
        _ => 8000
    };
} 