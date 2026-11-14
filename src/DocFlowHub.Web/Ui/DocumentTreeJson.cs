using System.Text.Json;
using DocFlowHub.Core.Models.Documents.Browser;

namespace DocFlowHub.Web.Ui;

/// <summary>Projects and folders for the move picker (design 7i), embedded in the page as JSON.</summary>
public static class DocumentTreeJson
{
    /// <remarks>The default encoder escapes &lt; and &gt;, so the result is safe inside a script element.</remarks>
    public static string Serialize(DocumentTree tree) => JsonSerializer.Serialize(
        tree.Projects.Select(p => new
        {
            id = p.Id,
            name = p.Name,
            color = Dfh.ProjectColor(p.Color, p.Id),
            folders = p.Folders.Select(Folder),
        }));

    private static object Folder(DocumentTreeFolder folder) => new
    {
        id = folder.Id,
        name = folder.Name,
        count = folder.DocumentCount,
        children = folder.Children.Select(Folder),
    };
}

/// <summary>Model of the shared delete / bulk delete / move dialogs.</summary>
public record DocumentDialogsView(string ReturnUrl, DocumentTree Tree);

/// <summary>
/// Model of the editor tab strip. A document tab shows the file type; Icon replaces it (Compare,
/// Upload, Edit). Tabs with a DocumentId are closable and remembered for the session.
/// </summary>
public record EditorTabsView(string Title, string? FileType = null, int? DocumentId = null, string? Icon = null, string? Suffix = null)
{
    public bool Closable => DocumentId != null;
}
