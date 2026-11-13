using DocFlowHub.Core.Models.Dashboard;
using DocFlowHub.Core.Services.Interfaces;
using DocFlowHub.Web.Ui;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DocFlowHub.Web.Pages.Admin;

/// <summary>Admin overview: the Dashboard layout (3a) with system-wide numbers.</summary>
[Authorize(Roles = "Administrator")]
public class IndexModel : PageModel
{
    private readonly IDashboardService _dashboardService;

    public IndexModel(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    /// <summary>"7d" (default), "30d" or "all".</summary>
    [BindProperty(SupportsGet = true)]
    public string? Period { get; set; }

    public DashboardPeriod SelectedPeriod { get; private set; }
    public AdminOverview? Overview { get; private set; }

    public async Task OnGetAsync()
    {
        SelectedPeriod = DashboardPeriods.Parse(Period);
        var result = await _dashboardService.GetAdminOverviewAsync(SelectedPeriod);
        Overview = result.Succeeded ? result.Data : null;
    }
}
