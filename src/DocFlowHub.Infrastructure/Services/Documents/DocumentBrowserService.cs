using DocFlowHub.Core.Models.Common;
using DocFlowHub.Core.Models.Documents;
using DocFlowHub.Core.Models.Documents.Browser;
using DocFlowHub.Core.Services.Interfaces;
using DocFlowHub.Infrastructure.Data;
using DocFlowHub.Infrastructure.Services.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DocFlowHub.Infrastructure.Services.Documents;

/// <summary>
/// Projected, read-only queries for the Documents screen: only the columns a screen shows are
/// selected, and counts are GROUP BYs in the database. Queries stay simple enough for both
/// SQL Server and the SQLite demo. Deleted documents are hidden by the global query filter.
/// </summary>
public class DocumentBrowserService : IDocumentBrowserService
{
    private const int MaxPageSize = 100;
    private const int InspectorVersions = 4;

    private readonly ApplicationDbContext _context;
    private readonly ILogger<DocumentBrowserService> _logger;

    public DocumentBrowserService(ApplicationDbContext context, ILogger<DocumentBrowserService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>Owned, or shared with a team the user belongs to (same rule as DocumentService).</summary>
    private IQueryable<Document> Visible(string userId) =>
        _context.Documents.Where(d =>
            d.OwnerId == userId ||
            (d.TeamId != null && _context.TeamMembers.Any(m => m.UserId == userId && m.TeamId == d.TeamId)));

    public async Task<ServiceResult<DocumentTree>> GetTreeAsync(string userId)
    {
        try
        {
            var docs = Visible(userId);
            var tree = new DocumentTree
            {
                TotalDocuments = await docs.CountAsync(),
                StorageBytes = await docs.SumAsync(d => (long?)d.FileSize) ?? 0,
            };

            // Projects are owner-only in the app.
            var projects = await _context.Projects
                .Where(p => p.OwnerId == userId && p.IsActive)
                .Select(p => new { p.Id, p.Name, p.Color })
                .ToListAsync();
            var projectCounts = await docs
                .Where(d => d.ProjectId != null)
                .GroupBy(d => d.ProjectId!.Value)
                .Select(g => new { Id = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Id, x => x.Count);

            var folders = await _context.Folders
                .Where(f => !f.IsArchived && f.Project.OwnerId == userId && f.Project.IsActive)
                .Select(f => new { f.Id, f.Name, f.ParentFolderId, f.ProjectId })
                .ToListAsync();
            var folderCounts = await docs
                .Where(d => d.FolderId != null)
                .GroupBy(d => d.FolderId!.Value)
                .Select(g => new { Id = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Id, x => x.Count);

            var nodes = folders.ToDictionary(f => f.Id, f => new DocumentTreeFolder
            {
                Id = f.Id,
                Name = f.Name,
                ParentId = f.ParentFolderId,
                DocumentCount = folderCounts.GetValueOrDefault(f.Id),
            });
            var roots = new Dictionary<int, List<DocumentTreeFolder>>();
            foreach (var f in folders.OrderBy(f => f.Name, StringComparer.OrdinalIgnoreCase))
            {
                var node = nodes[f.Id];
                // A folder whose parent is archived (hidden) is shown at the root so it stays reachable.
                if (f.ParentFolderId is int parentId && nodes.TryGetValue(parentId, out var parent))
                {
                    parent.Children.Add(node);
                }
                else
                {
                    if (!roots.TryGetValue(f.ProjectId, out var list)) roots[f.ProjectId] = list = new();
                    list.Add(node);
                }
            }

            tree.Projects = projects
                .OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
                .Select(p => new DocumentTreeProject
                {
                    Id = p.Id,
                    Name = p.Name,
                    Color = p.Color,
                    DocumentCount = projectCounts.GetValueOrDefault(p.Id),
                    Folders = roots.GetValueOrDefault(p.Id) ?? new(),
                })
                .ToList();

            tree.Teams = await _context.Teams
                .Where(t => t.OwnerId == userId || t.Members.Any(m => m.UserId == userId))
                .OrderBy(t => t.Name)
                .Select(t => new DocumentTreeTeam { Id = t.Id, Name = t.Name, MemberCount = t.Members.Count })
                .ToListAsync();

            var types = await docs
                .GroupBy(d => d.FileType)
                .Select(g => new { FileType = g.Key, Count = g.Count() })
                .ToListAsync();
            // ".PDF" and ".pdf" are the same type.
            tree.FileTypes = types
                .GroupBy(t => NormalizeType(t.FileType))
                .Where(g => g.Key.Length > 0)
                .Select(g => new DocumentTypeCount { FileType = g.Key, Count = g.Sum(x => x.Count) })
                .OrderByDescending(t => t.Count)
                .ThenBy(t => t.FileType)
                .ToList();

            return ServiceResult<DocumentTree>.Success(tree);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load the document tree for user {UserId}", userId);
            return ServiceResult<DocumentTree>.Failure("The project tree could not be loaded.");
        }
    }

    public async Task<ServiceResult<DocumentListPage>> GetDocumentsAsync(string userId, DocumentListQuery query)
    {
        try
        {
            var docs = Filter(Visible(userId), userId, query);
            var pageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);
            var total = await docs.CountAsync();
            var pageCount = Math.Max(1, (total + pageSize - 1) / pageSize);
            var page = Math.Clamp(query.Page, 1, pageCount);

            var rows = await Sort(docs, query)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(d => new
                {
                    d.Id,
                    d.Title,
                    d.FileType,
                    d.FileSize,
                    Modified = d.UpdatedAt ?? d.CreatedAt,
                    Version = d.Versions.Max(v => (int?)v.VersionNumber),
                    d.OwnerId,
                    d.Owner.FirstName,
                    d.Owner.LastName,
                    d.Owner.Email,
                    TeamName = d.Team != null ? d.Team.Name : null,
                    ProjectName = d.Project != null ? d.Project.Name : null,
                    FolderPath = d.Folder != null ? d.Folder.Path : null,
                    HasSummary = _context.DocumentSummaries.Any(s => s.DocumentId == d.Id),
                })
                .ToListAsync();

            return ServiceResult<DocumentListPage>.Success(new DocumentListPage
            {
                Total = total,
                Page = page,
                PageSize = pageSize,
                Items = rows.Select(r => new DocumentListRow
                {
                    Id = r.Id,
                    Title = r.Title,
                    FileType = r.FileType,
                    FileSize = r.FileSize,
                    ModifiedAt = r.Modified,
                    Version = r.Version,
                    OwnerName = DisplayFormat.PersonName(r.FirstName, r.LastName, r.Email),
                    IsOwner = r.OwnerId == userId,
                    TeamName = r.TeamName,
                    HasSummary = r.HasSummary,
                    Location = DisplayFormat.Location(r.ProjectName, r.FolderPath),
                }).ToList(),
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list documents for user {UserId}", userId);
            return ServiceResult<DocumentListPage>.Failure("The documents could not be loaded.");
        }
    }

    public async Task<ServiceResult<DocumentInspector>> GetInspectorAsync(string userId, int documentId)
    {
        try
        {
            var doc = await Visible(userId)
                .Where(d => d.Id == documentId)
                .Select(d => new
                {
                    d.Id,
                    d.Title,
                    d.FileType,
                    d.FileSize,
                    d.CreatedAt,
                    Modified = d.UpdatedAt ?? d.CreatedAt,
                    d.ProjectId,
                    d.FolderId,
                    d.OwnerId,
                    d.Owner.FirstName,
                    d.Owner.LastName,
                    d.Owner.Email,
                    TeamName = d.Team != null ? d.Team.Name : null,
                    ProjectName = d.Project != null ? d.Project.Name : null,
                    FolderPath = d.Folder != null ? d.Folder.Path : null,
                    VersionCount = d.Versions.Count,
                    VersionBytes = d.Versions.Sum(v => (long?)v.FileSize),
                    Categories = d.Categories.OrderBy(c => c.Name).Select(c => c.Name).ToList(),
                })
                .FirstOrDefaultAsync();
            if (doc == null)
            {
                return ServiceResult<DocumentInspector>.Failure("Document not found.");
            }

            var versions = await _context.DocumentVersions
                .Where(v => v.DocumentId == documentId)
                .OrderByDescending(v => v.VersionNumber)
                .Take(InspectorVersions)
                .Select(v => new { v.VersionNumber, v.ChangeSummary, v.CreatedAt, v.UserId, v.FileHash })
                .ToListAsync();
            var authorIds = versions.Select(v => v.UserId).Distinct().ToList();
            var authors = await _context.Users
                .Where(u => authorIds.Contains(u.Id))
                .Select(u => new { u.Id, u.FirstName, u.LastName, u.Email })
                .ToDictionaryAsync(u => u.Id, u => DisplayFormat.PersonName(u.FirstName, u.LastName, u.Email));

            var summary = await _context.DocumentSummaries
                .Where(s => s.DocumentId == documentId)
                .OrderByDescending(s => s.GeneratedAt)
                .Select(s => new DocumentInspectorSummary
                {
                    Text = s.Summary,
                    Model = s.AIModel,
                    Confidence = s.ConfidenceScore,
                    GeneratedAt = s.GeneratedAt,
                })
                .FirstOrDefaultAsync();

            return ServiceResult<DocumentInspector>.Success(new DocumentInspector
            {
                Id = doc.Id,
                Title = doc.Title,
                FileType = doc.FileType,
                FileSize = doc.FileSize,
                CreatedAt = doc.CreatedAt,
                ModifiedAt = doc.Modified,
                ProjectId = doc.ProjectId,
                FolderId = doc.FolderId,
                Location = DisplayFormat.Location(doc.ProjectName, doc.FolderPath),
                OwnerName = DisplayFormat.PersonName(doc.FirstName, doc.LastName, doc.Email),
                IsOwner = doc.OwnerId == userId,
                TeamName = doc.TeamName,
                Version = versions.Count > 0 ? versions[0].VersionNumber : null,
                Hash = versions.Count > 0 ? DisplayFormat.ShortHash(versions[0].FileHash) : null,
                VersionCount = doc.VersionCount,
                VersionBytes = doc.VersionBytes ?? 0,
                Categories = doc.Categories,
                Summary = string.IsNullOrWhiteSpace(summary?.Text) ? null : summary,
                RecentVersions = versions.Select(v => new DocumentInspectorVersion
                {
                    Number = v.VersionNumber,
                    Message = string.IsNullOrWhiteSpace(v.ChangeSummary) ? null : v.ChangeSummary.Trim(),
                    CreatedAt = v.CreatedAt,
                    Author = authors.GetValueOrDefault(v.UserId) ?? "Unknown",
                }).ToList(),
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load document {DocumentId} for user {UserId}", documentId, userId);
            return ServiceResult<DocumentInspector>.Failure("The document could not be loaded.");
        }
    }

    public async Task<ServiceResult<DocumentDetails>> GetDetailsAsync(string userId, int documentId)
    {
        try
        {
            var doc = await Visible(userId)
                .Where(d => d.Id == documentId)
                .Select(d => new
                {
                    d.Id,
                    d.Title,
                    d.Description,
                    d.FileType,
                    d.FileSize,
                    d.CreatedAt,
                    Modified = d.UpdatedAt ?? d.CreatedAt,
                    d.ProjectId,
                    d.FolderId,
                    d.OwnerId,
                    d.Owner.FirstName,
                    d.Owner.LastName,
                    d.Owner.Email,
                    d.TeamId,
                    TeamName = d.Team != null ? d.Team.Name : null,
                    ProjectName = d.Project != null ? d.Project.Name : null,
                    FolderPath = d.Folder != null ? d.Folder.Path : null,
                    Categories = d.Categories.OrderBy(c => c.Name).Select(c => c.Name).ToList(),
                })
                .FirstOrDefaultAsync();
            if (doc == null)
            {
                return ServiceResult<DocumentDetails>.Failure("Document not found.");
            }

            var summary = await _context.DocumentSummaries
                .Where(s => s.DocumentId == documentId)
                .OrderByDescending(s => s.GeneratedAt)
                .Select(s => new { s.Summary, s.KeyPoints, s.AIModel, s.ConfidenceScore, s.GeneratedAt })
                .FirstOrDefaultAsync();

            var versions = await VersionsAsync(documentId);
            var byId = versions.ToDictionary(v => v.Id, v => v.Number);
            var pairs = await _context.VersionComparisons
                .Where(c => c.DocumentId == documentId)
                .Select(c => new { c.FromVersionId, c.ToVersionId })
                .ToListAsync();

            return ServiceResult<DocumentDetails>.Success(new DocumentDetails
            {
                Id = doc.Id,
                Title = doc.Title,
                Description = doc.Description ?? string.Empty,
                FileType = doc.FileType,
                FileSize = doc.FileSize,
                CreatedAt = doc.CreatedAt,
                ModifiedAt = doc.Modified,
                ProjectId = doc.ProjectId,
                FolderId = doc.FolderId,
                Location = DisplayFormat.Location(doc.ProjectName, doc.FolderPath),
                OwnerName = DisplayFormat.PersonName(doc.FirstName, doc.LastName, doc.Email),
                IsOwner = doc.OwnerId == userId,
                TeamId = doc.TeamId,
                TeamName = doc.TeamName,
                Categories = doc.Categories,
                Summary = summary == null || string.IsNullOrWhiteSpace(summary.Summary) ? null : new DocumentDetailsSummary
                {
                    Text = summary.Summary,
                    KeyPoints = Lines(summary.KeyPoints),
                    Model = summary.AIModel,
                    Confidence = summary.ConfidenceScore,
                    GeneratedAt = summary.GeneratedAt,
                },
                Versions = versions,
                Comparisons = pairs
                    .Where(p => byId.ContainsKey(p.FromVersionId) && byId.ContainsKey(p.ToVersionId))
                    .Select(p => (Math.Min(byId[p.FromVersionId], byId[p.ToVersionId]), Math.Max(byId[p.FromVersionId], byId[p.ToVersionId])))
                    .Distinct()
                    .OrderByDescending(p => p.Item2)
                    .ThenByDescending(p => p.Item1)
                    .ToList(),
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load details of document {DocumentId} for user {UserId}", documentId, userId);
            return ServiceResult<DocumentDetails>.Failure("The document could not be loaded.");
        }
    }

    public async Task<ServiceResult<DocumentComparisonView>> GetComparisonAsync(string userId, int documentId, int? fromVersion, int? toVersion)
    {
        try
        {
            var doc = await Visible(userId)
                .Where(d => d.Id == documentId)
                .Select(d => new
                {
                    d.Id,
                    d.Title,
                    d.FileType,
                    ProjectName = d.Project != null ? d.Project.Name : null,
                    FolderPath = d.Folder != null ? d.Folder.Path : null,
                })
                .FirstOrDefaultAsync();
            if (doc == null)
            {
                return ServiceResult<DocumentComparisonView>.Failure("Document not found.");
            }

            var versions = await VersionsAsync(documentId);
            if (versions.Count < 2)
            {
                return ServiceResult<DocumentComparisonView>.Failure("This document has only one version, so there is nothing to compare.");
            }

            // Newest first. "to" defaults to the newest, "from" to the version before "to".
            var to = versions.FirstOrDefault(v => v.Number == toVersion) ?? versions[0];
            var from = versions.FirstOrDefault(v => v.Number == fromVersion && v.Number != to.Number)
                       ?? versions.FirstOrDefault(v => v.Number < to.Number)
                       ?? versions.First(v => v.Number != to.Number);
            if (from.Number > to.Number)
            {
                (from, to) = (to, from);
            }

            var comparison = await _context.VersionComparisons
                .Where(c => (c.FromVersionId == from.Id && c.ToVersionId == to.Id) ||
                            (c.FromVersionId == to.Id && c.ToVersionId == from.Id))
                .OrderByDescending(c => c.GeneratedAt)
                .Select(c => new
                {
                    c.ChangeSummary, c.DetailedChanges, c.ChangeType, c.Significance, c.AIModel,
                    c.ConfidenceScore, c.TokensUsed, c.GeneratedAt,
                })
                .FirstOrDefaultAsync();

            return ServiceResult<DocumentComparisonView>.Success(new DocumentComparisonView
            {
                DocumentId = doc.Id,
                Title = doc.Title,
                FileType = doc.FileType,
                Location = DisplayFormat.Location(doc.ProjectName, doc.FolderPath),
                Versions = versions,
                From = from,
                To = to,
                Comparison = comparison == null ? null : new DocumentComparisonResult
                {
                    Summary = comparison.ChangeSummary,
                    Changes = Lines(comparison.DetailedChanges),
                    ChangeType = comparison.ChangeType,
                    Significance = comparison.Significance,
                    Model = comparison.AIModel,
                    Confidence = comparison.ConfidenceScore,
                    TokensUsed = comparison.TokensUsed,
                    GeneratedAt = comparison.GeneratedAt,
                },
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load the comparison of document {DocumentId} for user {UserId}", documentId, userId);
            return ServiceResult<DocumentComparisonView>.Failure("The comparison could not be loaded.");
        }
    }

    /// <summary>All versions of a document, newest first, with author names.</summary>
    private async Task<List<DocumentDetailsVersion>> VersionsAsync(int documentId)
    {
        var rows = await _context.DocumentVersions
            .Where(v => v.DocumentId == documentId)
            .OrderByDescending(v => v.VersionNumber)
            .Select(v => new { v.Id, v.VersionNumber, v.ChangeSummary, v.CreatedAt, v.UserId, v.FileSize, v.FileType, v.FileHash })
            .ToListAsync();
        var authorIds = rows.Select(v => v.UserId).Distinct().ToList();
        var authors = await _context.Users
            .Where(u => authorIds.Contains(u.Id))
            .Select(u => new { u.Id, u.FirstName, u.LastName, u.Email })
            .ToDictionaryAsync(u => u.Id, u => DisplayFormat.PersonName(u.FirstName, u.LastName, u.Email));

        return rows.Select(v => new DocumentDetailsVersion
        {
            Id = v.Id,
            Number = v.VersionNumber,
            Message = string.IsNullOrWhiteSpace(v.ChangeSummary) ? null : v.ChangeSummary.Trim(),
            CreatedAt = v.CreatedAt,
            Author = authors.GetValueOrDefault(v.UserId) ?? "Unknown",
            FileSize = v.FileSize,
            FileType = v.FileType,
            Hash = DisplayFormat.ShortHash(v.FileHash),
        }).ToList();
    }

    /// <summary>Splits an AI list ("• a\n- b") into items without bullets.</summary>
    private static List<string> Lines(string? text) =>
        (text ?? string.Empty)
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(line => line.TrimStart('•', '-', '*', '–', '—', ' ').Trim())
            .Where(line => line.Length > 0)
            .ToList();

    private static IQueryable<Document> Filter(IQueryable<Document> docs, string userId, DocumentListQuery query)
    {
        var search = query.Search?.Trim();
        if (!string.IsNullOrEmpty(search))
        {
            // Case-insensitive on SQL Server (collation) and SQLite (SqliteCaseInsensitiveSearchInterceptor).
            docs = docs.Where(d => d.Title.Contains(search) || d.Description.Contains(search));
        }

        var type = NormalizeType(query.FileType);
        if (type.Length > 0)
        {
            var dotted = "." + type;
            docs = docs.Where(d => d.FileType.ToLower() == dotted || d.FileType.ToLower() == type);
        }

        docs = query.Owner switch
        {
            DocumentOwnerFilter.Me => docs.Where(d => d.OwnerId == userId),
            DocumentOwnerFilter.Others => docs.Where(d => d.OwnerId != userId),
            _ => docs,
        };

        if (query.CategoryId is int categoryId) docs = docs.Where(d => d.Categories.Any(c => c.Id == categoryId));
        if (query.ProjectId is int projectId) docs = docs.Where(d => d.ProjectId == projectId);
        if (query.FolderId is int folderId) docs = docs.Where(d => d.FolderId == folderId);
        if (query.TeamId is int teamId) docs = docs.Where(d => d.TeamId == teamId);
        return docs;
    }

    private static IQueryable<Document> Sort(IQueryable<Document> docs, DocumentListQuery query)
    {
        // Ties are broken by id so paging is stable.
        var sorted = (query.Sort, query.Descending) switch
        {
            (DocumentSort.Name, false) => docs.OrderBy(d => d.Title.ToLower()),
            (DocumentSort.Name, true) => docs.OrderByDescending(d => d.Title.ToLower()),
            (DocumentSort.Size, false) => docs.OrderBy(d => d.FileSize),
            (DocumentSort.Size, true) => docs.OrderByDescending(d => d.FileSize),
            (_, false) => docs.OrderBy(d => d.UpdatedAt ?? d.CreatedAt),
            _ => docs.OrderByDescending(d => d.UpdatedAt ?? d.CreatedAt),
        };
        return query.Descending ? sorted.ThenByDescending(d => d.Id) : sorted.ThenBy(d => d.Id);
    }

    private static string NormalizeType(string? fileType) =>
        (fileType ?? string.Empty).Trim().TrimStart('.').ToLowerInvariant();}
