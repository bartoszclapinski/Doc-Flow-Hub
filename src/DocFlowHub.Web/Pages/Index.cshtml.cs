using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Identity;
using DocFlowHub.Core.Identity;
using DocFlowHub.Infrastructure.Data;


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

        public string UserName { get; set; }
        public int TotalDocuments { get; set; }
        public int TotalTeams { get; set; }
        public int RecentUpdates { get; set; }
        public int SharedDocuments { get; set; }
        public List<DocumentViewModel> RecentDocuments { get; set; }
        public List<ActivityViewModel> RecentActivities { get; set; }

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
            RecentDocuments = new List<DocumentViewModel>();
            RecentActivities = new List<ActivityViewModel>();

            return Page();
        }
    }

    public class DocumentViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime LastModified { get; set; }
    }

    public class ActivityViewModel
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
