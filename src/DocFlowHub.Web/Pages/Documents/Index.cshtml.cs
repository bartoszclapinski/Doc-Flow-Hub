using System.Text.Json;
using DocFlowHub.Core.Models.Documents;
using DocFlowHub.Core.Models.Documents.Browser;
using DocFlowHub.Core.Models.Documents.Dto;
using DocFlowHub.Core.Services.Interfaces;
using DocFlowHub.Web.Extensions;
using DocFlowHub.Web.Ui;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DocFlowHub.Web.Pages.Documents;

/// <summary>
/// Documents (design 3h): project tree, filtered table with multi-select, details inspector.
/// Reads go through <see cref="IDocumentBrowserService"/>; changes use the existing document service.
/// </summary>
[Authorize]
public class IndexModel : PageModel
{
    private const int MaxBatch = 100;

    private readonly IDocumentBrowserService _browser;
    private readonly IDocumentService _documentService;
    private readonly IDocumentCategoryService _categoryService;
    private readonly IDemoModeService _demoMode;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(
        IDocumentBrowserService browser,
        IDocumentService documentService,
        IDocumentCategoryService categoryService,
        IDemoModeService demoMode,
        ILogger<IndexModel> logger)
    {
        _browser = browser;
        _documentService = documentService;
        _categoryService = categoryService;
        _demoMode = demoMode;
        _logger = logger;
    }

    public DocumentListState State { get; private set; } = new();
    public DocumentTree Tree { get; private set; } = new();
    public DocumentListPage? Documents { get; private set; }
    public IReadOnlyList<DocumentCategoryDto> Categories { get; private set; } = Array.Empty<DocumentCategoryDto>();
    public DocumentInspector? Selected { get; private set; }
    public string? ErrorMessage { get; private set; }
    public bool IsDemo => _demoMode.IsEnabled;

    /// <summary>Breadcrumb of the selected tree node: (label, href); the last one is the current node.</summary>
    public List<(string Label, string Href)> Breadcrumb { get; } = new();

    /// <summary>The selected project and folder ids, plus every folder on the way (expanded in the tree).</summary>
    public HashSet<int> OpenFolders { get; } = new();

    public async Task<IActionResult> OnGetAsync(
        string? q, string? type, string? owner, int? category, int? project, int? folder, int? team,
        string? sort, string? dir, int? page)
    {
        var userId = User.GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return RedirectToPage("/Account/Login");
        }

        State = DocumentListState.From(q, type, owner, category, project, folder, team, sort, dir, page);

        var tree = await _browser.GetTreeAsync(userId);
        if (tree.Succeeded && tree.Data != null)
        {
            Tree = tree.Data;
        }

        // A folder link carries its project; derive it when only the folder is given.
        if (State.Folder is int folderId && Tree.FindFolder(folderId) is { } hit && State.Project != hit.Project.Id)
        {
            State = State with { Project = hit.Project.Id };
        }
        BuildBreadcrumb();

        var categories = await _categoryService.GetAllCategoriesAsync();
        if (categories.Succeeded && categories.Data != null)
        {
            Categories = categories.Data.OrderBy(c => c.Name).ToList();
        }

        var list = await _browser.GetDocumentsAsync(userId, State.ToQuery());
        if (!list.Succeeded || list.Data == null)
        {
            ErrorMessage = list.Error ?? "The documents could not be loaded.";
            return Page();
        }
        Documents = list.Data;
        State = State with { Page = Documents.Page };

        if (Documents.Items.Count > 0)
        {
            var inspector = await _browser.GetInspectorAsync(userId, Documents.Items[0].Id);
            Selected = inspector.Succeeded ? inspector.Data : null;
        }

