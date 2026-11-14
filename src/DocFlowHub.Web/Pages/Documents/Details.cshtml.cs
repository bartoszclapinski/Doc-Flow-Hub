using DocFlowHub.Core.Models.AI;
using DocFlowHub.Core.Models.Documents.Browser;
using DocFlowHub.Core.Models.Teams;
using DocFlowHub.Core.Models.Teams.Dto;
using DocFlowHub.Core.Services.Interfaces;
using DocFlowHub.Web.Extensions;
using DocFlowHub.Web.Ui;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DocFlowHub.Web.Pages.Documents;

/// <summary>
/// Document details (design 3b): editor tabs, toolbar, preview placeholder (D5) and an inspector with
/// Info / AI / Versions / Sharing tabs. Delete and move use the shared dialogs (Documents handlers).
/// </summary>
[Authorize]
public class DetailsModel : PageModel
{
    private static readonly string[] Tabs = { "info", "ai", "versions", "sharing" };

    private readonly IDocumentBrowserService _browser;
    private readonly IDocumentService _documentService;
    private readonly IDocumentSummaryService _summaryService;
    private readonly IAISettingsService _aiSettings;
    private readonly IAIUsageTrackingService _aiUsage;
    private readonly ITeamService _teamService;
    private readonly IDemoModeService _demoMode;
    private readonly ILogger<DetailsModel> _logger;

    public DetailsModel(
        IDocumentBrowserService browser,
        IDocumentService documentService,
        IDocumentSummaryService summaryService,
        IAISettingsService aiSettings,
        IAIUsageTrackingService aiUsage,
        ITeamService teamService,
        IDemoModeService demoMode,
        ILogger<DetailsModel> logger)
    {
        _browser = browser;
        _documentService = documentService;
        _summaryService = summaryService;
        _aiSettings = aiSettings;
        _aiUsage = aiUsage;
        _teamService = teamService;
        _demoMode = demoMode;
        _logger = logger;
    }

    [BindProperty(SupportsGet = true)]
    public int Id { get; set; }

    /// <summary>Inspector tab: info (default), ai, versions or sharing.</summary>
    [BindProperty(SupportsGet = true)]
    public string? Tab { get; set; }

    public DocumentDetails Doc { get; private set; } = new();
    public DocumentTree Tree { get; private set; } = new();
    public IReadOnlyList<TeamDto> Teams { get; private set; } = Array.Empty<TeamDto>();
    public bool IsDemo => _demoMode.IsEnabled;

    /// <summary>Anyone who can open the document may edit it and upload versions (S12-4).</summary>
    public bool CanEdit => !IsDemo;

    /// <summary>Move, share, delete and delete versions: the owner only (S12-4).</summary>
    public bool CanManage => Doc.IsOwner && !IsDemo;

    public string ActiveTab => Tabs.Contains(Tab) ? Tab! : "info";

    /// <summary>"Summaries" switch in Settings · AI (it only hides the AI option when uploading).</summary>
    public bool SummariesOn { get; private set; } = true;

    /// <summary>Today's AI requests and the fixed limits (design 7d "failed · limit").</summary>
    public UserRateLimits? Limits { get; private set; }

    public bool LimitReached => Limits != null &&
        (Limits.CurrentDailyOperations >= Limits.DailyOperationLimit || Limits.CurrentHourlyOperations >= Limits.HourlyOperationLimit);
    public string DemoTip => "Read-only demo — changes are disabled";
    public string OwnerTip => IsDemo ? DemoTip : "Only the owner can do this";

    public async Task<IActionResult> OnGetAsync()
    {
        var userId = User.GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return RedirectToPage("/Account/Login");
        }

        var details = await _browser.GetDetailsAsync(userId, Id);
        if (!details.Succeeded || details.Data == null)
        {
            // Distinguish "not yours" (403 page) from "does not exist" (404 page).
            var exists = await _documentService.GetDocumentByIdAsync(Id);
            return exists.Succeeded ? Forbid() : NotFound();
        }
        Doc = details.Data;

        if (Doc.IsOwner)
        {
            var tree = await _browser.GetTreeAsync(userId);
            if (tree.Succeeded && tree.Data != null) Tree = tree.Data;

            var teams = await _teamService.GetUserTeamsAsync(userId, new TeamFilter { PageSize = 100 });
            if (teams.Succeeded && teams.Data != null) Teams = teams.Data.Items.OrderBy(t => t.Name).ToList();
        }

        if (Doc.Summary == null)
        {
            // Only needed for the "no summary yet" card; a failure leaves the plain card.
            try
            {
                var settings = await _aiSettings.GetUserSettingsAsync(userId);
                if (settings.Succeeded && settings.Data != null) SummariesOn = settings.Data.SummarizationEnabled;
                var limits = await _aiUsage.GetUserRateLimitsAsync(userId);
                if (limits.Succeeded) Limits = limits.Data;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not load the AI state of document {DocumentId}", Id);
            }
        }

