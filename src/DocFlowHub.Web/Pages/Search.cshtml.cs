using DocFlowHub.Core.Models.Documents.Browser;
using DocFlowHub.Core.Services.Interfaces;
using DocFlowHub.Web.Extensions;
using DocFlowHub.Web.Ui;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DocFlowHub.Web.Pages;

/// <summary>
/// Data for the command palette (design 7k, decision S15-1): documents by title, projects and folders
/// by name. Built on the existing read services; actions and navigation live in palette.js.
/// A plain GET (no JS) goes to the Documents search.
/// </summary>
[Authorize]
public class SearchModel : PageModel
{
    private const int MaxDocuments = 6;
    private const int MaxPlaces = 5;

    private readonly IDocumentBrowserService _browser;
    private readonly ILogger<SearchModel> _logger;

    public SearchModel(IDocumentBrowserService browser, ILogger<SearchModel> logger)
    {
        _browser = browser;
        _logger = logger;
    }

    public IActionResult OnGet(string? q) =>
        Redirect(string.IsNullOrWhiteSpace(q) ? "/Documents" : "/Documents?q=" + Uri.EscapeDataString(q.Trim()));

    public record PaletteDocument(int Id, string Title, string Type, string Kind, string Meta, string Url);
    public record PalettePlace(string Name, string Path, string Kind, int Count, string Url, int ProjectId, int? FolderId);
    public record PaletteResult(IReadOnlyList<PaletteDocument> Documents, IReadOnlyList<PalettePlace> Places);

    public async Task<IActionResult> OnGetPaletteAsync(string? q)
    {
        var tokens = Tokens(q);
        var userId = User.GetUserId();
        var documents = new List<PaletteDocument>();
        var places = new List<PalettePlace>();
        try
        {
            if (tokens.Length > 0)
            {
                // The list search matches one phrase, so ask for the longest word and check the rest here.
                var page = await _browser.GetDocumentsAsync(userId, new DocumentListQuery
                {
                    Search = tokens.OrderByDescending(t => t.Length).First(),
                    PageSize = 50,
                });
                if (page.Succeeded && page.Data != null)
                {
                    documents = page.Data.Items
                        .Where(d => Matches(d.Title, tokens))
                        .Take(MaxDocuments)
                        .Select(d =>
                        {
                            var (label, kind) = Dfh.FileType(d.FileType);
                            var meta = string.Join(" · ", new[] { d.Location, d.Version is int v ? $"v{v}" : null }.Where(s => !string.IsNullOrEmpty(s)));
                            return new PaletteDocument(d.Id, d.Title, label, kind, meta, $"/Documents/Details/{d.Id}");
                        })
                        .ToList();
                }
            }

            var tree = await _browser.GetTreeAsync(userId);
            if (tree.Succeeded && tree.Data != null)
            {
                places = Flatten(tree.Data)
                    .Where(p => tokens.Length == 0 || Matches(p.Path, tokens))
                    .Take(MaxPlaces)
                    .ToList();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Palette search failed for {Query}", q);
            return new JsonResult(new { error = "Search failed." }) { StatusCode = 500 };
        }

        return new JsonResult(new PaletteResult(documents, places));
    }

    private static string[] Tokens(string? q) =>
        (q ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Take(6)
            .ToArray();

    private static bool Matches(string text, string[] tokens) =>
        tokens.All(t => text.Contains(t, StringComparison.OrdinalIgnoreCase));

    /// <summary>Projects, then their folders depth-first, with "Project / folder / sub" paths.</summary>
    private static IEnumerable<PalettePlace> Flatten(DocumentTree tree)
    {
        foreach (var project in tree.Projects)
        {
            yield return new PalettePlace(project.Name, project.Name, "project", project.DocumentCount,
                WorkspaceLinks.Project(project.Id), project.Id, null);
            foreach (var place in Folders(project.Id, project.Name, project.Folders))
            {
                yield return place;
            }
        }
    }

    private static IEnumerable<PalettePlace> Folders(int projectId, string parentPath, IEnumerable<DocumentTreeFolder> folders)
    {
        foreach (var folder in folders)
        {
            var path = $"{parentPath} / {folder.Name}";
            yield return new PalettePlace(folder.Name, path, "folder", folder.DocumentCount,
                WorkspaceLinks.Folder(projectId, folder.Id), projectId, folder.Id);
            foreach (var child in Folders(projectId, path, folder.Children))
            {
                yield return child;
            }
        }
    }
}
