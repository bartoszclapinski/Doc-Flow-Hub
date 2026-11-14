using DocFlowHub.Core.Models;
using DocFlowHub.Core.Models.Common;
using DocFlowHub.Core.Models.Documents;
using DocFlowHub.Core.Models.Workspace;
using DocFlowHub.Core.Services.Interfaces;
using DocFlowHub.Infrastructure.Data;
using DocFlowHub.Infrastructure.Services.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DocFlowHub.Infrastructure.Services.Workspace;

/// <summary>
/// Projected, read-only queries for the Projects, project/folder and Teams screens (decision S13-1).
/// Counts are subqueries in the database; queries stay simple enough for SQL Server and SQLite.
/// </summary>
public class WorkspaceBrowserService : IWorkspaceBrowserService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<WorkspaceBrowserService> _logger;

    public WorkspaceBrowserService(ApplicationDbContext context, ILogger<WorkspaceBrowserService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>Owned, or shared with a team the user belongs to (same rule as DocumentService).</summary>
    private IQueryable<Document> Visible(string userId) =>
        _context.Documents.Where(d =>
            d.OwnerId == userId ||
            (d.TeamId != null && _context.TeamMembers.Any(m => m.UserId == userId && m.TeamId == d.TeamId)));

    public async Task<ServiceResult<List<ProjectRow>>> GetProjectsAsync(string userId, bool archived)
    {
        try
        {
            var docs = Visible(userId);
            var rows = await _context.Projects
                .Where(p => p.OwnerId == userId && p.IsActive == !archived)
                .Select(p => new ProjectRow
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    Color = p.Color,
                    IsActive = p.IsActive,
                    CreatedAt = p.CreatedAt,
                    UpdatedAt = p.UpdatedAt ?? p.CreatedAt,
                    FolderCount = _context.Folders.Count(f => f.ProjectId == p.Id),
                    DocumentCount = docs.Count(d => d.ProjectId == p.Id),
                    Bytes = docs.Where(d => d.ProjectId == p.Id).Sum(d => (long?)d.FileSize) ?? 0,
                })
                .ToListAsync();

            var lastChange = await docs
                .Where(d => d.ProjectId != null)
                .GroupBy(d => d.ProjectId!.Value)
                .Select(g => new { Id = g.Key, At = g.Max(d => d.UpdatedAt ?? d.CreatedAt) })
                .ToDictionaryAsync(x => x.Id, x => x.At);
            foreach (var row in rows)
            {
                if (lastChange.TryGetValue(row.Id, out var at) && at > row.UpdatedAt) row.UpdatedAt = at;
            }

            return ServiceResult<List<ProjectRow>>.Success(rows.OrderBy(r => r.Name, StringComparer.OrdinalIgnoreCase).ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list projects for user {UserId}", userId);
            return ServiceResult<List<ProjectRow>>.Failure("The projects could not be loaded.");
        }
    }

    public async Task<ServiceResult<ProjectInspector>> GetProjectAsync(string userId, int projectId)
    {
        try
        {
            var project = await ProjectAsync(userId, projectId);
            return project == null
                ? ServiceResult<ProjectInspector>.Failure("Project not found.")
                : ServiceResult<ProjectInspector>.Success(project);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load project {ProjectId} for user {UserId}", projectId, userId);
            return ServiceResult<ProjectInspector>.Failure("The project could not be loaded.");
        }
    }

    public async Task<ServiceResult<FolderView>> GetFolderViewAsync(string userId, int projectId, int? folderId)
    {
        try
        {
            var project = await ProjectAsync(userId, projectId);
            if (project == null)
            {
                return ServiceResult<FolderView>.Failure("Project not found.");
            }

            var view = new FolderView { Project = project };
            if (folderId != null)
            {
                var folder = await _context.Folders
                    .Where(f => f.Id == folderId && f.ProjectId == projectId)
                    .Select(f => new
                    {
                        f.Id, f.Name, f.Description, f.IsArchived, f.ParentFolderId, f.CreatedAt,
                        f.CreatedByUser.FirstName, f.CreatedByUser.LastName, f.CreatedByUser.Email,
                    })
                    .FirstOrDefaultAsync();
                if (folder == null)
                {
                    return ServiceResult<FolderView>.Failure("Folder not found.");
                }
                view.Folder = new FolderInfo
                {
                    Id = folder.Id,
                    Name = folder.Name,
                    Description = folder.Description,
                    IsArchived = folder.IsArchived,
                    ParentId = folder.ParentFolderId,
                    CreatedAt = folder.CreatedAt,
                    CreatorName = DisplayFormat.PersonName(folder.FirstName, folder.LastName, folder.Email),
                };

                var byId = project.Folders.ToDictionary(f => f.Id);
                for (int? id = folder.Id; id != null && byId.TryGetValue(id.Value, out var node); id = node.ParentId)
                {
                    view.Path.Insert(0, (node.Id, node.Name));
                }
            }

            var docs = Visible(userId);
            var subfolders = await _context.Folders
                .Where(f => f.ProjectId == projectId && f.ParentFolderId == folderId)
                .Select(f => new
                {
                    f.Id, f.Name, f.IsArchived,
                    Modified = f.UpdatedAt ?? f.CreatedAt,
                    Children = _context.Folders.Count(c => c.ParentFolderId == f.Id),
                    Docs = docs.Count(d => d.FolderId == f.Id),
                    LastDoc = docs.Where(d => d.FolderId == f.Id).Max(d => (DateTime?)(d.UpdatedAt ?? d.CreatedAt)),
                    f.CreatedByUser.FirstName, f.CreatedByUser.LastName, f.CreatedByUser.Email,
                })
                .ToListAsync();
            view.Subfolders = subfolders
                .Select(f => new FolderEntry
                {
                    Id = f.Id,
                    Name = f.Name,
                    IsArchived = f.IsArchived,
                    ItemCount = f.Children + f.Docs,
                    ModifiedAt = f.LastDoc is DateTime last && last > f.Modified ? last : f.Modified,
                    CreatorName = DisplayFormat.PersonName(f.FirstName, f.LastName, f.Email),
                })
                .OrderBy(f => f.IsArchived)
                .ThenBy(f => f.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var documents = await docs
                .Where(d => d.ProjectId == projectId && d.FolderId == folderId)
                .Select(d => new
                {
                    d.Id, d.Title, d.FileType, d.FileSize,
                    Modified = d.UpdatedAt ?? d.CreatedAt,
                    Version = d.Versions.Max(v => (int?)v.VersionNumber),
                    LastAuthor = d.Versions.OrderByDescending(v => v.VersionNumber).Select(v => v.UserId).FirstOrDefault(),
                    HasSummary = _context.DocumentSummaries.Any(s => s.DocumentId == d.Id),
                })
                .ToListAsync();
            var authorIds = documents.Select(d => d.LastAuthor).Where(id => id != null).Distinct().ToList();
            var authors = await _context.Users
                .Where(u => authorIds.Contains(u.Id))
                .Select(u => new { u.Id, u.FirstName, u.LastName, u.Email })
                .ToDictionaryAsync(u => u.Id, u => DisplayFormat.PersonName(u.FirstName, u.LastName, u.Email));
            view.Documents = documents
                .Select(d => new FolderDocument
                {
                    Id = d.Id,
                    Title = d.Title,
                    FileType = d.FileType,
                    FileSize = d.FileSize,
                    Version = d.Version,
                    ModifiedAt = d.Modified,
                    LastChangeBy = d.LastAuthor != null ? authors.GetValueOrDefault(d.LastAuthor) ?? "Unknown" : "Unknown",
                    HasSummary = d.HasSummary,
                })
                .OrderBy(d => d.Title, StringComparer.OrdinalIgnoreCase)
                .ToList();

            // Same rules as FolderService / ProjectService: only an empty folder or project can be deleted.
            view.IsEmpty = folderId != null
                ? view.Subfolders.Count == 0 && !await _context.Documents.AnyAsync(d => d.FolderId == folderId)
                : project.Folders.Count == 0 && !await _context.Documents.AnyAsync(d => d.ProjectId == projectId);

            return ServiceResult<FolderView>.Success(view);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load folder {FolderId} of project {ProjectId} for user {UserId}", folderId, projectId, userId);
            return ServiceResult<FolderView>.Failure("The folder could not be loaded.");
        }
    }

    public async Task<ServiceResult<List<TeamRow>>> GetTeamsAsync(string userId)
    {
        try
        {
            var rows = await Teams(userId)
                .Select(t => new
                {
                    t.Id, t.Name, t.Description, t.CreatedAt, t.OwnerId,
                    t.Owner.FirstName, t.Owner.LastName, t.Owner.Email,
                    MyRole = t.Members.Where(m => m.UserId == userId).Select(m => (TeamRole?)m.Role).FirstOrDefault(),
                    Members = t.Members.Count,
                    Docs = _context.Documents.Count(d => d.TeamId == t.Id),
                })
                .ToListAsync();

            return ServiceResult<List<TeamRow>>.Success(rows
                .Select(t => new TeamRow
                {
                    Id = t.Id,
                    Name = t.Name,
                    Description = t.Description,
                    CreatedAt = t.CreatedAt,
                    Role = RoleName(t.OwnerId == userId, t.MyRole),
                    MemberCount = t.Members,
                    DocumentCount = t.Docs,
                    OwnerName = DisplayFormat.PersonName(t.FirstName, t.LastName, t.Email),
                })
                .OrderBy(t => t.Name, StringComparer.OrdinalIgnoreCase)
                .ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list teams for user {UserId}", userId);
            return ServiceResult<List<TeamRow>>.Failure("The teams could not be loaded.");
        }
    }

    public async Task<ServiceResult<TeamView>> GetTeamAsync(string userId, int teamId)
    {
        try
        {
            var team = await Teams(userId)
                .Where(t => t.Id == teamId)
                .Select(t => new
                {
                    t.Id, t.Name, t.Description, t.CreatedAt, t.OwnerId,
                    t.Owner.FirstName, t.Owner.LastName, t.Owner.Email,
                    Docs = _context.Documents.Count(d => d.TeamId == t.Id),
                })
                .FirstOrDefaultAsync();
            if (team == null)
            {
                return ServiceResult<TeamView>.Failure("Team not found.");
            }

            var members = await _context.TeamMembers
                .Where(m => m.TeamId == teamId)
                .Select(m => new
                {
                    m.UserId, m.Role, m.JoinedAt,
                    m.User.FirstName, m.User.LastName, m.User.Email, m.User.LastLoginAt,
                })
                .ToListAsync();
            var myRole = members.FirstOrDefault(m => m.UserId == userId)?.Role;

            var view = new TeamView
            {
                Id = team.Id,
                Name = team.Name,
                Description = team.Description,
                CreatedAt = team.CreatedAt,
                Role = RoleName(team.OwnerId == userId, myRole),
                IsAdmin = myRole == TeamRole.Admin,
                MemberCount = members.Count,
                DocumentCount = team.Docs,
                OwnerName = DisplayFormat.PersonName(team.FirstName, team.LastName, team.Email),
                Members = members
                    .Select(m => new TeamMemberRow
                    {
                        UserId = m.UserId,
                        Name = DisplayFormat.PersonName(m.FirstName, m.LastName, m.Email),
                        Email = m.Email ?? string.Empty,
                        IsOwner = m.UserId == team.OwnerId,
                        IsAdmin = m.Role == TeamRole.Admin,
                        IsMe = m.UserId == userId,
                        JoinedAt = m.JoinedAt,
                        LastLoginAt = m.LastLoginAt,
                    })
                    .OrderByDescending(m => m.IsOwner)
                    .ThenByDescending(m => m.IsAdmin)
                    .ThenBy(m => m.Name, StringComparer.OrdinalIgnoreCase)
                    .ToList(),
            };
            return ServiceResult<TeamView>.Success(view);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load team {TeamId} for user {UserId}", teamId, userId);
            return ServiceResult<TeamView>.Failure("The team could not be loaded.");
        }
    }

    private IQueryable<Team> Teams(string userId) =>
        _context.Teams.Where(t => t.OwnerId == userId || t.Members.Any(m => m.UserId == userId));

    private static string RoleName(bool isOwner, TeamRole? role) =>
        isOwner ? "owner" : role == TeamRole.Admin ? "admin" : "member";

    /// <summary>The user's project (any state) with its folders in tree order, or null.</summary>
    private async Task<ProjectInspector?> ProjectAsync(string userId, int projectId)
    {
        var docs = Visible(userId);
        var project = await _context.Projects
            .Where(p => p.Id == projectId && p.OwnerId == userId)
            .Select(p => new ProjectInspector
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Color = p.Color,
                IsActive = p.IsActive,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt ?? p.CreatedAt,
                OwnerName = (p.Owner.FirstName + " " + p.Owner.LastName).Trim(),
                DocumentCount = docs.Count(d => d.ProjectId == p.Id),
                Bytes = docs.Where(d => d.ProjectId == p.Id).Sum(d => (long?)d.FileSize) ?? 0,
                SummarizedCount = docs.Count(d => d.ProjectId == p.Id && _context.DocumentSummaries.Any(s => s.DocumentId == d.Id)),
            })
            .FirstOrDefaultAsync();
        if (project == null)
        {
            return null;
        }
        if (string.IsNullOrEmpty(project.OwnerName))
        {
            project.OwnerName = await _context.Users.Where(u => u.Id == userId).Select(u => u.Email).FirstOrDefaultAsync() ?? "Unknown";
        }

        var lastDoc = await docs.Where(d => d.ProjectId == projectId).MaxAsync(d => (DateTime?)(d.UpdatedAt ?? d.CreatedAt));
        if (lastDoc > project.UpdatedAt) project.UpdatedAt = lastDoc.Value;

        var folders = await _context.Folders
            .Where(f => f.ProjectId == projectId)
            .Select(f => new { f.Id, f.Name, f.ParentFolderId, f.IsArchived, Docs = docs.Count(d => d.FolderId == f.Id) })
            .ToListAsync();
        project.FolderCount = folders.Count;

        // Parents before children, siblings by name.
        var children = folders.ToLookup(f => f.ParentFolderId);
        var ids = folders.Select(f => f.Id).ToHashSet();
        void Add(int? parentId, int level)
        {
            foreach (var f in children[parentId].OrderBy(f => f.Name, StringComparer.OrdinalIgnoreCase))
            {
                project.Folders.Add(new ProjectFolderRow
                {
                    Id = f.Id, Name = f.Name, ParentId = f.ParentFolderId, Level = level, IsArchived = f.IsArchived, DocumentCount = f.Docs,
                });
                Add(f.Id, level + 1);
            }
        }
        Add(null, 0);
        // A folder whose parent is missing would be skipped above; list it at the root.
        foreach (var orphan in folders.Where(f => f.ParentFolderId is int p && !ids.Contains(p)))
        {
            project.Folders.Add(new ProjectFolderRow { Id = orphan.Id, Name = orphan.Name, ParentId = null, IsArchived = orphan.IsArchived, DocumentCount = orphan.Docs });
        }
        return project;
    }
}
