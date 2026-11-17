using System.Text.Json;
using DocFlowHub.Core.Models.AI;
using DocFlowHub.Core.Models.Documents.Browser;
using DocFlowHub.Core.Models.Documents.Dto;
using DocFlowHub.Core.Models.Teams;
using DocFlowHub.Core.Models.Teams.Dto;
using DocFlowHub.Core.Services.Interfaces;
using DocFlowHub.Infrastructure.Services.Storage;
using DocFlowHub.Web.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;

namespace DocFlowHub.Web.Pages.Documents;

/// <summary>
/// Upload (design 3c, decision S12-7): one file, its details and destination. Sprint 23 brought back the
/// AI options of the old screen and made them real: the model and the level of detail of the upload
/// summary, a cost and time estimate, and saving them as the user's defaults. The "Destination"
/// inspector shows the project tree.
/// </summary>
[Authorize]
public class UploadModel : PageModel
{
    private readonly IDocumentService _documentService;
    private readonly IDocumentCategoryService _categoryService;
    private readonly ITeamService _teamService;
    private readonly IAISettingsService _aiSettings;
    private readonly IAIUsageTrackingService _aiUsage;
    private readonly IDocumentBrowserService _browser;
    private readonly DocumentStorageOptions _storage;
    private readonly ILogger<UploadModel> _logger;

    public UploadModel(
        IDocumentService documentService,
        IDocumentCategoryService categoryService,
        ITeamService teamService,
        IAISettingsService aiSettings,
        IAIUsageTrackingService aiUsage,
        IDocumentBrowserService browser,
        IOptions<DocumentStorageOptions> storage,
        ILogger<UploadModel> logger)
    {
        _documentService = documentService;
        _categoryService = categoryService;
        _teamService = teamService;
        _aiSettings = aiSettings;
        _aiUsage = aiUsage;
        _browser = browser;
        _storage = storage.Value;
        _logger = logger;
    }

    [BindProperty]
    public string Title { get; set; } = string.Empty;

    [BindProperty]
    public string? Description { get; set; }

    [BindProperty]
    public IFormFile? File { get; set; }

    [BindProperty]
    public List<int> SelectedCategoryIds { get; set; } = new();

    [BindProperty]
    public int? TeamId { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? ProjectId { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? FolderId { get; set; }

    [BindProperty]
    public bool GenerateSummary { get; set; } = true;

    /// <summary>The model for the upload summary (S23-3).</summary>
    [BindProperty]
    public AIModel SummaryModel { get; set; } = AIModelHelper.GetDefaultModel();

    [BindProperty]
    public SummaryDetail SummaryDetail { get; set; } = SummaryDetail.Standard;

    /// <summary>Save the summary switch, model and detail as the user's AI defaults.</summary>
    [BindProperty]
    public bool RememberDefaults { get; set; }

    /// <summary>Summaries are on in Settings · AI (and AI is not turned off for the account).</summary>
    public bool SummariesEnabled { get; private set; }

    /// <summary>The user's own API key is used instead of the system's.</summary>
    public bool UsesOwnKey { get; private set; }

    /// <summary>Files the summary can read text from; images and .doc get no summary (S23-4).</summary>
    public static readonly string[] SummarisableTypes = { ".pdf", ".docx", ".txt", ".md" };

    /// <summary>The models with the figures the estimate needs, for upload.js.</summary>
    public string ModelsJson => JsonSerializer.Serialize(AIModelHelper.GetAllModels().Select(m => new
    {
        id = m.ToString(),
        input = AIModelFacts.PricePerMillionTokens(m).Input,
        output = AIModelFacts.PricePerMillionTokens(m).Output,
        seconds = AIModelFacts.TypicalSeconds(m),
        strengths = AIModelFacts.Strengths(m),
    }));

    public static string DetailsJson => JsonSerializer.Serialize(Enum.GetValues<SummaryDetail>().ToDictionary(
        d => d.ToString(), d => SummaryDetailHelper.TypicalOutputTokens(d)));

    public IReadOnlyList<DocumentCategoryDto> Categories { get; private set; } = Array.Empty<DocumentCategoryDto>();
    public IReadOnlyList<TeamDto> Teams { get; private set; } = Array.Empty<TeamDto>();
    public DocumentTree Tree { get; private set; } = new();
    public bool AIFeaturesEnabled { get; private set; }
    public int? AiRequestsToday { get; private set; }
    public int? AiDailyLimit { get; private set; }
    public string? ErrorMessage { get; private set; }

    /// <summary>"pdf docx txt md …" for the dropzone hint, and the input's accept list.</summary>
    public IReadOnlyList<string> AllowedTypes => _storage.AllowedFileTypes;
    public long MaxFileBytes => _storage.MaxFileSizeBytes;

    public async Task<IActionResult> OnGetAsync()
    {
        var userId = User.GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return RedirectToPage("/Account/Login");
        }

        await LoadAsync(userId, applyDefaults: true);
        NormalizeDestination();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var userId = User.GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return RedirectToPage("/Account/Login");
        }

        await LoadAsync(userId, applyDefaults: false);
        if (!Enum.IsDefined(SummaryModel)) SummaryModel = AIModelHelper.GetDefaultModel();
        if (!Enum.IsDefined(SummaryDetail)) SummaryDetail = SummaryDetail.Standard;

        if (File == null || File.Length == 0)
        {
            return Fail("Choose a file to upload.");
        }

        // The destination must be one of the user's own projects and, if given, a folder in it.
        if (ProjectId != null && Tree.FindProject(ProjectId.Value) == null)
        {
            return Fail("Choose one of your projects, or no project.");
        }
        if (FolderId != null && (ProjectId == null || Tree.FindFolder(FolderId.Value)?.Project.Id != ProjectId))
        {
            return Fail("The folder must belong to the chosen project.");
        }
        if (TeamId != null && Teams.All(t => t.Id != TeamId))
        {
            return Fail("You can only share with a team you belong to.");
        }

        if (RememberDefaults && SummariesEnabled)
        {
            await RememberAsync(userId);
        }

        // No summary for a file without readable text: it would only fail after the upload (S23-4).
        var summarisable = SummarisableTypes.Contains(Path.GetExtension(File.FileName).ToLowerInvariant());
        var summarise = GenerateSummary && SummariesEnabled && summarisable;

        var title = string.IsNullOrWhiteSpace(Title) ? Path.GetFileNameWithoutExtension(File.FileName) : Title.Trim();
        var result = await _documentService.CreateDocumentAsync(new CreateDocumentRequest
        {
            Title = title,
            Description = Description?.Trim() ?? string.Empty,
            CategoryIds = SelectedCategoryIds,
            TeamId = TeamId,
            ProjectId = ProjectId,
            FolderId = FolderId,
            OwnerId = userId,
            GenerateSummary = summarise,
            SummaryModel = SummaryModel,
            SummaryDetail = SummaryDetail,
        }, File);

        if (!result.Succeeded || result.Data == null)
        {
            return Fail(result.Error ?? "The file could not be uploaded.");
        }

        TempData["ToastSuccess"] = summarise
            ? $"Uploaded \"{result.Data.Title}\". The AI summary ({SummaryModel.ToApiString()}, {SummaryDetail.ToString().ToLowerInvariant()}) will appear in a moment."
            : $"Uploaded \"{result.Data.Title}\"";
        // upload.js sends the form itself to show progress (S23-5); it gets the address to go to.
        return IsProgressUpload
            ? new JsonResult(new { url = Url.Page("/Documents/Details", new { id = result.Data.Id }) })
            : RedirectToPage("/Documents/Details", new { id = result.Data.Id });
    }