        return Page();
    }

    /// <summary>Inspector HTML for one document (loaded when a row is selected).</summary>
    public async Task<IActionResult> OnGetInspectorAsync(int id, string? returnUrl)
    {
        var userId = User.GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var result = await _browser.GetInspectorAsync(userId, id);
        if (!result.Succeeded || result.Data == null)
        {
            Response.StatusCode = StatusCodes.Status404NotFound;
            return Content("This document is no longer available.", "text/plain");
        }

        return Partial("_DocumentInspector", new DocumentInspectorView(result.Data, IsDemo, SafeReturnUrl(returnUrl)));
    }

    /// <summary>Downloads the current version. Also used by Folders/Details.</summary>
    public async Task<IActionResult> OnPostDownloadAsync(int documentId, string? returnUrl)
    {
        var userId = User.GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var access = await _documentService.CanUserAccessDocumentAsync(documentId, userId);
        if (!access.Succeeded || !access.Data)
        {
            return Forbid();
        }

        var document = await _documentService.GetDocumentByIdAsync(documentId);
        if (!document.Succeeded || document.Data?.CurrentVersionId == null)
        {
            return Back(returnUrl, error: document.Error ?? "This document has no file to download.");
        }

        var download = await _documentService.DownloadDocumentVersionAsync(documentId, document.Data.CurrentVersionId.Value);
        if (!download.Succeeded || download.Data == null)
        {
            return Back(returnUrl, error: download.Error ?? "The file could not be downloaded.");
        }

        var fileName = $"{document.Data.Title}{document.Data.FileType}";
        return File(download.Data, ContentType(document.Data.FileType), fileName);
    }

    public async Task<IActionResult> OnPostDeleteAsync(int documentId, string? returnUrl)
    {
        var userId = User.GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var document = await _documentService.GetDocumentByIdAsync(documentId);
        if (!document.Succeeded || document.Data == null)
        {
            return Back(returnUrl, error: document.Error ?? "Document not found.");
        }
        if (document.Data.OwnerId != userId)
        {
            return Back(returnUrl, error: "You can only delete documents that you own.");
        }

        var result = await _documentService.DeleteDocumentAsync(documentId);
        return result.Succeeded
            ? Back(returnUrl, success: $"Deleted \"{document.Data.Title}\"")
            : Back(returnUrl, error: result.Error ?? "The document could not be deleted.");
    }

    /// <summary>Soft-deletes several documents; the JSON has one result per document (design 7h).</summary>
    public async Task<IActionResult> OnPostBulkDeleteAsync([FromBody] BulkDeleteRequest? request)
    {
        var userId = User.GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return new JsonResult(new { success = false, message = "You are signed out. Sign in and try again." });
        }

        var ids = request?.DocumentIds?.Distinct().ToList() ?? new List<int>();
        if (ids.Count == 0 || ids.Count > MaxBatch)
        {
            return new JsonResult(new { success = false, message = $"Select between 1 and {MaxBatch} documents." });
        }

        var result = await _documentService.BulkDeleteDocumentsAsync(ids, userId);
        if (!result.Succeeded || result.Data == null)
        {
            return new JsonResult(new { success = false, message = result.Error ?? "The documents could not be deleted." });
        }

        return new JsonResult(new
        {
            success = true,
            deleted = result.Data.SuccessfulDeletions,
            failed = result.Data.FailedDeletions,
            results = result.Data.Results.Select(r => new
            {
                documentId = r.DocumentId,
                success = r.Success,
                reason = r.Success ? null : DeleteReason(r.ErrorMessage),
            }),
        });
    }

    /// <summary>Moves one or more documents to a project root or a folder (design 7i), one result per document.</summary>
    public async Task<IActionResult> OnPostMoveAsync([FromBody] MoveRequest? request)
    {
        var userId = User.GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return new JsonResult(new { success = false, message = "You are signed out. Sign in and try again." });
        }

        var ids = request?.DocumentIds?.Distinct().ToList() ?? new List<int>();
        if (ids.Count == 0 || ids.Count > MaxBatch)
        {
            return new JsonResult(new { success = false, message = $"Select between 1 and {MaxBatch} documents." });
        }
        if (request!.ProjectId == null && request.FolderId != null)
        {
            return new JsonResult(new { success = false, message = "Choose a project or a folder." });
        }

        var results = new List<object>();
        var moved = 0;
        foreach (var id in ids)
        {
            var result = await _documentService.MoveDocumentAsync(new MoveDocumentRequest
            {
                DocumentId = id,
                ProjectId = request.ProjectId,
                FolderId = request.FolderId,
                UserId = userId,
            });
            if (result.Succeeded) moved++;
            results.Add(new
            {
                documentId = id,
                success = result.Succeeded,
                reason = result.Succeeded ? null : MoveReason(result.Error),
            });
        }

        if (moved < ids.Count)
        {
            _logger.LogInformation("Moved {Moved} of {Total} documents for user {UserId}", moved, ids.Count, userId);
        }
        return new JsonResult(new { success = true, moved, failed = ids.Count - moved, results });
    }

    /// <summary>Projects and folders for the move picker, embedded in the page as JSON.</summary>
    public string MoveTreeJson() => JsonSerializer.Serialize(
        Tree.Projects.Select(p => new
        {
            id = p.Id,
            name = p.Name,
            color = Dfh.ProjectColor(p.Color, p.Id),
            folders = p.Folders.Select(FolderNode),
        }));

    private static object FolderNode(DocumentTreeFolder folder) => new
    {
        id = folder.Id,
        name = folder.Name,
        count = folder.DocumentCount,
        children = folder.Children.Select(FolderNode),
    };

    private void BuildBreadcrumb()
    {
        if (State.Folder is int folderId && Tree.FindFolder(folderId) is { } hit)
        {
            Breadcrumb.Add((hit.Project.Name, State.Scoped(project: hit.Project.Id).Href));
            var chain = new List<DocumentTreeFolder>();
            var node = hit.Folder;
            var byId = hit.Project.AllFolders().ToDictionary(f => f.Id);
            while (node != null)
            {
                chain.Insert(0, node);
                node = node.ParentId is int parentId && byId.TryGetValue(parentId, out var parent) ? parent : null;
            }
            foreach (var f in chain)
            {
                OpenFolders.Add(f.Id);
                Breadcrumb.Add((f.Name, State.Scoped(project: hit.Project.Id, folder: f.Id).Href));
            }
        }
        else if (State.Project is int projectId && Tree.FindProject(projectId) is { } project)
        {
            Breadcrumb.Add((project.Name, State.Scoped(project: project.Id).Href));
        }
        else if (State.Team is int teamId && Tree.Teams.FirstOrDefault(t => t.Id == teamId) is { } t)
        {
            Breadcrumb.Add(("Teams", State.Scoped().Href));
            Breadcrumb.Add((t.Name, State.Scoped(team: t.Id).Href));
        }
        else if (State.HasScope)
        {
            // A project, folder or team the user cannot see: the list is empty, say what was asked for.
            Breadcrumb.Add(("Unavailable", State.Href));
        }
        else
        {
            Breadcrumb.Add(("All documents", State.Scoped().Href));
        }
    }

    private IActionResult Back(string? returnUrl, string? success = null, string? error = null)
    {
        if (success != null) TempData["ToastSuccess"] = success;
        if (error != null) TempData["ToastError"] = error;
        return LocalRedirect(SafeReturnUrl(returnUrl));
    }

    private string SafeReturnUrl(string? returnUrl) =>
        !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl) ? returnUrl : "/Documents";

    private static string DeleteReason(string? error) =>
        error != null && error.Contains("own", StringComparison.OrdinalIgnoreCase) ? "not owner · skipped"
        : error != null && error.Contains("not found", StringComparison.OrdinalIgnoreCase) ? "already deleted"
        : "failed";

    private static string MoveReason(string? error) =>
        error != null && error.Contains("authorized", StringComparison.OrdinalIgnoreCase) ? "not owner · skipped"
        : error ?? "failed";

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

    public class MoveRequest
    {
        public List<int> DocumentIds { get; set; } = new();
        public int? ProjectId { get; set; }
        public int? FolderId { get; set; }
    }
}

/// <summary>Model of the inspector partial.</summary>
public record DocumentInspectorView(DocumentInspector Doc, bool IsDemo, string ReturnUrl);

/// <summary>Model of the recursive tree-folder partial.</summary>
public record DocumentTreeFolderNode(
    DocumentTreeFolder Folder, int ProjectId, int Level, DocumentListState State, IReadOnlySet<int> OpenFolders);
