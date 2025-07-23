using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Identity;
using DocFlowHub.Core.Identity;
using DocFlowHub.Core.Services.Interfaces;
using DocFlowHub.Core.Models.Documents;
using DocFlowHub.Core.Models.Teams;
using DocFlowHub.Web.Extensions;
using System.Security.Claims;

namespace DocFlowHub.Web.Pages
{
    public class IndexModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IDocumentService _documentService;
        private readonly ITeamService _teamService;

        public IndexModel(
            UserManager<ApplicationUser> userManager,
            IDocumentService documentService,
            ITeamService teamService)
        {
            _userManager = userManager;
            _documentService = documentService;
            _teamService = teamService;
        }

        public string? UserName { get; set; }
        public int TotalDocuments { get; set; }
        public int TotalTeams { get; set; }
        public int TeamsAsOwner { get; set; }
        public int TeamsAsMember { get; set; }
        public int RecentUpdates { get; set; }
        public int SharedDocuments { get; set; }
        public List<DocumentSummary> RecentDocuments { get; set; } = new();
        public List<TeamSummary> RecentTeams { get; set; } = new();
        public List<ActivitySummary> RecentActivities { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                // Show landing page for unauthenticated users
                return Page();
            }

            // Redirect administrators to admin dashboard
            if (User.IsInRole("Administrator"))
            {
                return RedirectToPage("/Admin/Index");
            }

            UserName = user.UserName;
            var userId = User.GetUserId();

            if (!string.IsNullOrEmpty(userId))
            {
                await LoadUserStatistics(userId);
                await LoadRecentDocuments(userId);
                await LoadRecentTeams(userId);
                await LoadRecentActivities(userId);
            }