        ViewData["StatusPath"] = string.Join(" / ",
            new[] { Doc.Location, Doc.Title + Doc.FileType.ToLowerInvariant() }.Where(p => !string.IsNullOrEmpty(p)));
        return Page();
    }

    /// <summary>Downloads one version (the current one when versionId is not given).</summary>
    public async Task<IActionResult> OnPostDownloadAsync(int? versionId)
    {
        var userId = User.GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var details = await _browser.GetDetailsAsync(userId, Id);
        if (!details.Succeeded || details.Data == null)
        {
            return Forbid();
        }

        var version = versionId == null
            ? details.Data.Current
            : details.Data.Versions.FirstOrDefault(v => v.Id == versionId);
        if (version == null)
        {
            return Back("versions", error: "That version no longer exists.");
        }

        var download = await _documentService.DownloadDocumentVersionAsync(Id, version.Id);
        if (!download.Succeeded || download.Data == null)
        {
            return Back("versions", error: download.Error ?? "The file could not be downloaded.");
        }

        var extension = string.IsNullOrEmpty(version.FileType) ? details.Data.FileType : version.FileType;
        var fileName = version == details.Data.Current
            ? $"{details.Data.Title}{extension}"
            : $"{details.Data.Title}_v{version.Number}{extension}";
        return File(download.Data, ContentType(extension), fileName);
    }

    public async Task<IActionResult> OnPostDeleteVersionAsync(int versionId)
    {
        var userId = User.GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var result = await _documentService.DeleteDocumentVersionAsync(Id, versionId, userId);
        return result.Succeeded
            ? Back("versions", success: "Version deleted")
            : Back("versions", error: result.Error ?? "The version could not be deleted.");
    }

    public async Task<IActionResult> OnPostShareAsync(int? teamId)
    {
        var userId = User.GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var details = await _browser.GetDetailsAsync(userId, Id);
        if (!details.Succeeded || details.Data == null || !details.Data.IsOwner)
        {
            return Back("sharing", error: "Only the owner can change who can see this document.");
        }

        if (teamId == null)
        {
            if (details.Data.TeamId == null)
            {
                return Back("sharing");
            }
            var unshare = await _documentService.UnshareDocumentFromTeamAsync(Id);
            return unshare.Succeeded
                ? Back("sharing", success: "The document is private again")
                : Back("sharing", error: unshare.Error ?? "The document could not be made private.");
        }

        var teams = await _teamService.GetUserTeamsAsync(userId, new TeamFilter { PageSize = 100 });
        var team = teams.Succeeded ? teams.Data?.Items.FirstOrDefault(t => t.Id == teamId) : null;
        if (team == null)
        {
            return Back("sharing", error: "You can only share with a team you belong to.");
        }

        var share = await _documentService.ShareDocumentWithTeamAsync(Id, team.Id);
        return share.Succeeded
            ? Back("sharing", success: $"Shared with {team.Name}")
            : Back("sharing", error: share.Error ?? "The document could not be shared.");
    }

    /// <summary>Generates the AI summary of a document that has none (it never replaces an existing one).</summary>
    public async Task<IActionResult> OnPostGenerateSummaryAsync()
    {
        var userId = User.GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var details = await _browser.GetDetailsAsync(userId, Id);
        if (!details.Succeeded || details.Data == null)
        {
            return Forbid();
        }
        if (details.Data.Summary != null)
        {
            return Back("ai");
        }

        // The daily and hourly limits were only counted before; a manual summary now respects them.
        if (!await _aiUsage.CheckRateLimitsAsync(userId, "Summary"))
        {
            return Back("ai", error: "Your AI limit is reached. It resets at midnight UTC (hourly limit: at the top of the hour).");
        }

        var settings = await _aiSettings.GetUserSettingsAsync(userId);
        var model = settings.Succeeded && settings.Data != null ? settings.Data.PreferredModel : AIModelHelper.GetDefaultModel();
        try
        {
            var result = await _summaryService.GenerateSummaryAsync(Id, model);
            return result.Succeeded
                ? Back("ai", success: "AI summary ready")
                : Back("ai", error: result.Error ?? "The summary could not be generated.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Generating the summary of document {DocumentId} failed", Id);
            return Back("ai", error: "The summary could not be generated. Try again later.");
        }
    }

    private IActionResult Back(string tab, string? success = null, string? error = null)
    {
        if (success != null) TempData["ToastSuccess"] = success;
        if (error != null) TempData["ToastError"] = error;
        return RedirectToPage(new { id = Id, tab = tab == "info" ? null : tab });
    }

    private static string ContentType(string fileExtension) => fileExtension.ToLowerInvariant() switch
    {
        ".pdf" => "application/pdf",
        ".doc" => "application/msword",
        ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        ".md" => "text/markdown",
        ".txt" => "text/plain",
        ".jpg" or ".jpeg" => "image/jpeg",
        ".png" => "image/png",
        ".gif" => "image/gif",
        _ => "application/octet-stream",
    };

    /// <summary>MIME type shown in the inspector ("application/pdf").</summary>
    public static string MimeType(string fileExtension) => ContentType(fileExtension);
}
