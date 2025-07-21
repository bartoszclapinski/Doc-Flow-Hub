using System.ComponentModel.DataAnnotations;

namespace DocFlowHub.Core.Models.Admin;

public class UserManagementFilter
{
    public string? SearchTerm { get; set; }
    public string? Role { get; set; }
    public UserStatus? Status { get; set; }
    public DateTime? RegisteredAfter { get; set; }
    public DateTime? RegisteredBefore { get; set; }
    public DateTime? LastLoginAfter { get; set; }
    public DateTime? LastLoginBefore { get; set; }
    public ActivityLevel? ActivityLevel { get; set; }
    public string? SortBy { get; set; } = "Name";
    public bool SortDescending { get; set; } = false;
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public enum UserStatus
{
    All,
    Active,
    Inactive,
    Locked,
    EmailNotConfirmed
}

public enum ActivityLevel
{
    All,
    VeryActive,    // > 50 documents or frequent logins
    Active,        // 10-50 documents or regular logins
    Moderate,      // 1-10 documents or occasional logins
    LowActivity,   // < 1 document or rare logins
    Inactive       // No recent activity
}

public class BulkUserOperation
{
    [Required]
    public List<string> UserIds { get; set; } = new();
    
    [Required]
    public BulkOperationType Operation { get; set; }
    
    public string? NewRole { get; set; }
    public string? Message { get; set; }
    public bool SendNotification { get; set; } = true;
}

public enum BulkOperationType
{
    Activate,
    Deactivate,
    Delete,
    AssignRole,
    RemoveRole,
    SendPasswordReset,
    SendMessage,
    LockAccount,
    UnlockAccount
}

public class BulkOperationResult
{
    public int TotalUsers { get; set; }
    public int SuccessfulOperations { get; set; }
    public int FailedOperations { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> SuccessfulUserIds { get; set; } = new();
    public List<string> FailedUserIds { get; set; } = new();
    public DateTime CompletedAt { get; set; } = DateTime.UtcNow;
} 