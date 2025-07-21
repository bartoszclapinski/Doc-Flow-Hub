using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DocFlowHub.Core.Models.Admin;

/// <summary>
/// Entity for storing global system settings and configuration
/// </summary>
[Table("SystemSettings")]
public class SystemSettings
{
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Unique setting key identifier
    /// </summary>
    [Required]
    [StringLength(100)]
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Setting value as JSON string for flexibility
    /// </summary>
    [Required]
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable description of the setting
    /// </summary>
    [StringLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// Setting category for organization (AI, Security, Performance, etc.)
    /// </summary>
    [Required]
    [StringLength(50)]
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Data type for proper parsing (String, Number, Boolean, JSON)
    /// </summary>
    [Required]
    [StringLength(20)]
    public string DataType { get; set; } = "String";

    /// <summary>
    /// Whether this setting is sensitive (API keys, passwords)
    /// </summary>
    public bool IsSensitive { get; set; } = false;

    /// <summary>
    /// Whether this setting requires application restart to take effect
    /// </summary>
    public bool RequiresRestart { get; set; } = false;

    /// <summary>
    /// Default value for the setting
    /// </summary>
    public string? DefaultValue { get; set; }

    /// <summary>
    /// When this setting was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When this setting was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// User ID who last updated this setting
    /// </summary>
    [StringLength(450)]
    public string? UpdatedBy { get; set; }
} 