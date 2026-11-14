namespace DocFlowHub.Infrastructure.Services.Common;

/// <summary>Small formatting rules shared by the read-only screen services.</summary>
internal static class DisplayFormat
{
    /// <summary>"First Last", or the e-mail address when the user has no name.</summary>
    public static string PersonName(string? first, string? last, string? email)
    {
        var name = $"{first} {last}".Trim();
        return name.Length > 0 ? name : email ?? "Unknown";
    }

    /// <summary>First 7 hex characters of the stored MD5 hex string, like a short commit id.</summary>
    public static string? ShortHash(string? hash) =>
        hash is { Length: >= 7 } && hash[..7].All(Uri.IsHexDigit) ? hash[..7].ToLowerInvariant() : null;

    /// <summary>"Project / folder / subfolder" from a project name and a folder path ("/folder/subfolder").</summary>
    public static string? Location(string? projectName, string? folderPath)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(projectName)) parts.Add(projectName.Trim());
        if (!string.IsNullOrWhiteSpace(folderPath))
        {
            parts.AddRange(folderPath.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        }
        return parts.Count == 0 ? null : string.Join(" / ", parts);
    }
}
