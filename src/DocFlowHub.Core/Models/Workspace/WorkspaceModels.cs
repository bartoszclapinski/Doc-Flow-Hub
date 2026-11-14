namespace DocFlowHub.Core.Models.Workspace;

/// <summary>One row of the Projects table (design 8a).</summary>
public class ProjectRow
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Color { get; set; }
    public bool IsActive { get; set; }
    public int FolderCount { get; set; }
    public int DocumentCount { get; set; }
    public long Bytes { get; set; }

    /// <summary>The project's own change or its newest document change, whichever is later.</summary>
    public DateTime UpdatedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>The Projects inspector: a project, its coverage by AI summaries and its folders.</summary>
public class ProjectInspector : ProjectRow
{
    public string OwnerName { get; set; } = string.Empty;
    public int SummarizedCount { get; set; }

    /// <summary>Every folder in tree order (parents before children), archived ones included.</summary>
    public List<ProjectFolderRow> Folders { get; set; } = new();
}

public class ProjectFolderRow
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int? ParentId { get; set; }
    public int Level { get; set; }
    public bool IsArchived { get; set; }
    public int DocumentCount { get; set; }
}

/// <summary>The project / folder browser (design 8b): the project root when Folder is null.</summary>
public class FolderView
{
    public ProjectInspector Project { get; set; } = new();
    public FolderInfo? Folder { get; set; }

    /// <summary>Folders from the root down to the current one (empty at the project root).</summary>
    public List<(int Id, string Name)> Path { get; set; } = new();

    public List<FolderEntry> Subfolders { get; set; } = new();
    public List<FolderDocument> Documents { get; set; } = new();

    /// <summary>Size of the documents directly here.</summary>
    public long Bytes => Documents.Sum(d => d.FileSize);

    /// <summary>A folder can be deleted only when it has no subfolders and no documents (archived ones count).</summary>
    public bool IsEmpty { get; set; }
}

public class FolderInfo
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsArchived { get; set; }
    public int? ParentId { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatorName { get; set; } = string.Empty;
}

public class FolderEntry
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsArchived { get; set; }

    /// <summary>Subfolders plus documents directly inside.</summary>
    public int ItemCount { get; set; }
    public DateTime ModifiedAt { get; set; }
    public string CreatorName { get; set; } = string.Empty;
}

public class FolderDocument
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public int? Version { get; set; }
    public DateTime ModifiedAt { get; set; }

    /// <summary>Who uploaded the newest version.</summary>
    public string LastChangeBy { get; set; } = string.Empty;
    public bool HasSummary { get; set; }
}

/// <summary>One row of the Teams table.</summary>
public class TeamRow
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    /// <summary>"owner", "admin" or "member".</summary>
    public string Role { get; set; } = "member";
    public int MemberCount { get; set; }
    public int DocumentCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public string OwnerName { get; set; } = string.Empty;
}

/// <summary>The team page (design 3d).</summary>
public class TeamView : TeamRow
{
    public bool IsOwner => Role == "owner";

    /// <summary>Admins (the owner is one) add and remove members and change roles.</summary>
    public bool IsAdmin { get; set; }
    public List<TeamMemberRow> Members { get; set; } = new();
}

public class TeamMemberRow
{
    public string UserId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsOwner { get; set; }
    public bool IsAdmin { get; set; }
    public bool IsMe { get; set; }
    public DateTime JoinedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
}
