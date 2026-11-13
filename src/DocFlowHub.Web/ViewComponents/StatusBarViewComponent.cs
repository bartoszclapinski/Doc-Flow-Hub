using System.Reflection;
using System.Security.Claims;
using DocFlowHub.Core.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace DocFlowHub.Web.ViewComponents;

/// <summary>
/// The 24px status bar under every page: storage provider, current path, the signed-in
/// user's AI requests today, the demo flag and the app version.
/// </summary>
public class StatusBarViewComponent : ViewComponent
{
    private static readonly TimeSpan AiUsageCacheTime = TimeSpan.FromSeconds(60);

    private readonly IConfiguration _configuration;
    private readonly IDemoModeService _demoMode;
    private readonly IAIUsageTrackingService _aiUsage;
    private readonly IMemoryCache _cache;

    public StatusBarViewComponent(
        IConfiguration configuration,
        IDemoModeService demoMode,
        IAIUsageTrackingService aiUsage,
        IMemoryCache cache)
    {
        _configuration = configuration;
        _demoMode = demoMode;
        _aiUsage = aiUsage;
        _cache = cache;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var model = new StatusBarModel
        {
            StorageLabel = string.Equals(_configuration["DocumentStorage:Provider"], "Local", StringComparison.OrdinalIgnoreCase)
                ? "Local storage"
                : "Azure Blob",
            Path = ViewData["StatusPath"] as string,
            IsDemo = _demoMode.IsEnabled,
            Version = AppVersion.Value,
        };

        var userId = UserClaimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId != null)
        {
            (model.AiRequestsToday, model.AiDailyLimit) = await GetAiUsageAsync(userId);
        }

        return View(model);
    }

    private async Task<(int? used, int? limit)> GetAiUsageAsync(string userId)
    {
        var key = $"dfh-statusbar-ai:{userId}";
        if (_cache.TryGetValue(key, out (int? used, int? limit) cached))
        {
            return cached;
        }

        (int? used, int? limit) usage = (null, null);
        try
        {
            var result = await _aiUsage.GetUserRateLimitsAsync(userId);
            if (result.Succeeded && result.Data != null)
            {
                usage = (result.Data.CurrentDailyOperations, result.Data.DailyOperationLimit);
            }
        }
        catch (Exception)
        {
            // The error page renders the status bar too; a database failure must not break it.
        }

        _cache.Set(key, usage, AiUsageCacheTime);
        return usage;
    }

    /// <summary>"v1.0.0" plus the short commit hash when the build stamped one.</summary>
    private static readonly Lazy<string> AppVersion = new(() =>
    {
        var informational = typeof(StatusBarViewComponent).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "1.0.0";
        var parts = informational.Split('+', 2);
        var version = $"v{parts[0]}";
        return parts.Length == 2 && parts[1].Length >= 7 ? $"{version} · {parts[1][..7]}" : version;
    });
}

public class StatusBarModel
{
    public string StorageLabel { get; set; } = string.Empty;
    public string? Path { get; set; }
    public int? AiRequestsToday { get; set; }
    public int? AiDailyLimit { get; set; }
    public bool IsDemo { get; set; }
    public string Version { get; set; } = string.Empty;
}
