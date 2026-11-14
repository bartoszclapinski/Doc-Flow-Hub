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
/// Upload (design 3c, decision S12-7): one file, its details and destination, and whether to
/// generate an AI summary. The "Destination" inspector shows the project tree.
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

        await LoadAsync(userId);
        NormalizeDestination();
        GenerateSummary = AIFeaturesEnabled;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var userId = User.GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return RedirectToPage("/Account/Login");
        }

        await LoadAsync(userId);

        if (File == null || File.Length == 0)
        {
            ErrorMessage = "Choose a file to upload.";
            return Page();
        }

        // The destination must be one of the user's own projects and, if given, a folder in it.
        if (ProjectId != null && Tree.FindProject(ProjectId.Value) == null)
        {
            ErrorMessage = "Choose one of your projects, or no project.";
            return Page();
        }
        if (FolderId != null && (ProjectId == null || Tree.FindFolder(FolderId.Value)?.Project.Id != ProjectId))
        {
            ErrorMessage = "The folder must belong to the chosen project.";
            return Page();
        }
        if (TeamId != null && Teams.All(t => t.Id != TeamId))
        {
            ErrorMessage = "You can only share with a team you belong to.";
            return Page();
        }

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
            GenerateSummary = GenerateSummary && AIFeaturesEnabled,
        }, File);

        if (!result.Succeeded || result.Data == null)
        {
            ErrorMessage = result.Error ?? "The file could not be uploaded.";
            return Page();
        }

        TempData["ToastSuccess"] = GenerateSummary && AIFeaturesEnabled
            ? $"Uploaded \"{result.Data.Title}\". The AI summary will appear in a moment."
            : $"Uploaded \"{result.Data.Title}\"";
        return RedirectToPage("/Documents/Details", new { id = result.Data.Id });
    }

    private async Task LoadAsync(string userId)
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
