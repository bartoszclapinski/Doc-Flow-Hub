using System.ComponentModel.DataAnnotations;

namespace DocFlowHub.Core.Models.Admin.Dto;

/// <summary>
/// DTO for system settings data transfer
/// </summary>
public class SystemSettingsDto
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Key { get; set; } = string.Empty;

    [Required]
    public string Value { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    [Required]
    [StringLength(50)]
    public string Category { get; set; } = string.Empty;

    [Required]
    [StringLength(20)]
    public string DataType { get; set; } = "String";

    public bool IsSensitive { get; set; } = false;
    public bool RequiresRestart { get; set; } = false;
    public string? DefaultValue { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}

/// <summary>
/// Request model for updating system settings
/// </summary>
public class UpdateSystemSettingsRequest
{
    /// <summary>
    /// Dictionary of setting key-value pairs to update
    /// </summary>
    [Required]
    public Dictionary<string, string> Settings { get; set; } = new();

    /// <summary>
    /// Category to update (optional - updates all if not specified)
    /// </summary>
    public string? Category { get; set; }
}

/// <summary>
/// Grouped settings by category for admin UI
/// </summary>
public class SystemSettingsGroupDto
{
    public string Category { get; set; } = string.Empty;
    public List<SystemSettingsDto> Settings { get; set; } = new();
} 