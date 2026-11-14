using DocFlowHub.Core.Models.Documents.Browser;
using DocFlowHub.Core.Models.Workspace;

namespace DocFlowHub.Web.Ui;

/// <summary>Links of the project / folder pages (design 8b).</summary>
public static class WorkspaceLinks
{
    public static string Project(int projectId) => $"/Projects/Details/{projectId}";

    public static string Folder(int projectId, int folderId) => $"/Folders/Details?Id={folderId}&ProjectId={projectId}";

    public static string Place(int projectId, int? folderId) => folderId is int id ? Folder(projectId, id) : Project(projectId);
}

/// <summary>Model of the project tree tool window on the project / folder pages.</summary>
public record ProjectTreeView(DocumentTree Tree, int? ProjectId, int? FolderId, IReadOnlySet<int> Open, bool Movable);

/// <summary>Model of one folder in the project tree (recursive partial).</summary>
public record ProjectTreeFolderNode(DocumentTreeFolder Folder, int ProjectId, int Level, ProjectTreeView View);

/// <summary>Model of the project inspector (Projects list and project page).</summary>
public record ProjectInspectorView(ProjectInspector Project, bool IsDemo, bool OnProjectPage, string ReturnUrl);

/// <summary>Model of the subfolders + documents table of a project or folder.</summary>
public record FolderBrowserView(FolderView View, bool IsDemo);

/// <summary>Model of the folder form fields partial; Parents are the folders it may be placed in.</summary>
public record FolderFieldsView(
    string ProjectName, string? Name, string? Description, int? ParentFolderId,
    IReadOnlyList<ProjectFolderRow> Parents, string? NameError);

/// <summary>Model of the project form fields partial.</summary>
public record ProjectFieldsView(string? Name, string? Description, string? Color, string? NameError);

/// <summary>Project colours offered by the forms (decision S13-8): hex values on the design hues.</summary>
public static class ProjectPalette
{
    public static readonly (string Name, string Hex)[] Colors =
    {
        ("Green", "#22c55e"),
        ("Blue", "#3b82f6"),
        ("Red", "#ef4444"),
        ("Amber", "#f59e0b"),
        ("Pink", "#ec4899"),
        ("Teal", "#14b8a6"),
        ("Violet", "#8b5cf6"),
    };

    /// <summary>A hex colour such as "#22c55e", or null for anything else.</summary>
    public static string? Clean(string? value)
    {
        var v = value?.Trim();
        return v is { Length: 7 } && v[0] == '#' && v.Skip(1).All(Uri.IsHexDigit) ? v.ToLowerInvariant() : null;
    }
}

public static class TeamUi
{
    /// <summary>"FI" for "Finance", "BP" for "Board pack": the team badge.</summary>
    public static string Badge(string name)
    {
        var words = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var badge = words.Length >= 2 ? $"{words[0][0]}{words[1][0]}" : name.Length >= 2 ? name[..2] : name;
        return badge.ToUpperInvariant();
    }

    public static string RoleLabel(string role) => role switch
    {
        "owner" => "Owner",
        "admin" => "Admin",
        _ => "Member",
    };
}
