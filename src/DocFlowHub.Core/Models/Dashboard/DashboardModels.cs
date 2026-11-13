namespace DocFlowHub.Core.Models.Dashboard;

/// <summary>Time window for the dashboard's "+N" figure and activity feed.</summary>
public enum DashboardPeriod
{
    Week = 7,
    Month = 30,
    All = 0,
}

/// <summary>A signed-in user's dashboard: only documents they own or that are shared with one of their teams.</summary>
public class UserDashboard
{
    public int Documents { get; set; }
    public int Versions { get; set; }
    public int Teams { get; set; }

    /// <summary>Documents owned by someone else and shared with one of the user's teams.</summary>
    public int SharedWithMe { get; set; }

    /// <summary>Versions (including first uploads) created in the period; null for <see cref="DashboardPeriod.All"/>.</summary>
    public int? VersionsInPeriod { get; set; }

    public List<DashboardDocumentRow> RecentDocuments { get; set; } = new();

    public int DocumentsWithoutSummary { get; set; }
    public List<DashboardDocumentRow> NewestWithoutSummary { get; set; } = new();

    public long StorageBytes { get; set; }
    public List<StorageByType> Storage { get; set; } = new();

    public List<DashboardTeamRow> TeamList { get; set; } = new();
    public List<ActivityItem> Activity { get; set; } = new();
}

/// <summary>System-wide numbers for the admin overview.</summary>
public class AdminOverview
{
    public int Users { get; set; }
    public int Administrators { get; set; }
    public int InactiveUsers { get; set; }
    public int Documents { get; set; }
    public int Versions { get; set; }
    public int Teams { get; set; }

    /// <summary>Null for <see cref="DashboardPeriod.All"/>.</summary>
    public int? NewDocumentsInPeriod { get; set; }
    public int? NewUsersInPeriod { get; set; }

    public List<AdminUserRow> RecentUsers { get; set; } = new();

    public int AiRequestsToday { get; set; }
    public int AiRequestsInPeriod { get; set; }
    public int AiFailedInPeriod { get; set; }
    public long AiTokensInPeriod { get; set; }

    public long StorageBytes { get; set; }
    public List<StorageByType> Storage { get; set; } = new();

    public List<DashboardTeamRow> LargestTeams { get; set; } = new();
    public List<ActivityItem> Activity { get; set; } = new();
}

public class DashboardDocumentRow
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;

    /// <summary>Stored file type, e.g. ".pdf".</summary>
    public string FileType { get; set; } = string.Empty;

    /// <summary>Folder path without the leading slash, else the project name, else null.</summary>
    public string? Location { get; set; }
    public int? Version { get; set; }
    public long FileSize { get; set; }
    public DateTime ModifiedAt { get; set; }
    public string OwnerName { get; set; } = string.Empty;
    public bool HasSummary { get; set; }
}

public class StorageByType
{
    public string FileType { get; set; } = string.Empty;
    public int Count { get; set; }
    public long Bytes { get; set; }
}

public class DashboardTeamRow
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    /// <summary>"owner", "admin" or "member" for the current user; null on the admin overview.</summary>
    public string? Role { get; set; }
    public int MemberCount { get; set; }
}

public class AdminUserRow
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsAdministrator { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
}

public enum ActivityKind
{
    /// <summary>First version of a document.</summary>
    Created,
    /// <summary>A later version of a document.</summary>
    Version,
    AiSummary,
    AiComparison,
    TeamJoined,
    UserRegistered,
}

/// <summary>One line of the activity feed: "{Actor} {Verb} {Subject}{SubjectSuffix}".</summary>
public class ActivityItem
{
    public ActivityKind Kind { get; set; }
    public DateTime At { get; set; }

    /// <summary>Person's display name; null when the actor is the AI.</summary>
    public string? Actor { get; set; }
    public string Verb { get; set; } = string.Empty;
    public string? Subject { get; set; }
    public string? SubjectSuffix { get; set; }
    public int? DocumentId { get; set; }
    public int? TeamId { get; set; }

    /// <summary>First 7 characters of the version's file hash, when there is one.</summary>
    public string? Hash { get; set; }
}
