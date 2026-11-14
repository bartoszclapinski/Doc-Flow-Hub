namespace DocFlowHub.Core.Models.Documents.Browser;

/// <summary>Who owns the listed documents, relative to the signed-in user.</summary>
public enum DocumentOwnerFilter
{
    Anyone,
    Me,
    Others,
}

public enum DocumentSort
{
    Modified,
    Name,
    Size,
}

/// <summary>One page of the Documents list. Filters combine with AND; null means "any".</summary>
public class DocumentListQuery
{
    public string? Search { get; set; }

    /// <summary>File type without the dot, case-insensitive ("pdf").</summary>
    public string? FileType { get; set; }

    public DocumentOwnerFilter Owner { get; set; } = DocumentOwnerFilter.Anyone;
    public int? CategoryId { get; set; }
    public int? ProjectId { get; set; }
    public int? FolderId { get; set; }
    public int? TeamId { get; set; }
    public DocumentSort Sort { get; set; } = DocumentSort.Modified;
    public bool Descending { get; set; } = true;
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
}

public class DocumentListPage
{
    public List<DocumentListRow> Items { get; set; } = new();
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }

    public int PageCount => PageSize <= 0 ? 0 : (Total + PageSize - 1) / PageSize;
    public int From => Total == 0 ? 0 : (Page - 1) * PageSize + 1;
    public int To => Math.Min(Page * PageSize, Total);
}

public class DocumentListRow
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public int? Version { get; set; }
    public DateTime ModifiedAt { get; set; }
    public string OwnerName { get; set; } = string.Empty;
    public bool IsOwner { get; set; }
    public string? TeamName { get; set; }
    public bool HasSummary { get; set; }

    /// <summary>"Project / folder / subfolder", or null when the document is in no project.</summary>
    public string? Location { get; set; }
}

/// <summary>Everything the left tool window and the toolbar filters need.</summary>
public class DocumentTree
{
    /// <summary>All documents the user can see.</summary>
    public int TotalDocuments { get; set; }
    public long StorageBytes { get; set; }
    public List<DocumentTreeProject> Projects { get; set; } = new();
    public List<DocumentTreeTeam> Teams { get; set; } = new();

    /// <summary>File types present in the visible documents ("pdf"), most common first.</summary>
    public List<DocumentTypeCount> FileTypes { get; set; } = new();

    public DocumentTreeProject? FindProject(int id) => Projects.FirstOrDefault(p => p.Id == id);

    public (DocumentTreeProject Project, DocumentTreeFolder Folder)? FindFolder(int id)
    {
        foreach (var project in Projects)
        {
            var folder = project.AllFolders().FirstOrDefault(f => f.Id == id);
            if (folder != null) return (project, folder);
        }
        return null;
    }
}

public class DocumentTreeProject
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    /// <summary>Stored colour (a hex value such as "#3b82f6"), if any.</summary>
    public string? Color { get; set; }

    /// <summary>Visible documents anywhere in the project.</summary>
    public int DocumentCount { get; set; }

    /// <summary>Root folders; each holds its subfolders.</summary>
    public List<DocumentTreeFolder> Folders { get; set; } = new();

    public IEnumerable<DocumentTreeFolder> AllFolders() => Folders.SelectMany(f => f.SelfAndDescendants());
}

public class DocumentTreeFolder
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int? ParentId { get; set; }

    /// <summary>Visible documents directly in this folder.</summary>
    public int DocumentCount { get; set; }
    public List<DocumentTreeFolder> Children { get; set; } = new();

    public IEnumerable<DocumentTreeFolder> SelfAndDescendants() =>
        new[] { this }.Concat(Children.SelectMany(c => c.SelfAndDescendants()));
}

public class DocumentTreeTeam
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int MemberCount { get; set; }
}

public class DocumentTypeCount
{
    public string FileType { get; set; } = string.Empty;
    public int Count { get; set; }
}

/// <summary>The Documents inspector: one document, its AI summary and its latest versions.</summary>
public class DocumentInspector
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string? Location { get; set; }
    public int? ProjectId { get; set; }
    public int? FolderId { get; set; }
    public string OwnerName { get; set; } = string.Empty;
    public bool IsOwner { get; set; }
    public string? TeamName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ModifiedAt { get; set; }
    public int? Version { get; set; }

    /// <summary>First 7 hex characters of the current version's file hash.</summary>
    public string? Hash { get; set; }

    public int VersionCount { get; set; }

    /// <summary>Size of all versions together.</summary>
    public long VersionBytes { get; set; }

    public List<string> Categories { get; set; } = new();
    public DocumentInspectorSummary? Summary { get; set; }

    /// <summary>Newest first, at most four.</summary>
    public List<DocumentInspectorVersion> RecentVersions { get; set; } = new();
}

public class DocumentInspectorSummary
{
    public string Text { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public double? Confidence { get; set; }
    public DateTime GeneratedAt { get; set; }
}

public class DocumentInspectorVersion
{
    public int Number { get; set; }
    public string? Message { get; set; }
    public DateTime CreatedAt { get; set; }
    public string Author { get; set; } = string.Empty;
}
