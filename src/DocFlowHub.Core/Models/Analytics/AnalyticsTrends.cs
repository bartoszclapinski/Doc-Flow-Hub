namespace DocFlowHub.Core.Models.Analytics;

/// <summary>
/// Time-series analytics for trending data
/// </summary>
public class AnalyticsTrends
{
    public List<DailyMetrics> DailyMetrics { get; set; } = new();
    public List<WeeklyMetrics> WeeklyMetrics { get; set; } = new();
    public List<MonthlyMetrics> MonthlyMetrics { get; set; } = new();
    
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int TotalDays => (int)(EndDate - StartDate).TotalDays;
}

/// <summary>
/// Daily metrics for trend analysis
/// </summary>
public class DailyMetrics
{
    public DateTime Date { get; set; }
    public int NewUsers { get; set; }
    public int ActiveUsers { get; set; }
    public int DocumentsUploaded { get; set; }
    public int ProjectsCreated { get; set; }
    public int FoldersCreated { get; set; }
    public int TeamsCreated { get; set; }
    public int AIOperations { get; set; }
    public decimal AIUsageCost { get; set; }
    public long StorageUsedBytes { get; set; }
    public int Logins { get; set; }
    public int ErrorCount { get; set; }
    
    // Calculated properties
    public string DateFormatted => Date.ToString("MMM dd");
    public string StorageFormatted => FormatBytes(StorageUsedBytes);
    public string AIUsageCostFormatted => $"${AIUsageCost:F2}";
    
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
/// Weekly metrics for trend analysis
/// </summary>
public class WeeklyMetrics
{
    public DateTime WeekStart { get; set; }
    public DateTime WeekEnd { get; set; }
    public int WeekNumber { get; set; }
    public int NewUsers { get; set; }
    public int ActiveUsers { get; set; }
    public int DocumentsUploaded { get; set; }
    public int ProjectsCreated { get; set; }
    public int AIOperations { get; set; }
    public decimal AIUsageCost { get; set; }
    public long StorageUsedBytes { get; set; }
    
    // Calculated properties
    public string WeekLabel => $"Week {WeekNumber}";
    public string DateRange => $"{WeekStart:MMM dd} - {WeekEnd:MMM dd}";
    public string StorageFormatted => FormatBytes(StorageUsedBytes);
    public string AIUsageCostFormatted => $"${AIUsageCost:F2}";
    
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
/// Monthly metrics for trend analysis
/// </summary>
public class MonthlyMetrics
{
    public DateTime Month { get; set; }
    public int NewUsers { get; set; }
    public int ActiveUsers { get; set; }
    public int DocumentsUploaded { get; set; }
    public int ProjectsCreated { get; set; }
    public int AIOperations { get; set; }
    public decimal AIUsageCost { get; set; }
    public long StorageUsedBytes { get; set; }
    
    // Growth rates (compared to previous month)
    public double UserGrowthRate { get; set; }
    public double DocumentGrowthRate { get; set; }
    public double StorageGrowthRate { get; set; }
    public double AIUsageGrowthRate { get; set; }
    
    // Calculated properties
    public string MonthLabel => Month.ToString("MMM yyyy");
    public string StorageFormatted => FormatBytes(StorageUsedBytes);
    public string AIUsageCostFormatted => $"${AIUsageCost:F2}";
    public string UserGrowthFormatted => $"{UserGrowthRate:F1}%";
    public string DocumentGrowthFormatted => $"{DocumentGrowthRate:F1}%";
    public string StorageGrowthFormatted => $"{StorageGrowthRate:F1}%";
    
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
/// Performance analytics for system monitoring
/// </summary>
public class PerformanceAnalytics
{
    public double AverageResponseTime { get; set; } // in milliseconds
    public double PeakResponseTime { get; set; } // in milliseconds
    public double MinResponseTime { get; set; } // in milliseconds
    public int TotalRequests { get; set; }
    public int SuccessfulRequests { get; set; }
    public int FailedRequests { get; set; }
    public double SuccessRate { get; set; } // percentage
    public double ErrorRate { get; set; } // percentage
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    
    // Top errors
    public List<ErrorFrequency> TopErrors { get; set; } = new();
    
    // Response time distribution
    public List<ResponseTimeRange> ResponseTimeDistribution { get; set; } = new();
    
    // Calculated properties
    public string AverageResponseTimeFormatted => $"{AverageResponseTime:F2} ms";
    public string SuccessRateFormatted => $"{SuccessRate:F2}%";
    public string ErrorRateFormatted => $"{ErrorRate:F2}%";
    public TimeSpan Duration => EndTime - StartTime;
}

/// <summary>
/// Error frequency tracking
/// </summary>
public class ErrorFrequency
{
    public string ErrorType { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public int Count { get; set; }
    public double Percentage { get; set; }
    public DateTime LastOccurrence { get; set; }
}

/// <summary>
/// Response time distribution ranges
/// </summary>
public class ResponseTimeRange
{
    public string Range { get; set; } = string.Empty; // e.g., "0-100ms"
    public int Count { get; set; }
    public double Percentage { get; set; }
} 