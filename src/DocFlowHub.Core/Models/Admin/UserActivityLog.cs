using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DocFlowHub.Core.Identity;

namespace DocFlowHub.Core.Models.Admin;

[Table("UserActivityLogs")]
public class UserActivityLog
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    [StringLength(450)]
    public string UserId { get; set; } = string.Empty;
    
    [Required]
    [StringLength(100)]
    public string ActivityType { get; set; } = string.Empty;
    
    [Required]
    [StringLength(500)]
    public string Description { get; set; } = string.Empty;
    
    [StringLength(450)]
    public string? ResourceId { get; set; }
    
    [StringLength(200)]
    public string? ResourceName { get; set; }
    
    [StringLength(45)]
    public string IpAddress { get; set; } = string.Empty;
    
    [StringLength(500)]
    public string UserAgent { get; set; } = string.Empty;
    
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    [Column(TypeName = "nvarchar(max)")]
    public string? AdditionalData { get; set; } // JSON for extra context
}

[Table("UserSecurityEvents")]
public class UserSecurityEvent
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    [StringLength(450)]
    public string UserId { get; set; } = string.Empty;
    
    [Required]
    [StringLength(100)]
    public string EventType { get; set; } = string.Empty;
    
    [Required]
    [StringLength(500)]
    public string Description { get; set; } = string.Empty;
    
    [Required]
    [StringLength(20)]
    public string Severity { get; set; } = string.Empty; // Low, Medium, High, Critical
    
    [StringLength(45)]
    public string IpAddress { get; set; } = string.Empty;
    
    [StringLength(500)]
    public string UserAgent { get; set; } = string.Empty;
    
    public bool IsResolved { get; set; } = false;
    
    public DateTime? ResolvedAt { get; set; }
    
    [StringLength(450)]
    public string? ResolvedBy { get; set; }
    
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    [Column(TypeName = "nvarchar(max)")]
    public string? EventData { get; set; } // JSON for event-specific data
}

[Table("UserCommunications")]
public class UserCommunicationEntity
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    [StringLength(450)]
    public string UserId { get; set; } = string.Empty;
    
    [StringLength(450)]
    public string? AdminId { get; set; }
    
    [Required]
    [StringLength(200)]
    public string Subject { get; set; } = string.Empty;
    
    [Required]
    [Column(TypeName = "nvarchar(max)")]
    public string Message { get; set; } = string.Empty;
    
    [Required]
    [StringLength(50)]
    public string Type { get; set; } = string.Empty; // Maps to CommunicationType enum
    
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
    
    public bool IsRead { get; set; } = false;
    
    public DateTime? ReadAt { get; set; }
    
    [StringLength(45)]
    public string? SenderIpAddress { get; set; }
    
    // Navigation properties
    [ForeignKey("UserId")]
    public virtual ApplicationUser? User { get; set; }
    
    [ForeignKey("AdminId")]
    public virtual ApplicationUser? Admin { get; set; }
}

[Table("UserLoginAttempts")]
public class UserLoginAttempt
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    [StringLength(450)]
    public string UserId { get; set; } = string.Empty;
    
    [StringLength(100)]
    public string? Email { get; set; }
    
    [Required]
    [StringLength(45)]
    public string IpAddress { get; set; } = string.Empty;
    
    [StringLength(500)]
    public string UserAgent { get; set; } = string.Empty;
    
    public bool IsSuccessful { get; set; }
    
    [StringLength(200)]
    public string? FailureReason { get; set; }
    
    public DateTime AttemptedAt { get; set; } = DateTime.UtcNow;
    
    [StringLength(100)]
    public string? Country { get; set; }
    
    [StringLength(100)]
    public string? City { get; set; }
}

[Table("UserDevices")]
public class UserDevice
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    [StringLength(450)]
    public string UserId { get; set; } = string.Empty;
    
    [Required]
    [StringLength(500)]
    public string DeviceFingerprint { get; set; } = string.Empty;
    
    [StringLength(100)]
    public string? DeviceType { get; set; } // Desktop, Mobile, Tablet
    
    [StringLength(100)]
    public string? OperatingSystem { get; set; }
    
    [StringLength(100)]
    public string? Browser { get; set; }
    
    [StringLength(45)]
    public string? LastIpAddress { get; set; }
    
    public DateTime FirstSeen { get; set; } = DateTime.UtcNow;
    
    public DateTime LastSeen { get; set; } = DateTime.UtcNow;
    
    public bool IsTrusted { get; set; } = false;
    
    [StringLength(200)]
    public string? DeviceName { get; set; }
    
    // Navigation property
    [ForeignKey("UserId")]
    public virtual ApplicationUser? User { get; set; }
} 