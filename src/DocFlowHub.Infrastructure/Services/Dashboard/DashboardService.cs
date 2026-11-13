using DocFlowHub.Core.Models;
using DocFlowHub.Core.Models.Common;
using DocFlowHub.Core.Models.Dashboard;
using DocFlowHub.Core.Models.Documents;
using DocFlowHub.Core.Services.Interfaces;
using DocFlowHub.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DocFlowHub.Infrastructure.Services.Dashboard;

/// <summary>
/// Aggregate, read-only queries behind the Dashboard and the admin overview. Every number is a
/// COUNT/SUM in the database, so the pages no longer load whole document lists to count them.
/// Queries stay simple enough for both SQL Server and the SQLite demo.
/// </summary>
public class DashboardService : IDashboardService
{
    private const string AdministratorRole = "Administrator";
    private const int RecentDocumentsCount = 10;
    private const int WithoutSummaryShown = 3;
    private const int TeamsShown = 4;
    private const int RecentUsersCount = 10;
    private const int ActivityCount = 20;

    private readonly ApplicationDbContext _context;
    private readonly ILogger<DashboardService> _logger;

    public DashboardService(ApplicationDbContext context, ILogger<DashboardService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ServiceResult<UserDashboard>> GetUserDashboardAsync(string userId, DashboardPeriod period)
    {
        try
        {
            var since = Since(period);

            // Same visibility rule as DocumentService.GetDocumentsAsync: owned, or shared with a team
            // the user belongs to. Deleted documents are hidden by the global query filter.
            var docs = _context.Documents.Where(d =>
                d.OwnerId == userId ||
                (d.TeamId != null && _context.TeamMembers.Any(m => m.UserId == userId && m.TeamId == d.TeamId)));
            var versions = _context.DocumentVersions.Where(v => docs.Any(d => d.Id == v.DocumentId));

            var teams = _context.Teams.Where(t => t.OwnerId == userId || t.Members.Any(m => m.UserId == userId));
            var memberships = _context.TeamMembers.Where(m => teams.Any(t => t.Id == m.TeamId));

            var withoutSummary = docs.Where(d => !_context.DocumentSummaries.Any(s => s.DocumentId == d.Id));

            var dashboard = new UserDashboard
            {
                Documents = await docs.CountAsync(),
                Versions = await versions.CountAsync(),
                SharedWithMe = await docs.CountAsync(d => d.OwnerId != userId),
                VersionsInPeriod = since == null ? null : await versions.CountAsync(v => v.CreatedAt >= since),
                Teams = await teams.CountAsync(),
                RecentDocuments = await DocumentRowsAsync(docs, RecentDocumentsCount),
                DocumentsWithoutSummary = await withoutSummary.CountAsync(),
                NewestWithoutSummary = await DocumentRowsAsync(withoutSummary, WithoutSummaryShown),
            };

            (dashboard.Storage, dashboard.StorageBytes) = await StorageAsync(docs);

            var teamRows = await teams
                .OrderBy(t => t.Name)
                .Take(TeamsShown)
                .Select(t => new
                {
                    t.Id,
                    t.Name,
                    IsOwner = t.OwnerId == userId,
                    Role = t.Members.Where(m => m.UserId == userId).Select(m => (TeamRole?)m.Role).FirstOrDefault(),
                    Members = t.Members.Count,
                })
                .ToListAsync();
            dashboard.TeamList = teamRows.Select(t => new DashboardTeamRow
            {
                Id = t.Id,
                Name = t.Name,
                Role = t.IsOwner ? "owner" : t.Role == TeamRole.Admin ? "admin" : "member",
                MemberCount = t.Members,
            }).ToList();

            dashboard.Activity = await ActivityAsync(docs, memberships, since, includeRegistrations: false);
            return ServiceResult<UserDashboard>.Success(dashboard);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load the dashboard for user {UserId}", userId);
            return ServiceResult<UserDashboard>.Failure("The dashboard could not be loaded.");
        }
    }

    public async Task<ServiceResult<AdminOverview>> GetAdminOverviewAsync(DashboardPeriod period)
    {
        try
        {
            var since = Since(period);
            var today = DateTime.UtcNow.Date;

            var docs = _context.Documents.AsQueryable();
            var adminIds = _context.UserRoles
                .Where(ur => _context.Roles.Any(r => r.Id == ur.RoleId && r.Name == AdministratorRole))
                .Select(ur => ur.UserId);

            var overview = new AdminOverview
            {
                Users = await _context.Users.CountAsync(),
                Administrators = await adminIds.Distinct().CountAsync(),
                InactiveUsers = await _context.Users.CountAsync(u => !u.IsActive),
                Documents = await docs.CountAsync(),
                Versions = await _context.DocumentVersions.CountAsync(v => docs.Any(d => d.Id == v.DocumentId)),
                Teams = await _context.Teams.CountAsync(),
                NewDocumentsInPeriod = since == null ? null : await docs.CountAsync(d => d.CreatedAt >= since),
                NewUsersInPeriod = since == null ? null : await _context.Users.CountAsync(u => u.CreatedAt >= since),
                AiRequestsToday = await _context.AIUsageLogs.CountAsync(l => l.CreatedAt >= today),
            };

            var aiLogs = since == null ? _context.AIUsageLogs : _context.AIUsageLogs.Where(l => l.CreatedAt >= since);
            overview.AiRequestsInPeriod = await aiLogs.CountAsync();
            overview.AiFailedInPeriod = await aiLogs.CountAsync(l => !l.IsSuccess);
            overview.AiTokensInPeriod = await aiLogs.SumAsync(l => (long)l.TokensUsed);

            overview.RecentUsers = await _context.Users
                .OrderByDescending(u => u.CreatedAt)
                .Take(RecentUsersCount)
                .Select(u => new AdminUserRow
                {
                    Id = u.Id,
                    Name = (u.FirstName + " " + u.LastName).Trim(),
                    Email = u.Email ?? string.Empty,
                    IsAdministrator = adminIds.Contains(u.Id),
                    IsActive = u.IsActive,
                    CreatedAt = u.CreatedAt,
                    LastLoginAt = u.LastLoginAt,
                })
                .ToListAsync();
            foreach (var user in overview.RecentUsers.Where(u => u.Name.Length == 0))
            {
                user.Name = user.Email;
            }

            (overview.Storage, overview.StorageBytes) = await StorageAsync(docs);

            overview.LargestTeams = await _context.Teams
                .OrderByDescending(t => t.Members.Count)
                .ThenBy(t => t.Name)
                .Take(TeamsShown)
                .Select(t => new DashboardTeamRow { Id = t.Id, Name = t.Name, MemberCount = t.Members.Count })
                .ToListAsync();

            overview.Activity = await ActivityAsync(docs, _context.TeamMembers, since, includeRegistrations: true);
            return ServiceResult<AdminOverview>.Success(overview);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load the admin overview");
            return ServiceResult<AdminOverview>.Failure("The overview could not be loaded.");
        }
    }

    private static DateTime? Since(DashboardPeriod period) =>
        period == DashboardPeriod.All ? null : DateTime.UtcNow.AddDays(-(int)period);

    private async Task<List<DashboardDocumentRow>> DocumentRowsAsync(IQueryable<Document> docs, int take)
    {
        var rows = await docs
            .OrderByDescending(d => d.UpdatedAt ?? d.CreatedAt)
            .Take(take)
            .Select(d => new
            {
                d.Id,
                d.Title,
                d.FileType,
                d.FileSize,
                Modified = d.UpdatedAt ?? d.CreatedAt,
                FolderPath = d.Folder != null ? d.Folder.Path : null,
                ProjectName = d.Project != null ? d.Project.Name : null,
                Version = d.Versions.Max(v => (int?)v.VersionNumber),
                d.Owner.FirstName,
                d.Owner.LastName,
                d.Owner.Email,
                HasSummary = _context.DocumentSummaries.Any(s => s.DocumentId == d.Id),
            })
            .ToListAsync();

        return rows.Select(r => new DashboardDocumentRow
        {
            Id = r.Id,
            Title = r.Title,
            FileType = r.FileType,
            FileSize = r.FileSize,
            ModifiedAt = r.Modified,
            Location = !string.IsNullOrWhiteSpace(r.FolderPath) ? r.FolderPath.Trim('/') : r.ProjectName,
            Version = r.Version,
            OwnerName = DisplayName(r.FirstName, r.LastName, r.Email),
            HasSummary = r.HasSummary,
        }).ToList();
    }

    private static async Task<(List<StorageByType> types, long total)> StorageAsync(IQueryable<Document> docs)
    {
        var raw = await docs
            .GroupBy(d => d.FileType)
            .Select(g => new { FileType = g.Key, Count = g.Count(), Bytes = g.Sum(d => d.FileSize) })
            .ToListAsync();

        // ".PDF" and ".pdf" are the same type.
        var types = raw
            .GroupBy(r => r.FileType.Trim().TrimStart('.').ToLowerInvariant())
            .Select(g => new StorageByType { FileType = g.Key, Count = g.Sum(x => x.Count), Bytes = g.Sum(x => x.Bytes) })
            .OrderByDescending(t => t.Bytes)
            .ToList();
        return (types, types.Sum(t => t.Bytes));
    }

    /// <summary>
    /// Merges the events the database already records: versions, AI summaries and comparisons,
    /// team joins and (admin only) new accounts. Moves and shares are not recorded, so they are absent.
    /// </summary>
    private async Task<List<ActivityItem>> ActivityAsync(
        IQueryable<Document> docs, IQueryable<TeamMember> memberships, DateTime? since, bool includeRegistrations)
    {
        var versionQuery = _context.DocumentVersions.Where(v => docs.Any(d => d.Id == v.DocumentId));
        var summaryQuery = from s in _context.DocumentSummaries
                           join d in docs on s.DocumentId equals d.Id
                           select new { s.GeneratedAt, DocumentId = d.Id, d.Title };
        var comparisonQuery = from c in _context.VersionComparisons
                              join d in docs on c.DocumentId equals d.Id
                              select new
                              {
                                  c.GeneratedAt,
                                  DocumentId = d.Id,
                                  d.Title,
                                  From = _context.DocumentVersions.Where(v => v.Id == c.FromVersionId).Select(v => (int?)v.VersionNumber).FirstOrDefault(),
                                  To = _context.DocumentVersions.Where(v => v.Id == c.ToVersionId).Select(v => (int?)v.VersionNumber).FirstOrDefault(),
                              };
        if (since != null)
        {
            versionQuery = versionQuery.Where(v => v.CreatedAt >= since);
            summaryQuery = summaryQuery.Where(s => s.GeneratedAt >= since);
            comparisonQuery = comparisonQuery.Where(c => c.GeneratedAt >= since);
            memberships = memberships.Where(m => m.JoinedAt >= since);
        }

        var versionEvents = await versionQuery
            .OrderByDescending(v => v.CreatedAt)
            .Take(ActivityCount)
            .Select(v => new { v.CreatedAt, v.VersionNumber, v.FileHash, v.UserId, v.UserName, v.DocumentId, v.Document.Title })
            .ToListAsync();
        var summaryEvents = await summaryQuery.OrderByDescending(s => s.GeneratedAt).Take(ActivityCount).ToListAsync();
        var comparisonEvents = await comparisonQuery.OrderByDescending(c => c.GeneratedAt).Take(ActivityCount).ToListAsync();
        var joinEvents = await memberships
            .OrderByDescending(m => m.JoinedAt)
            .Take(ActivityCount)
            .Select(m => new { m.JoinedAt, m.UserId, m.TeamId, TeamName = m.Team.Name })
            .ToListAsync();

        var registrations = includeRegistrations
            ? await (since == null ? _context.Users : _context.Users.Where(u => u.CreatedAt >= since))
                .OrderByDescending(u => u.CreatedAt)
                .Take(ActivityCount)
                .Select(u => new { u.Id, u.CreatedAt })
                .ToListAsync()
            : new();

        var userIds = versionEvents.Select(v => v.UserId)
            .Concat(joinEvents.Select(j => j.UserId))
            .Concat(registrations.Select(r => r.Id))
            .Where(id => !string.IsNullOrEmpty(id))
            .Distinct()
            .ToList();
        var names = (await _context.Users
                .Where(u => userIds.Contains(u.Id))
                .Select(u => new { u.Id, u.FirstName, u.LastName, u.Email })
                .ToListAsync())
            .ToDictionary(u => u.Id, u => DisplayName(u.FirstName, u.LastName, u.Email));

        string NameOf(string? id, string? fallback) =>
            id != null && names.TryGetValue(id, out var name) ? name
            : string.IsNullOrWhiteSpace(fallback) ? "Someone" : fallback;

        var items = new List<ActivityItem>();
        items.AddRange(versionEvents.Select(v => new ActivityItem
        {
            Kind = v.VersionNumber <= 1 ? ActivityKind.Created : ActivityKind.Version,
            At = v.CreatedAt,
            Actor = NameOf(v.UserId, v.UserName),
            Verb = v.VersionNumber <= 1 ? "created" : $"uploaded v{v.VersionNumber} of",
            Subject = v.Title,
            DocumentId = v.DocumentId,
            Hash = ShortHash(v.FileHash),
        }));
        items.AddRange(summaryEvents.Select(s => new ActivityItem
        {
            Kind = ActivityKind.AiSummary,
            At = s.GeneratedAt,
            Verb = "summarized",
            Subject = s.Title,
            DocumentId = s.DocumentId,
        }));
        items.AddRange(comparisonEvents.Select(c => new ActivityItem
        {
            Kind = ActivityKind.AiComparison,
            At = c.GeneratedAt,
            Verb = "compared",
            Subject = c.Title,
            SubjectSuffix = c.From != null && c.To != null ? $" v{c.From} ↔ v{c.To}" : null,
            DocumentId = c.DocumentId,
        }));
        items.AddRange(joinEvents.Select(j => new ActivityItem
        {
            Kind = ActivityKind.TeamJoined,
            At = j.JoinedAt,
            Actor = NameOf(j.UserId, null),
            Verb = "joined",
            Subject = j.TeamName,
            TeamId = j.TeamId,
        }));
        items.AddRange(registrations.Select(r => new ActivityItem
        {
            Kind = ActivityKind.UserRegistered,
            At = r.CreatedAt,
            Actor = NameOf(r.Id, null),
            Verb = "created an account",
        }));

        return items.OrderByDescending(i => i.At).Take(ActivityCount).ToList();
    }

    private static string DisplayName(string? first, string? last, string? email)
    {
        var name = $"{first} {last}".Trim();
        return name.Length > 0 ? name : email ?? "Unknown";
    }

    /// <summary>First 7 hex characters of the stored MD5 hex string, like a short commit id.</summary>
    private static string? ShortHash(string? hash) =>
        hash is { Length: >= 7 } && hash[..7].All(Uri.IsHexDigit) ? hash[..7].ToLowerInvariant() : null;
}
