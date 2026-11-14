using System.ComponentModel.DataAnnotations;
using DocFlowHub.Core.Models.Documents.Browser;
using DocFlowHub.Core.Models.Documents.Dto;
using DocFlowHub.Core.Services.Interfaces;
using DocFlowHub.Infrastructure.Services.Storage;
using DocFlowHub.Web.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;

namespace DocFlowHub.Web.Pages.Documents;

/// <summary>
/// Edit a document (no mock; follows Upload, decision S12-8): its details, and a new version. Anyone
/// who can open the document may do both (the backend's rule, S12-4). Results are toasts after a redirect.
/// </summary>
[Authorize]
public class EditModel : PageModel
{
    private readonly IDocumentService _documentService;
    private readonly IDocumentCategoryService _categoryService;
    private readonly IDocumentBrowserService _browser;
    private readonly DocumentStorageOptions _storage;

    public EditModel(
        IDocumentService documentService,
        IDocumentCategoryService categoryService,
        IDocumentBrowserService browser,
        IOptions<DocumentStorageOptions> storage)
    {
        _documentService = documentService;
        _categoryService = categoryService;
        _browser = browser;
        _storage = storage.Value;
    }

    [BindProperty(SupportsGet = true)]
    public int Id { get; set; }

    [BindProperty]
    [Required(ErrorMessage = "Enter a title")]
    [StringLength(200, ErrorMessage = "The title can have at most 200 characters")]
    public string Title { get; set; } = string.Empty;

    [BindProperty]
    [StringLength(1000, ErrorMessage = "The description can have at most 1000 characters")]
    public string? Description { get; set; }

    [BindProperty]
    public List<int> SelectedCategoryIds { get; set; } = new();

    [BindProperty]
    public IFormFile? NewVersionFile { get; set; }

    [BindProperty]
    [StringLength(500, ErrorMessage = "The note can have at most 500 characters")]
    public string? ChangeSummary { get; set; }

    public DocumentDetails Doc { get; private set; } = new();
    public IReadOnlyList<DocumentCategoryDto> Categories { get; private set; } = Array.Empty<DocumentCategoryDto>();

    /// <summary>Which form failed validation ("details" or "version"), to show its errors.</summary>
    public string? FailedForm { get; private set; }
    public string? ErrorMessage { get; private set; }

    public IReadOnlyList<string> AllowedTypes => _storage.AllowedFileTypes;
    public long MaxFileBytes => _storage.MaxFileSizeBytes;

    public async Task<IActionResult> OnGetAsync()
    {
        var load = await LoadAsync();
        if (load != null) return load;

        Title = Doc.Title;
        Description = Doc.Description;
        var current = await _categoryService.GetDocumentCategoriesAsync(Id);
        if (current.Succeeded && current.Data != null)
        {
            SelectedCategoryIds = current.Data.Select(c => c.Id).ToList();
        }
        return Page();
    }

    public async Task<IActionResult> OnPostUpdateMetadataAsync()
    {
        var load = await LoadAsync();
        if (load != null) return load;

        ModelState.Remove(nameof(ChangeSummary));
        ModelState.Remove(nameof(NewVersionFile));
        if (!ModelState.IsValid)
        {
            FailedForm = "details";
            return Page();
        }

        var update = await _documentService.UpdateDocumentAsync(Id, new UpdateDocumentRequest
        {
            Title = Title.Trim(),
            Description = Description?.Trim() ?? string.Empty,
        });
        if (!update.Succeeded)
        {
            FailedForm = "details";
            ErrorMessage = update.Error ?? "The details could not be saved.";
            return Page();
        }

        var categories = await _categoryService.AssignCategoriesToDocumentAsync(Id, SelectedCategoryIds);
        if (!categories.Succeeded)
        {
            FailedForm = "details";
            ErrorMessage = categories.Error ?? "The categories could not be saved.";
            return Page();
        }

        TempData["ToastSuccess"] = "Details saved";
        return RedirectToPage("/Documents/Details", new { id = Id });
    }

    public async Task<IActionResult> OnPostUploadNewVersionAsync()
    {
        var load = await LoadAsync();
        if (load != null) return load;

        // The details form's fields are not part of this post.
        ModelState.Remove(nameof(Title));
        ModelState.Remove(nameof(Description));
        ModelState.Remove(nameof(SelectedCategoryIds));
        Title = Doc.Title;
        Description = Doc.Description;

        if (NewVersionFile == null || NewVersionFile.Length == 0)
        {
            ModelState.AddModelError(nameof(NewVersionFile), "Choose a file for the new version");
        }
        if (!ModelState.IsValid)
        {
            FailedForm = "version";
            return Page();
        }

        var userId = User.GetUserId()!;
        var upload = await _documentService.UploadNewVersionAsync(new UploadVersionRequest
        {
            DocumentId = Id,
            File = NewVersionFile!,
            ChangeSummary = ChangeSummary?.Trim() ?? string.Empty,
            UserId = userId,
        });
        if (!upload.Succeeded)
        {
            FailedForm = "version";
            ErrorMessage = upload.Error ?? "The new version could not be uploaded.";
            return Page();
        }

        var number = (Doc.Current?.Number ?? 0) + 1;
        TempData["ToastSuccess"] = $"Uploaded v{number}";
        return RedirectToPage("/Documents/Details", new { id = Id, tab = "versions" });
    }

    /// <summary>Loads the document the user can see, or returns the 403/404 result to send instead.</summary>
    private async Task<IActionResult?> LoadAsync()
    {
        var userId = User.GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return RedirectToPage("/Account/Login");
        }

        var details = await _browser.GetDetailsAsync(userId, Id);
        if (!details.Succeeded || details.Data == null)
        {
            var exists = await _documentService.GetDocumentByIdAsync(Id);
            return exists.Succeeded ? Forbid() : NotFound();
        }
        Doc = details.Data;

        var categories = await _categoryService.GetAllCategoriesAsync();
        if (categories.Succeeded && categories.Data != null)
        {
            Categories = categories.Data.OrderBy(c => c.Name).ToList();
        }
        return null;
    }
}
