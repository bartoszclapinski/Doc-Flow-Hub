using System.Globalization;
using System.Reflection;

namespace DocFlowHub.Web.Ui;

/// <summary>Formatting helpers shared by the redesigned (IDE-style) pages.</summary>
public static class Dfh
{
    private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

    /// <summary>Hues the design uses for project/team colour squares and file types.</summary>
    private static readonly int[] TeamHues = { 150, 250, 25, 80, 330, 200, 295 };

    /// <summary>"v1.0.0", plus the short commit hash when the build stamped one.</summary>
    public static string AppVersion => AppVersionValue.Value;

    private static readonly Lazy<string> AppVersionValue = new(() =>
    {
        var informational = typeof(Dfh).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "1.0.0";
        var parts = informational.Split('+', 2);
        var version = $"v{parts[0]}";
        return parts.Length == 2 && parts[1].Length >= 7 ? $"{version} · {parts[1][..7]}" : version;
    });

    /// <summary>Label and colour modifier for a stored file type such as ".pdf" → ("PDF", "pdf").</summary>
    public static (string Label, string Kind) FileType(string? fileType)
    {
        var ext = (fileType ?? string.Empty).Trim().TrimStart('.').ToLowerInvariant();
        var kind = ext switch
        {
            "pdf" => "pdf",
            "md" or "markdown" => "md",
            "doc" or "docx" or "odt" or "rtf" => "docx",
            "xls" or "xlsx" or "csv" or "ods" => "xlsx",
            _ => "txt",
        };
        var label = ext.Length == 0 ? "FILE" : ext.Length > 4 ? ext[..4].ToUpperInvariant() : ext.ToUpperInvariant();
        return (label, kind);
    }

    /// <summary>"6 KB", "2.4 MB", "1.47 GB".</summary>
    public static string FileSize(long bytes)
    {
        const double kb = 1024, mb = kb * 1024, gb = mb * 1024;
        return bytes switch
        {
            < 1024 => $"{bytes} B",
            < 1024 * 1024 => $"{Math.Max(1, Math.Round(bytes / kb)).ToString("0", Invariant)} KB",
            < 1024L * 1024 * 1024 => $"{(bytes / mb).ToString("0.#", Invariant)} MB",
            _ => $"{(bytes / gb).ToString("0.##", Invariant)} GB",
        };
    }

    /// <summary>"just now", "5m ago", "2h ago", "yesterday", "3d ago", then "Mar 4" / "Mar 4, 2025". Stored times are UTC.</summary>
    public static string Relative(DateTime utc)
    {
        var now = DateTime.UtcNow;
        var age = now - utc;
        if (age < TimeSpan.FromMinutes(1)) return "just now";
        if (age < TimeSpan.FromHours(1)) return $"{(int)age.TotalMinutes}m ago";
        if (age < TimeSpan.FromHours(24)) return $"{(int)age.TotalHours}h ago";
        if (age < TimeSpan.FromDays(2)) return "yesterday";
        if (age < TimeSpan.FromDays(7)) return $"{(int)age.TotalDays}d ago";
        return utc.Year == now.Year ? utc.ToString("MMM d", Invariant) : utc.ToString("MMM d, yyyy", Invariant);
    }

    /// <summary>Full timestamp for a title tooltip, e.g. "2026-11-13 14:05 UTC".</summary>
    public static string Timestamp(DateTime utc) => utc.ToString("yyyy-MM-dd HH:mm 'UTC'", Invariant);

    /// <summary>"Anna Kowalska" → "AK"; "anna@x.test" → "A".</summary>
    public static string Initials(string? name)
    {
        var parts = (name ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return parts.Length switch
        {
            0 => "?",
            1 => parts[0][..1].ToUpperInvariant(),
            _ => (parts[0][..1] + parts[^1][..1]).ToUpperInvariant(),
        };
    }

    /// <summary>Teams have no stored colour: derive a stable one from the id (decision 14).</summary>
    public static string TeamColor(int teamId) =>
        $"oklch(0.74 0.13 {TeamHues[Math.Abs(teamId) % TeamHues.Length]})";

    /// <summary>"12.5%" of a part in a whole.</summary>
    public static string Percent(long part, long whole) =>
        whole <= 0 ? "0%" : (part * 100.0 / whole).ToString("0.#", Invariant) + "%";

    /// <summary>"1 team" / "3 teams" (English plural with an "s").</summary>
    public static string Noun(long count, string singular) => count == 1 ? singular : singular + "s";

    /// <summary>"1,234" with a thin grouping that reads well in mono.</summary>
    public static string Number(long value) => value.ToString("#,0", Invariant);
}
