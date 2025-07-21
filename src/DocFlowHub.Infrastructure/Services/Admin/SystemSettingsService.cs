using System.Globalization;
using System.Text.Json;
using DocFlowHub.Core.Models.Admin;
using DocFlowHub.Core.Models.Admin.Dto;
using DocFlowHub.Core.Models.Common;
using DocFlowHub.Core.Services.Interfaces;
using DocFlowHub.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DocFlowHub.Infrastructure.Services.Admin;

public class SystemSettingsService : ISystemSettingsService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<SystemSettingsService> _logger;

    public SystemSettingsService(ApplicationDbContext context, ILogger<SystemSettingsService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ServiceResult<List<SystemSettingsGroupDto>>> GetSettingsGroupedByCategoryAsync()
    {
        try
        {
            var settings = await _context.SystemSettings
                .OrderBy(s => s.Category)
                .ThenBy(s => s.Key)
                .ToListAsync();

            var groups = settings
                .GroupBy(s => s.Category)
                .Select(g => new SystemSettingsGroupDto
                {
                    Category = g.Key,
                    Settings = g.Select(ConvertToDto).ToList()
                })
                .ToList();

            return ServiceResult<List<SystemSettingsGroupDto>>.Success(groups);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting settings grouped by category");
            return ServiceResult<List<SystemSettingsGroupDto>>.Failure("Failed to retrieve system settings");
        }
    }

    public async Task<ServiceResult<List<SystemSettingsDto>>> GetSettingsByCategoryAsync(string category)
    {
        try
        {
            var settings = await _context.SystemSettings
                .Where(s => s.Category == category)
                .OrderBy(s => s.Key)
                .ToListAsync();

            var dtos = settings.Select(ConvertToDto).ToList();
            return ServiceResult<List<SystemSettingsDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting settings for category {Category}", category);
            return ServiceResult<List<SystemSettingsDto>>.Failure($"Failed to retrieve settings for category: {category}");
        }
    }

    public async Task<ServiceResult<SystemSettingsDto?>> GetSettingByKeyAsync(string key)
    {
        try
        {
            var setting = await _context.SystemSettings
                .FirstOrDefaultAsync(s => s.Key == key);

            if (setting == null)
            {
                return ServiceResult<SystemSettingsDto?>.Success(null);
            }

            return ServiceResult<SystemSettingsDto?>.Success(ConvertToDto(setting));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting setting by key {Key}", key);
            return ServiceResult<SystemSettingsDto?>.Failure($"Failed to retrieve setting: {key}");
        }
    }

    public async Task<ServiceResult<T?>> GetSettingValueAsync<T>(string key)
    {
        try
        {
            var setting = await _context.SystemSettings
                .FirstOrDefaultAsync(s => s.Key == key);

            if (setting == null)
            {
                return ServiceResult<T?>.Success(default(T));
            }

            var convertedValue = ConvertValue<T>(setting.Value, setting.DataType);
            return ServiceResult<T?>.Success(convertedValue);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting setting value for key {Key}", key);
            return ServiceResult<T?>.Failure($"Failed to retrieve setting value: {key}");
        }
    }

    public async Task<ServiceResult<bool>> UpdateSettingsAsync(UpdateSystemSettingsRequest request, string updatedBy)
    {
        try
        {
            var keys = request.Settings.Keys.ToList();
            var existingSettings = await _context.SystemSettings
                .Where(s => keys.Contains(s.Key))
                .ToListAsync();

            if (!string.IsNullOrEmpty(request.Category))
            {
                existingSettings = existingSettings.Where(s => s.Category == request.Category).ToList();
            }

            var updatedCount = 0;
            var errors = new List<string>();

            foreach (var setting in existingSettings)
            {
                if (request.Settings.TryGetValue(setting.Key, out var newValue))
                {
                    // Validate the value
                    var validationResult = ValidateSettingValue(setting.DataType, newValue);
                    if (!validationResult.Succeeded)
                    {
                        errors.Add($"{setting.Key}: {validationResult.Error}");
                        continue;
                    }

                    setting.Value = newValue;
                    setting.UpdatedAt = DateTime.UtcNow;
                    setting.UpdatedBy = updatedBy;
                    updatedCount++;
                }
            }

            if (errors.Any())
            {
                return ServiceResult<bool>.Failure($"Validation errors: {string.Join(", ", errors)}");
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("Updated {Count} system settings by user {UserId}", updatedCount, updatedBy);

            return ServiceResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating system settings");
            return ServiceResult<bool>.Failure("Failed to update system settings");
        }
    }

    public async Task<ServiceResult<bool>> UpdateSettingAsync(string key, string value, string updatedBy)
    {
        try
        {
            var setting = await _context.SystemSettings
                .FirstOrDefaultAsync(s => s.Key == key);

            if (setting == null)
            {
                return ServiceResult<bool>.Failure($"Setting not found: {key}");
            }

            // Validate the value
            var validationResult = ValidateSettingValue(setting.DataType, value);
            if (!validationResult.Succeeded)
            {
                return ServiceResult<bool>.Failure(validationResult.Error ?? "Validation failed");
            }

            setting.Value = value;
            setting.UpdatedAt = DateTime.UtcNow;
            setting.UpdatedBy = updatedBy;

            await _context.SaveChangesAsync();
            _logger.LogInformation("Updated setting {Key} by user {UserId}", key, updatedBy);

            return ServiceResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating setting {Key}", key);
            return ServiceResult<bool>.Failure($"Failed to update setting: {key}");
        }
    }

    public async Task<ServiceResult<bool>> ResetSettingToDefaultAsync(string key, string updatedBy)
    {
        try
        {
            var setting = await _context.SystemSettings
                .FirstOrDefaultAsync(s => s.Key == key);

            if (setting == null)
            {
                return ServiceResult<bool>.Failure($"Setting not found: {key}");
            }

            if (string.IsNullOrEmpty(setting.DefaultValue))
            {
                return ServiceResult<bool>.Failure($"No default value available for setting: {key}");
            }

            setting.Value = setting.DefaultValue;
            setting.UpdatedAt = DateTime.UtcNow;
            setting.UpdatedBy = updatedBy;

            await _context.SaveChangesAsync();
            _logger.LogInformation("Reset setting {Key} to default by user {UserId}", key, updatedBy);

            return ServiceResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting setting {Key} to default", key);
            return ServiceResult<bool>.Failure($"Failed to reset setting: {key}");
        }
    }

    public async Task<ServiceResult<bool>> ResetCategoryToDefaultAsync(string category, string updatedBy)
    {
        try
        {
            var settings = await _context.SystemSettings
                .Where(s => s.Category == category && !string.IsNullOrEmpty(s.DefaultValue))
                .ToListAsync();

            foreach (var setting in settings)
            {
                setting.Value = setting.DefaultValue!;
                setting.UpdatedAt = DateTime.UtcNow;
                setting.UpdatedBy = updatedBy;
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("Reset {Count} settings in category {Category} to default by user {UserId}", 
                settings.Count, category, updatedBy);

            return ServiceResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting category {Category} to default", category);
            return ServiceResult<bool>.Failure($"Failed to reset category: {category}");
        }
    }

    public async Task<ServiceResult<List<SystemSettingsDto>>> GetRestartRequiredSettingsAsync()
    {
        try
        {
            var settings = await _context.SystemSettings
                .Where(s => s.RequiresRestart)
                .OrderBy(s => s.Category)
                .ThenBy(s => s.Key)
                .ToListAsync();

            var dtos = settings.Select(ConvertToDto).ToList();
            return ServiceResult<List<SystemSettingsDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting restart required settings");
            return ServiceResult<List<SystemSettingsDto>>.Failure("Failed to retrieve restart required settings");
        }
    }

    public ServiceResult<bool> ValidateSettingValue(string dataType, string value)
    {
        try
        {
            switch (dataType.ToUpperInvariant())
            {
                case "STRING":
                    // Strings are always valid
                    return ServiceResult<bool>.Success(true);

                case "NUMBER":
                    if (!decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out _))
                    {
                        return ServiceResult<bool>.Failure("Value must be a valid number");
                    }
                    break;

                case "BOOLEAN":
                    if (!bool.TryParse(value, out _))
                    {
                        return ServiceResult<bool>.Failure("Value must be true or false");
                    }
                    break;

                case "JSON":
                    try
                    {
                        JsonDocument.Parse(value);
                    }
                    catch (JsonException)
                    {
                        return ServiceResult<bool>.Failure("Value must be valid JSON");
                    }
                    break;

                default:
                    return ServiceResult<bool>.Failure($"Unknown data type: {dataType}");
            }

            return ServiceResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating setting value");
            return ServiceResult<bool>.Failure("Validation error occurred");
        }
    }

    public async Task<ServiceResult<List<SystemSettingsDto>>> GetSensitiveSettingsAsync()
    {
        try
        {
            var settings = await _context.SystemSettings
                .Where(s => s.IsSensitive)
                .OrderBy(s => s.Category)
                .ThenBy(s => s.Key)
                .ToListAsync();

            var dtos = settings.Select(s => 
            {
                var dto = ConvertToDto(s);
                dto.Value = "***MASKED***"; // Mask sensitive values
                return dto;
            }).ToList();

            return ServiceResult<List<SystemSettingsDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting sensitive settings");
            return ServiceResult<List<SystemSettingsDto>>.Failure("Failed to retrieve sensitive settings");
        }
    }

    private SystemSettingsDto ConvertToDto(SystemSettings setting)
    {
        return new SystemSettingsDto
        {
            Id = setting.Id,
            Key = setting.Key,
            Value = setting.Value,
            Description = setting.Description,
            Category = setting.Category,
            DataType = setting.DataType,
            IsSensitive = setting.IsSensitive,
            RequiresRestart = setting.RequiresRestart,
            DefaultValue = setting.DefaultValue,
            CreatedAt = setting.CreatedAt,
            UpdatedAt = setting.UpdatedAt,
            UpdatedBy = setting.UpdatedBy
        };
    }

    private T? ConvertValue<T>(string value, string dataType)
    {
        try
        {
            var targetType = typeof(T);
            var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

            switch (dataType.ToUpperInvariant())
            {
                case "BOOLEAN":
                    if (underlyingType == typeof(bool))
                        return (T)(object)bool.Parse(value);
                    break;

                case "NUMBER":
                    if (underlyingType == typeof(int))
                        return (T)(object)int.Parse(value, CultureInfo.InvariantCulture);
                    if (underlyingType == typeof(decimal))
                        return (T)(object)decimal.Parse(value, CultureInfo.InvariantCulture);
                    if (underlyingType == typeof(double))
                        return (T)(object)double.Parse(value, CultureInfo.InvariantCulture);
                    break;

                case "JSON":
                    if (underlyingType != typeof(string))
                        return JsonSerializer.Deserialize<T>(value);
                    break;
            }

            // Default to string conversion
            return (T)Convert.ChangeType(value, underlyingType, CultureInfo.InvariantCulture);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error converting value {Value} to type {Type}", value, typeof(T).Name);
            return default(T);
        }
    }
} 