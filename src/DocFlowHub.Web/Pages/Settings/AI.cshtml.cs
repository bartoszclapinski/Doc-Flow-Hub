using System.ComponentModel.DataAnnotations;
using DocFlowHub.Core.Models.Admin.Browser;
using DocFlowHub.Core.Models.AI;
using DocFlowHub.Core.Services.Interfaces;
using DocFlowHub.Web.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DocFlowHub.Web.Pages.Settings;

/// <summary>
/// Settings · AI (design 3f, decision S14-10). Every stored preference stays editable, but only the
/// preferred model and the summary / comparison switches change what the app does today; the others
/// carry a "Saved, not applied yet" note. Usage: this month and the last 14 days from the AI log.
/// </summary>
[Authorize]
public class AIModel : PageModel
{
    private readonly IAISettingsService _settings;
    private readonly IAIUsageTrackingService _usage;
    private readonly IAdminBrowserService _browser;

    public AIModel(IAISettingsService settings, IAIUsageTrackingService usage, IAdminBrowserService browser)
    {
        _settings = settings;
        _usage = usage;
        _browser = browser;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public UserAiUsage Usage { get; private set; } = new();
    public int DailyLimit { get; private set; }
    public string? ErrorMessage { get; private set; }

    public class InputModel
    {
        public Core.Models.AI.AIModel PreferredModel { get; set; } = AIModelHelper.GetDefaultModel();

        [StringLength(500, ErrorMessage = "The key can have at most 500 characters")]
        public string? CustomApiKey { get; set; }

        public bool UseCustomApiKey { get; set; }
        public bool SummarizationEnabled { get; set; } = true;
        public bool VersionComparisonEnabled { get; set; } = true;
        public bool AutoSummarizeOnUpload { get; set; } = true;

        [Range(0.1, 1.0, ErrorMessage = "Between 0.1 and 1.0")]
        public double QualityPreference { get; set; } = 0.7;

        [Range(100, 10000, ErrorMessage = "Between 100 and 10,000")]
        public int MaxTokensPerOperation { get; set; } = 2000;

        [Range(0.1, 1.0, ErrorMessage = "Between 0.1 and 1.0")]
        public double ComparisonSensitivity { get; set; } = 0.5;

        public bool EnableCaching { get; set; } = true;

        [Range(1, 48, ErrorMessage = "Between 1 and 48 hours")]
        public int CacheDurationHours { get; set; } = 24;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var userId = User.GetUserId()!;
        var settings = await _settings.GetUserSettingsAsync(userId);
        if (settings.Succeeded && settings.Data != null)
        {
            var s = settings.Data;
            Input = new InputModel
            {
                PreferredModel = s.PreferredModel,
                CustomApiKey = s.CustomApiKey,
                UseCustomApiKey = s.UseCustomApiKey,
                SummarizationEnabled = s.SummarizationEnabled,
                VersionComparisonEnabled = s.VersionComparisonEnabled,
                AutoSummarizeOnUpload = s.AutoSummarizeOnUpload,
                QualityPreference = s.QualityPreference,
                MaxTokensPerOperation = s.MaxTokensPerOperation,
                ComparisonSensitivity = s.ComparisonSensitivity,
                EnableCaching = s.EnableCaching,
                CacheDurationHours = s.CacheDurationHours,
            };
        }
        else
        {
            ErrorMessage = settings.Error ?? "Your AI settings could not be loaded.";
        }
        await LoadUsageAsync(userId);
        return Page();
    }

    public async Task<IActionResult> OnPostSaveSettingsAsync()
    {
        var userId = User.GetUserId()!;
        if (!ModelState.IsValid)
        {
            await LoadUsageAsync(userId);
            return Page();
        }

        var result = await _settings.UpdateUserSettingsAsync(userId, new AISettings
        {
            UserId = userId,
            PreferredModel = Input.PreferredModel,
            CustomApiKey = string.IsNullOrWhiteSpace(Input.CustomApiKey) ? null : Input.CustomApiKey.Trim(),
            UseCustomApiKey = Input.UseCustomApiKey,
            SummarizationEnabled = Input.SummarizationEnabled,
            VersionComparisonEnabled = Input.VersionComparisonEnabled,
            AutoSummarizeOnUpload = Input.AutoSummarizeOnUpload,
            QualityPreference = Input.QualityPreference,
            MaxTokensPerOperation = Input.MaxTokensPerOperation,
            ComparisonSensitivity = Input.ComparisonSensitivity,
            EnableCaching = Input.EnableCaching,
            CacheDurationHours = Input.CacheDurationHours,
            IsActive = true,
            UpdatedAt = DateTime.UtcNow,
        });
        if (result.Succeeded) TempData["ToastSuccess"] = "AI settings saved";
        else TempData["ToastError"] = result.Error ?? "The settings could not be saved.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostResetToDefaultsAsync()
    {
        var result = await _settings.ResetToDefaultsAsync(User.GetUserId()!);
        if (result.Succeeded) TempData["ToastSuccess"] = "AI settings reset to the defaults";
        else TempData["ToastError"] = result.Error ?? "The settings could not be reset.";
        return RedirectToPage();
    }

    private async Task LoadUsageAsync(string userId)
    {
        var usage = await _browser.GetUserAiUsageAsync(userId);
        if (usage.Succeeded && usage.Data != null) Usage = usage.Data;
        var limits = await _usage.GetUserRateLimitsAsync(userId);
        DailyLimit = limits.Succeeded && limits.Data != null ? limits.Data.DailyOperationLimit : 0;
    }
}
