using DocFlowHub.Core.Models.Admin.Dto;
using DocFlowHub.Core.Models.Common;

namespace DocFlowHub.Core.Services.Interfaces;

/// <summary>
/// Service interface for managing system settings
/// </summary>
public interface ISystemSettingsService
{
    /// <summary>
    /// Get all system settings grouped by category
    /// </summary>
    Task<ServiceResult<List<SystemSettingsGroupDto>>> GetSettingsGroupedByCategoryAsync();

    /// <summary>
    /// Get settings for a specific category
    /// </summary>
    Task<ServiceResult<List<SystemSettingsDto>>> GetSettingsByCategoryAsync(string category);

    /// <summary>
    /// Get a specific setting by key
    /// </summary>
    Task<ServiceResult<SystemSettingsDto?>> GetSettingByKeyAsync(string key);

    /// <summary>
    /// Get a setting value by key with type conversion
    /// </summary>
    Task<ServiceResult<T?>> GetSettingValueAsync<T>(string key);

    /// <summary>
    /// Update multiple settings
    /// </summary>
    Task<ServiceResult<bool>> UpdateSettingsAsync(UpdateSystemSettingsRequest request, string updatedBy);

    /// <summary>
    /// Update a single setting
    /// </summary>
    Task<ServiceResult<bool>> UpdateSettingAsync(string key, string value, string updatedBy);

    /// <summary>
    /// Reset a setting to its default value
    /// </summary>
    Task<ServiceResult<bool>> ResetSettingToDefaultAsync(string key, string updatedBy);

    /// <summary>
    /// Reset all settings in a category to default values
    /// </summary>
    Task<ServiceResult<bool>> ResetCategoryToDefaultAsync(string category, string updatedBy);

    /// <summary>
    /// Get all settings that require application restart
    /// </summary>
    Task<ServiceResult<List<SystemSettingsDto>>> GetRestartRequiredSettingsAsync();

    /// <summary>
    /// Validate setting value based on data type
    /// </summary>
    ServiceResult<bool> ValidateSettingValue(string dataType, string value);

    /// <summary>
    /// Get sensitive settings (masked values)
    /// </summary>
    Task<ServiceResult<List<SystemSettingsDto>>> GetSensitiveSettingsAsync();
} 