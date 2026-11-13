using DocFlowHub.Core.Models.Dashboard;

namespace DocFlowHub.Web.Ui;

/// <summary>Query-string values and labels for the dashboard period dropdown ("Last 7 days").</summary>
public static class DashboardPeriods
{
    public static readonly DashboardPeriod[] All = { DashboardPeriod.Week, DashboardPeriod.Month, DashboardPeriod.All };

    /// <summary>"7d" (default), "30d" or "all".</summary>
    public static DashboardPeriod Parse(string? value) => value?.Trim().ToLowerInvariant() switch
    {
        "30d" => DashboardPeriod.Month,
        "all" => DashboardPeriod.All,
        _ => DashboardPeriod.Week,
    };

    public static string Query(DashboardPeriod period) => period switch
    {
        DashboardPeriod.Month => "30d",
        DashboardPeriod.All => "all",
        _ => "7d",
    };

    public static string Label(DashboardPeriod period) => period switch
    {
        DashboardPeriod.Month => "Last 30 days",
        DashboardPeriod.All => "All time",
        _ => "Last 7 days",
    };

    /// <summary>Suffix for the "+N" stat: "+7 this week".</summary>
    public static string DeltaLabel(DashboardPeriod period) => period == DashboardPeriod.Month ? "in 30 days" : "this week";
}

/// <summary>Model of the storage-by-file-type panel (Dashboard, admin overview).</summary>
public record StoragePanel(IReadOnlyList<StorageByType> Types, long TotalBytes)
{
    private const int NamedTypes = 4;

    /// <summary>The largest four types by size, then everything else as "Other".</summary>
    public IReadOnlyList<(string Label, string Kind, long Bytes, int Count)> Segments()
    {
        var segments = Types.Take(NamedTypes)
            .Select(t =>
            {
                var (label, kind) = Dfh.FileType(t.FileType);
                return (label, kind, t.Bytes, t.Count);
            })
            .ToList();
        var rest = Types.Skip(NamedTypes).ToList();
        if (rest.Count > 0)
        {
            segments.Add(("Other", "other", rest.Sum(t => t.Bytes), rest.Sum(t => t.Count)));
        }
        return segments;
    }
}

/// <summary>Model of the activity feed partial. Admins may not have access to every document or team, so links are optional.</summary>
public record ActivityFeed(IReadOnlyList<ActivityItem> Items, bool Links);