    private bool IsProgressUpload => Request.Headers["X-DFH-Upload"] == "1";

    /// <summary>The form again with the error, or the error as JSON for the progress upload.</summary>
    private IActionResult Fail(string message)
    {
        if (IsProgressUpload)
        {
            return new JsonResult(new { error = message }) { StatusCode = StatusCodes.Status400BadRequest };
        }
        ErrorMessage = message;
        return Page();
    }

    /// <summary>Saves the summary switch, model and detail as the user's AI defaults (S23-3).</summary>
    private async Task RememberAsync(string userId)
    {
        try
        {
            var settings = await _aiSettings.GetUserSettingsAsync(userId);
            if (!settings.Succeeded || settings.Data == null) return;
            settings.Data.PreferredModel = SummaryModel;
            settings.Data.QualityPreference = SummaryDetailHelper.ToQuality(SummaryDetail);
            settings.Data.AutoSummarizeOnUpload = GenerateSummary;
            await _aiSettings.UpdateUserSettingsAsync(userId, settings.Data);
        }
        catch (Exception ex)
        {
            // The upload matters more than the saved defaults.
            _logger.LogWarning(ex, "Could not save the upload AI defaults");
        }
    }

    private async Task LoadAsync(string userId, bool applyDefaults)
    {
        var categories = await _categoryService.GetAllCategoriesAsync();
        if (categories.Succeeded && categories.Data != null)
        {
            Categories = categories.Data.OrderBy(c => c.Name).ToList();
        }

        var teams = await _teamService.GetUserTeamsAsync(userId, new TeamFilter { PageSize = 100 });
        if (teams.Succeeded && teams.Data != null)
        {
            Teams = teams.Data.Items.OrderBy(t => t.Name).ToList();
        }

        var tree = await _browser.GetTreeAsync(userId);
        if (tree.Succeeded && tree.Data != null)
        {
            Tree = tree.Data;
        }

        try
        {
            var enabled = await _aiSettings.AreAIFeaturesEnabledAsync(userId);
            AIFeaturesEnabled = enabled.Succeeded && enabled.Data;
            var settings = await _aiSettings.GetUserSettingsAsync(userId);
            if (settings.Succeeded && settings.Data is { } s)
            {
                SummariesEnabled = AIFeaturesEnabled && s.SummarizationEnabled;
                UsesOwnKey = s.UseCustomApiKey && !string.IsNullOrEmpty(s.CustomApiKey);
                if (applyDefaults)
                {
                    // The defaults come from Settings · AI.
                    SummaryModel = Enum.IsDefined(s.PreferredModel) ? s.PreferredModel : AIModelHelper.GetDefaultModel();
                    SummaryDetail = SummaryDetailHelper.FromQuality(s.QualityPreference);
                    GenerateSummary = SummariesEnabled && s.AutoSummarizeOnUpload;
                }
            }
            else if (applyDefaults)
            {
                GenerateSummary = false;
            }
            var limits = await _aiUsage.GetUserRateLimitsAsync(userId);
            if (limits.Succeeded && limits.Data != null)
            {
                AiRequestsToday = limits.Data.CurrentDailyOperations;
                AiDailyLimit = limits.Data.DailyOperationLimit;
            }
        }
        catch (Exception ex)
        {
            // The AI options are optional; the upload itself still works.
            _logger.LogWarning(ex, "Could not load AI settings for the upload page");
        }
    }

    /// <summary>Drops a project or folder from the query string that is not the user's.</summary>
    private void NormalizeDestination()
    {
        if (FolderId is int folderId && Tree.FindFolder(folderId) is { } hit)
        {
            ProjectId = hit.Project.Id;
            return;
        }
        FolderId = null;
        if (ProjectId is int projectId && Tree.FindProject(projectId) == null)
        {
            ProjectId = null;
        }
    }
}
