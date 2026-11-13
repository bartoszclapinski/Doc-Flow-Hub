using DocFlowHub.Core.Models.Common;
using DocFlowHub.Core.Models.Dashboard;

namespace DocFlowHub.Core.Services.Interfaces;

/// <summary>Read-only aggregate queries for the Dashboard and the admin overview.</summary>
public interface IDashboardService
{
    /// <summary>Dashboard for one user: documents they own or that are shared with one of their teams.</summary>
    Task<ServiceResult<UserDashboard>> GetUserDashboardAsync(string userId, DashboardPeriod period);

    /// <summary>System-wide overview for administrators.</summary>
    Task<ServiceResult<AdminOverview>> GetAdminOverviewAsync(DashboardPeriod period);
}
