namespace DocFlowHub.Core.Models.Analytics;

/// <summary>
/// Comprehensive system analytics and statistics
/// </summary>
public class SystemAnalytics
{
    // User Statistics
    public int TotalUsers { get; set; }
    public int ActiveUsers { get; set; } // Users active in last 30 days
    public int NewUsersThisMonth { get; set; }
    public int AdminUsers { get; set; }

    // Document Statistics
    public int TotalDocuments { get; set; }
    public int DocumentsUploadedThisMonth { get; set; }
    public long TotalStorageUsedBytes { get; set; }
    public double AverageDocumentSizeMB { get; set; }
    public int DocumentsWithSummaries { get; set; }

    // Project and Organization Statistics
    public int TotalProjects { get; set; }
    public int ActiveProjects { get; set; } // Projects with activity in last 30 days
    public int TotalFolders { get; set; }
    public int ArchivedProjects { get; set; }
    public int ArchivedFolders { get; set; }

    // Team Statistics
    public int TotalTeams { get; set; }
    public int ActiveTeams { get; set; } // Teams with activity in last 30 days
    public double AverageTeamSize { get; set; }
    public int TotalTeamMembers { get; set; }

    // AI Usage Statistics
    public int AIOperationsThisMonth { get; set; }
    public decimal AIUsageCostThisMonth { get; set; }
    public int DocumentSummariesGenerated { get; set; }
    public int VersionComparisonsPerformed { get; set; }

    // System Performance
    public double AverageResponseTimeMs { get; set; }
    public double SystemUptimePercentage { get; set; }
    public int ErrorCount { get; set; }
    public double ErrorRate { get; set; } // Percentage

    // Growth Metrics
    public double UserGrowthRate { get; set; } // Percentage month-over-month
    public double DocumentGrowthRate { get; set; } // Percentage month-over-month
    public double StorageGrowthRate { get; set; } // Percentage month-over-month

    // Activity Metrics
    public int DailyActiveUsers { get; set; }
    public int WeeklyActiveUsers { get; set; }
    public int MonthlyActiveUsers { get; set; }

    // Top Categories
    public List<CategoryUsage> TopDocumentCategories { get; set; } = new();
    public List<UserActivity> MostActiveUsers { get; set; } = new();
    public List<ProjectActivity> MostActiveProjects { get; set; } = new();

    // Calculated Properties
    public string TotalStorageFormatted => FormatBytes(TotalStorageUsedBytes);
    public string AverageDocumentSizeFormatted => $"{AverageDocumentSizeMB:F2} MB";
    public string AIUsageCostFormatted => $"${AIUsageCostThisMonth:F2}";
    public string SystemUptimeFormatted => $"{SystemUptimePercentage:F2}%";
    public string ErrorRateFormatted => $"{ErrorRate:F2}%";

    private static string FormatBytes(long bytes)
    {
        const int scale = 1024;
        string[] orders = { "GB", "MB", "KB", "Bytes" };
        long max = (long)Math.Pow(scale, orders.Length - 1);

        foreach (string order in orders)
        {
            if (bytes > max)
                return $"{decimal.Divide(bytes, max):##.##} {order}";
            
            max /= scale;
        }
        return "0 Bytes";
    }
}

/// <summary>
/// Document category usage statistics
/// </summary>
public class CategoryUsage
{
    public string CategoryName { get; set; } = string.Empty;
    public int DocumentCount { get; set; }
    public double Percentage { get; set; }
}

/// <summary>
/// User activity statistics
/// </summary>
public class UserActivity
{
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int DocumentsUploaded { get; set; }
    public int ProjectsCreated { get; set; }
    public int TeamsJoined { get; set; }
    public DateTime LastActivity { get; set; }
    public int ActivityScore { get; set; }
}

/// <summary>
/// Project activity statistics
/// </summary>
public class ProjectActivity
{
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public string OwnerName { get; set; } = string.Empty;
    public int DocumentCount { get; set; }
    public int FolderCount { get; set; }
    public int TeamMemberCount { get; set; }
    public DateTime LastActivity { get; set; }
    public int ActivityScore { get; set; }
} 