            return Page();
        }

        private async Task LoadUserStatistics(string userId)
        {
            try
            {
                // Get user's documents using GetDocumentsAsync with filter
                var userFilter = new DocumentFilter
                {
                    OwnerId = userId,
                    PageNumber = 1,
                    PageSize = 1000, // Get all documents for counting
                    IncludeDeleted = false
                };

                var userDocuments = await _documentService.GetDocumentsAsync(userId, userFilter);
                if (userDocuments.Succeeded)
                {
                    TotalDocuments = userDocuments.Data.TotalItems;

                    // Count recent updates (documents modified in last 7 days)
                    RecentUpdates = userDocuments.Data.Items.Count(d => 
                        (d.UpdatedAt ?? d.CreatedAt) >= DateTime.Now.AddDays(-7));

                    // Get shared documents (documents with TeamId)
                    SharedDocuments = userDocuments.Data.Items.Count(d => d.TeamId.HasValue);
                }

                // Get user's teams count
                var userTeams = await _teamService.GetUserTeamsAsync(userId, new TeamFilter());
                if (userTeams.Succeeded)
                {
                    TotalTeams = userTeams.Data.TotalItems;
                    TeamsAsOwner = userTeams.Data.Items.Count(t => t.OwnerId == userId);
                    TeamsAsMember = userTeams.Data.Items.Count(t => t.OwnerId != userId);
                }
            }
            catch (Exception)
            {
                // Log error and set default values
                TotalDocuments = 0;
                TotalTeams = 0;
                RecentUpdates = 0;
                SharedDocuments = 0;
            }
        }

        private async Task LoadRecentDocuments(string userId)
        {
            try
            {
                var filter = new DocumentFilter
                {
                    OwnerId = userId,
                    PageNumber = 1,
                    PageSize = 5, // Get latest 5 documents
                    IncludeDeleted = false
                };

                var recentDocs = await _documentService.GetDocumentsAsync(userId, filter);
                
                if (recentDocs.Succeeded)
                {
                    // Sort by UpdatedAt descending to get most recent first
                    RecentDocuments = recentDocs.Data.Items
                        .OrderByDescending(d => d.UpdatedAt ?? d.CreatedAt)
                        .Take(5)
                        .Select(d => new DocumentSummary
                        {
                            Id = d.Id,
                            Title = d.Title,
                            Description = d.Description ?? "",
                            LastModified = d.UpdatedAt ?? d.CreatedAt,
                            FileType = d.FileType,
                            FileSize = d.FileSize
                        }).ToList();
                }
            }
            catch (Exception)
            {
                // Log error and set empty list
                RecentDocuments = new List<DocumentSummary>();
            }
        }

        private async Task LoadRecentTeams(string userId)
        {
            try
            {
                var filter = new TeamFilter
                {
                    PageNumber = 1,
                    PageSize = 5 // Get latest 5 teams
                };

                var recentTeams = await _teamService.GetUserTeamsAsync(userId, filter);
                
                if (recentTeams.Succeeded)
                {
                    // Sort by UpdatedAt descending to get most recent first
                    RecentTeams = recentTeams.Data.Items
                        .OrderByDescending(t => t.UpdatedAt.HasValue ? t.UpdatedAt.Value : t.CreatedAt)
                        .Take(5)
                        .Select(t => new TeamSummary
                        {
                            Id = t.Id,
                            Name = t.Name,
                            Description = t.Description ?? "",
                            LastModified = t.UpdatedAt.HasValue ? t.UpdatedAt.Value : t.CreatedAt,
                            MemberCount = t.MemberCount,
                            IsOwner = t.OwnerId == userId
                        }).ToList();
                }
            }
            catch (Exception)
            {
                // Log error and set empty list
                RecentTeams = new List<TeamSummary>();
            }
        }

        private async Task LoadRecentActivities(string userId)
        {
            try
            {
                // Generate activity feed based on recent document changes
                var filter = new DocumentFilter
                {
                    OwnerId = userId,
                    PageNumber = 1,
                    PageSize = 20, // Get more documents to analyze for activities
                    IncludeDeleted = false
                };

                var recentDocs = await _documentService.GetDocumentsAsync(userId, filter);
                var activities = new List<ActivitySummary>();

                if (recentDocs.Succeeded)
                {
                    foreach (var doc in recentDocs.Data.Items.Take(10))
                    {
                        // Check if document was updated recently
                        if (doc.UpdatedAt.HasValue && doc.UpdatedAt.Value >= DateTime.Now.AddDays(-30))
                        {
                            activities.Add(new ActivitySummary
                            {
                                Title = "Document Updated",
                                Description = $"{doc.Title} was updated",
                                Timestamp = doc.UpdatedAt.Value,
                                UserId = userId,
                                UserName = UserName ?? "Unknown"
                            });
                        }
                        else if (doc.CreatedAt >= DateTime.Now.AddDays(-30))
                        {
                            activities.Add(new ActivitySummary
                            {
                                Title = "Document Created",
                                Description = $"{doc.Title} was uploaded",
                                Timestamp = doc.CreatedAt,
                                UserId = userId,
                                UserName = UserName ?? "Unknown"
                            });
                        }

                        // Add version-related activities if document has multiple versions
                        if (doc.CurrentVersionId.HasValue)
                        {
                            try
                            {
                                var versions = await _documentService.GetDocumentVersionsAsync(doc.Id, new DocumentFilter { PageSize = 5 });
                                if (versions.Succeeded && versions.Data.TotalItems > 1)
                                {
                                    var latestVersion = versions.Data.Items.FirstOrDefault();
                                    if (latestVersion != null && latestVersion.CreatedAt >= DateTime.Now.AddDays(-30))
                                    {
                                        activities.Add(new ActivitySummary
                                        {
                                            Title = "New Version Uploaded",
                                            Description = $"New version of {doc.Title} was uploaded",
                                            Timestamp = latestVersion.CreatedAt,
                                            UserId = userId,
                                            UserName = UserName ?? "Unknown"
                                        });
                                    }
                                }
                            }
                            catch
                            {
                                // Skip version info if error occurs
                            }
                        }
                    }
                }

                // Sort activities by timestamp and take the most recent ones
                RecentActivities = activities
                    .OrderByDescending(a => a.Timestamp)
                    .Take(5)
                    .ToList();
            }
            catch (Exception)
            {
                // Log error and set empty list
                RecentActivities = new List<ActivitySummary>();
            }
        }
    }

    public class DocumentSummary
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime LastModified { get; set; }
        public string FileType { get; set; } = string.Empty;
        public long FileSize { get; set; } // Size in bytes
    }

    public class ActivitySummary
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
    }

    public class TeamSummary
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime LastModified { get; set; }
        public int MemberCount { get; set; }
        public bool IsOwner { get; set; }
    }
}
