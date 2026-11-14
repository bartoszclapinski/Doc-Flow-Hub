using DocFlowHub.Core.Models.AI;
using DocFlowHub.Core.Models.Documents.Browser;
using DocFlowHub.Core.Services.Interfaces;
using DocFlowHub.Web.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DocFlowHub.Web.Pages.Documents;

/// <summary>
/// Version compare (design 3i, decision S12-5): two versions side by side with the AI comparison in
/// the inspector. GET only reads a saved comparison; generating one is a POST (disabled in the demo).
/// </summary>
[Authorize]
public class CompareModel : PageModel
{
    private readonly IDocumentBrowserService _browser;
    private readonly IDocumentService _documentService;
    private readonly IVersionComparisonService _comparisons;
    private readonly IAISettingsService _aiSettings;
    private readonly IDemoModeService _demoMode;
    private readonly ILogger<CompareModel> _logger;

    public CompareModel(
        IDocumentBrowserService browser,
        IDocumentService documentService,
        IVersionComparisonService comparisons,
        IAISettingsService aiSettings,
        IDemoModeService demoMode,
        ILogger<CompareModel> logger)
    {
        _browser = browser;
        _documentService = documentService;
        _comparisons = comparisons;
        _aiSettings = aiSettings;
        _demoMode = demoMode;
        _logger = logger;
    }

    [BindProperty(SupportsGet = true)]
    public int Id { get; set; }

    public DocumentComparisonView View { get; private set; } = new();
    public bool IsDemo => _demoMode.IsEnabled;

    public async Task<IActionResult> OnGetAsync(int? from, int? to)
    {
        var userId = User.GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return RedirectToPage("/Account/Login");
        }

        var result = await _browser.GetComparisonAsync(userId, Id, from, to);
        if (!result.Succeeded || result.Data == null)
        {
            var details = await _browser.GetDetailsAsync(userId, Id);
            if (details.Succeeded)
            {
                // The user can see the document, there is just nothing to compare (one version).
                TempData["ToastError"] = result.Error ?? "These versions can't be compared.";
                return RedirectToPage("/Documents/Details", new { id = Id });
            }
            var exists = await _documentService.GetDocumentByIdAsync(Id);
            return exists.Succeeded ? Forbid() : NotFound();
        }

        View = result.Data;
        ViewData["StatusPath"] = string.Join(" / ",
            new[] { View.Location, View.Title + View.FileType.ToLowerInvariant() }.Where(p => !string.IsNullOrEmpty(p)));
        return Page();
    }

    /// <summary>Asks the AI to compare two versions (uses the user's preferred model) and shows the result.</summary>
    public async Task<IActionResult> OnPostGenerateAsync(int from, int to)
    {
        var userId = User.GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var view = await _browser.GetComparisonAsync(userId, Id, from, to);
        if (!view.Succeeded || view.Data == null)
        {
            return Forbid();
        }
        var (older, newer) = (view.Data.From.Number, view.Data.To.Number);

        var settings = await _aiSettings.GetUserSettingsAsync(userId);
        var model = settings.Succeeded && settings.Data != null ? settings.Data.PreferredModel : AIModelHelper.GetDefaultModel();
        try
        {
            var result = await _comparisons.CompareDocumentVersionsAsync(Id, older, newer, model, userId);
            if (result.Succeeded)
            {
                TempData["ToastSuccess"] = $"AI comparison of v{older} and v{newer} ready";
            }
            else
            {
                TempData["ToastError"] = result.Error ?? "The versions could not be compared.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Comparing versions {From} and {To} of document {DocumentId} failed", older, newer, Id);
            TempData["ToastError"] = "The versions could not be compared. Try again later.";
        }
        return RedirectToPage(new { id = Id, from = older, to = newer });
    }

    /// <summary>Chips for the comparison (design 3i): change kinds and the significance.</summary>
    public static IEnumerable<(string Label, bool High)> Chips(DocumentComparisonResult comparison)
    {
        var kinds = new (ChangeType Flag, string Label)[]
        {
            (ChangeType.ContentAddition, "additions"),
            (ChangeType.ContentDeletion, "deletions"),
            (ChangeType.ContentModification, "edits"),
            (ChangeType.StructuralChange, "structure"),
            (ChangeType.SemanticChange, "meaning"),
        };
        foreach (var (flag, label) in kinds)
        {
            if (comparison.ChangeType.HasFlag(flag)) yield return (label, false);
        }
        yield return (comparison.Significance switch
        {
            ChangeSignificance.High => "high impact",
            ChangeSignificance.Medium => "medium impact",
            _ => "low impact",
        }, comparison.Significance == ChangeSignificance.High);
    }
}
