using DocFlowHub.Core.Models.Workspace;
using DocFlowHub.Core.Services.Interfaces;
using DocFlowHub.Web.Extensions;
using DocFlowHub.Web.Ui;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DocFlowHub.Web.Pages.Projects;

/// <summary>Projects (design 8a, decision S13-3): table of active or archived projects with an inspector.</summary>
[Authorize]
public class IndexModel : PageModel
{
    private readonly IWorkspaceBrowserService _workspace;
    private readonly IProjectService _projectService;
    private readonly IDemoModeService _demoMode;

    public IndexModel(IWorkspaceBrowserService workspace, IProjectService projectService, IDemoModeService demoMode)
    {
        _workspace = workspace;
        _projectService = projectService;
        _demoMode = demoMode;
    }

    /// <summary>"archived" shows archived projects; anything else the active ones.</summary>
    [BindProperty(SupportsGet = true)]
    public string? Status { get; set; }

    public bool Archived => string.Equals(Status, "archived", StringComparison.OrdinalIgnoreCase);
    public IReadOnlyList<ProjectRow> Projects { get; private set; } = Array.Empty<ProjectRow>();
    public ProjectInspector? Selected { get; private set; }
    public string? ErrorMessage { get; private set; }
    public bool IsDemo => _demoMode.IsEnabled;
    public string ReturnUrl => Archived ? "/Projects?status=archived" : "/Projects";

    public async Task<IActionResult> OnGetAsync()
    {
        var userId = User.GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return RedirectToPage("/Account/Login");
        }

        var result = await _workspace.GetProjectsAsync(userId, Archived);
        if (!result.Succeeded || result.Data == null)
        {
            ErrorMessage = result.Error ?? "The projects could not be loaded.";
            return Page();
        }
        Projects = result.Data;

        if (Projects.Count > 0)
        {
            var first = await _workspace.GetProjectAsync(userId, Projects[0].Id);
            Selected = first.Succeeded ? first.Data : null;
        }
        return Page();
    }

    public async Task<IActionResult> OnGetInspectorAsync(int id, string? returnUrl)
    {
        var userId = User.GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var result = await _workspace.GetProjectAsync(userId, id);
        if (!result.Succeeded || result.Data == null)
        {
            Response.StatusCode = StatusCodes.Status404NotFound;
            return Content("This project is no longer available.", "text/plain");
        }
        return Partial("_ProjectInspector", new ProjectInspectorView(result.Data, IsDemo, OnProjectPage: false, Safe(returnUrl)));
    }

    public async Task<IActionResult> OnPostArchiveAsync(int projectId, string? returnUrl) =>
        await RunAsync(returnUrl, userId => _projectService.ArchiveProjectAsync(projectId, userId), "Project archived");

    public async Task<IActionResult> OnPostRestoreAsync(int projectId, string? returnUrl) =>
        await RunAsync(returnUrl, userId => _projectService.RestoreProjectAsync(projectId, userId), "Project restored");

    public async Task<IActionResult> OnPostDeleteAsync(int projectId) =>
        await RunAsync("/Projects", userId => _projectService.DeleteProjectAsync(projectId, userId), "Project deleted");

    private async Task<IActionResult> RunAsync(string? returnUrl, Func<string, Task<Core.Models.Common.ServiceResult>> action, string success)
    {
        var userId = User.GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var result = await action(userId);
        if (result.Succeeded) TempData["ToastSuccess"] = success;
        else TempData["ToastError"] = result.Error ?? "The project could not be changed.";
        return LocalRedirect(Safe(returnUrl));
    }

    private string Safe(string? returnUrl) =>
        !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl) ? returnUrl : "/Projects";
}
