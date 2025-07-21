using System.ComponentModel.DataAnnotations;

namespace DocFlowHub.Core.Models.Admin;

public class UserActivityReport
{
    public string UserId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    
    // Activity Metrics
    public UserActivityMetrics ActivityMetrics { get; set; } = new();
    
    // Usage Statistics
    public UserUsageStatistics UsageStatistics { get; set; } = new();
    
    // Security Status
    public UserSecurityStatus SecurityStatus { get; set; } = new();
    
    // Recent Activity
    public List<UserActivityEntry> RecentActivities { get; set; } = new();
}

public class UserActivityMetrics
{
    public int DocumentsUploaded { get; set; }
    public int DocumentsDownloaded { get; set; }
    public int DocumentsShared { get; set; }
    public int ProjectsCreated { get; set; }
    public int ProjectsParticipated { get; set; }
    public int TeamsJoined { get; set; }
    public int TeamsManaged { get; set; }
    public int AIOperationsPerformed { get; set; }
    public int LoginCount { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public DateTime? LastActivityAt { get; set; }
    public ActivityLevel ActivityLevel { get; set; }
}

public class UserUsageStatistics
{
    public long StorageUsedBytes { get; set; }
    public long StorageLimitBytes { get; set; }
    public decimal StorageUsagePercentage { get; set; }
    public long BandwidthUsedBytes { get; set; }
    public int AITokensUsed { get; set; }
    public int AITokensLimit { get; set; }
    public decimal AIUsagePercentage { get; set; }
}

public class UserSecurityStatus
{
    public bool IsEmailConfirmed { get; set; }
    public bool IsTwoFactorEnabled { get; set; }
    public bool IsLockedOut { get; set; }
    public DateTime? LockoutEnd { get; set; }
    public int FailedLoginAttempts { get; set; }
    public DateTime? LastPasswordChange { get; set; }
    public bool RequiresPasswordChange { get; set; }
    public List<string> RecentIpAddresses { get; set; } = new();
    public List<string> RecentDevices { get; set; } = new();
    public int SecurityScore { get; set; } // 0-100
    public List<SecurityAlert> SecurityAlerts { get; set; } = new();
}

public class UserActivityEntry
{
    public DateTime Timestamp { get; set; }
    public string ActivityType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? ResourceId { get; set; }
    public string? ResourceName { get; set; }
    public string IpAddress { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
}

public class SecurityAlert
{
    public string AlertType { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string Severity { get; set; } = string.Empty; // Low, Medium, High, Critical
    public bool IsResolved { get; set; }
}

public class UserCommunication
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string AdminId { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public CommunicationType Type { get; set; }
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }
}

public enum CommunicationType
{
    AdminMessage,
    SystemNotification,
    SecurityAlert,
    PasswordReset,
    Welcome,
    AccountUpdate,
    BulkMessage
}

public class UserManagementStatistics
{
    public int TotalUsers { get; set; }
    public int ActiveUsers { get; set; }
    public int InactiveUsers { get; set; }
    public int LockedUsers { get; set; }
    public int UnconfirmedUsers { get; set; }
    public int NewUsersToday { get; set; }
    public int NewUsersThisWeek { get; set; }
    public int NewUsersThisMonth { get; set; }
    public List<RoleDistribution> RoleDistribution { get; set; } = new();
    public List<ActivityLevelDistribution> ActivityDistribution { get; set; } = new();
}

public class RoleDistribution
{
    public string RoleName { get; set; } = string.Empty;
    public int UserCount { get; set; }
    public decimal Percentage { get; set; }
}

public class ActivityLevelDistribution
{
    public ActivityLevel Level { get; set; }
    public int UserCount { get; set; }
    public decimal Percentage { get; set; }
} 