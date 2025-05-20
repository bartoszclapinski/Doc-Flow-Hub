using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Identity;
using DocFlowHub.Core.Identity;
using DocFlowHub.Infrastructure.Data;
using System.Security.Claims;

namespace DocFlowHub.Web.Pages
{
    public class IndexModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public IndexModel(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        public string? UserName { get; set; }
        public int TotalDocuments { get; set; }
        public int TotalTeams { get; set; }
        public int RecentUpdates { get; set; }
        public int SharedDocuments { get; set; }
        public List<DocumentSummary> RecentDocuments { get; set; } = new();
        public List<ActivitySummary> RecentActivities { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToPage("/Account/Login");
            }

            UserName = user.UserName;
            TotalDocuments = 0;
            TotalTeams = 0;
            RecentUpdates = 0;
            SharedDocuments = 0;
            RecentDocuments = new List<DocumentSummary>();
            RecentActivities = new List<ActivitySummary>();

            // TODO: Replace with actual data from services
            RecentDocuments = new List<DocumentSummary>
            {
                new() 
                { 
                    Id = 1, 
                    Title = "Project Proposal", 
                    Description = "Initial project proposal document",
                    LastModified = DateTime.Now.AddDays(-1),
                    FileType = "pdf",
                    FileSize = 1250000 // 1.25 MB
                },
                new() 
                { 
                    Id = 2,
                    Title = "Meeting Notes", 
                    Description = "Notes from team meeting",
                    LastModified = DateTime.Now.AddHours(-3),
                    FileType = "docx",
                    FileSize = 350000 // 350 KB
                }
            };

            RecentActivities = new List<ActivitySummary>
            {
                new() 
                { 
                    Title = "Document Updated", 
                    Description = "Project proposal was updated",
                    Timestamp = DateTime.Now.AddDays(-1),
                    UserId = "user-1",
                    UserName = "John Doe"
                },
                new() 
                { 
                    Title = "New Comment", 
                    Description = "Comment added to meeting notes",
                    Timestamp = DateTime.Now.AddHours(-5),
                    UserId = "user-2",
                    UserName = "Jane Smith"
                }
            };

            return Page();
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
}
