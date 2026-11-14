namespace DocFlowHub.Core.Models.Admin.Browser;

/// <summary>Counts in the admin navigation (design 3e).</summary>
public class AdminNavStats
{
    public int Users { get; set; }
    public int Roles { get; set; }
    public int ActiveIn30Days { get; set; }

    /// <summary>Users who can't sign in (lockout). The admin "deactivate" and "lock" actions both do this.</summary>
    public int Locked { get; set; }
    public int SignupsIn7Days { get; set; }
}

/// <summary>One row of the admin users table (design 3e).</summary>
public class AdminUserRow
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new();
    public bool IsActive { get; set; }
    public bool IsLocked { get; set; }
    public bool EmailConfirmed { get; set; }
    public List<string> Teams { get; set; } = new();
    public long StorageBytes { get; set; }
    public int DocumentCount { get; set; }

    /// <summary>AI requests in the last 30 days.</summary>
    public int AiRequests { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public DateTime CreatedAt { get; set; }

    public bool IsAdministrator => Roles.Contains("Administrator");

    /// <summary>
    /// "locked" (can't sign in), "unconfirmed" or "active". Deactivating and locking a user are the same
    /// lockout in UserManagementService; the IsActive flag is not set or checked anywhere.
    /// </summary>
    public string Status => IsLocked ? "locked" : !EmailConfirmed ? "unconfirmed" : "active";
}

/// <summary>The admin user page: the row plus what the admins did about this user.</summary>
public class AdminUserDetails : AdminUserRow
{
    public int VersionCount { get; set; }
    public int SummaryCount { get; set; }
    public List<AdminActionRow> RecentActions { get; set; } = new();
}

/// <summary>An action recorded in UserActivityLogs (by an admin or the user).</summary>
public class AdminActionRow
{
    public DateTime At { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ActorName { get; set; } = string.Empty;
}

/// <summary>The Analytics screen (design 8c) for the last <see cref="Days"/> days.</summary>
public class AdminAnalytics
{
    public int Days { get; set; }
    public int ActiveUsers { get; set; }

    /// <summary>Versions uploaded in the period (first uploads included).</summary>
    public int Uploads { get; set; }
    public long StoredBytes { get; set; }
    public int AiRequests { get; set; }
    public decimal AiCost { get; set; }

    /// <summary>One entry per day, oldest first, days without uploads included.</summary>
    public List<DailyCount> UploadsPerDay { get; set; } = new();

    public List<AiOperationUsage> AiByOperation { get; set; } = new();

    /// <summary>The model used most in the period, if any.</summary>
    public string? TopModel { get; set; }
    public double CacheHitRate { get; set; }
    public double FailureRate { get; set; }
    public double? P95LatencySeconds { get; set; }

    public List<StorageUser> TopStorage { get; set; } = new();
    public List<AdminActionRow> AdminActions { get; set; } = new();
}

public class DailyCount
{
    public DateTime Day { get; set; }
    public int Count { get; set; }
}

public class AiOperationUsage
{
    public string Operation { get; set; } = string.Empty;
    public int Requests { get; set; }
    public decimal Cost { get; set; }

    /// <summary>Share of all requests in the period, 0–1.</summary>
    public double Share { get; set; }
}

public class StorageUser
{
    public string UserId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public long Bytes { get; set; }
    public int Documents { get; set; }
}

/// <summary>One user's AI usage today (User limits page).</summary>
public class AiUsageToday
{
    public string UserId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int Requests { get; set; }
    public int RequestsThisHour { get; set; }
    public decimal Cost { get; set; }
}

/// <summary>The current user's AI usage for Settings · AI (design 3f).</summary>
public class UserAiUsage
{
    public int RequestsThisMonth { get; set; }
    public int RequestsToday { get; set; }

    /// <summary>Last 14 days, oldest first.</summary>
    public List<DailyCount> Last14Days { get; set; } = new();
}
