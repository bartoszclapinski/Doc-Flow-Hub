using System.ComponentModel.DataAnnotations;
using DocFlowHub.Core.Models.Projects.Dto;
using DocFlowHub.Core.Services.Interfaces;
using DocFlowHub.Web.Extensions;
using DocFlowHub.Web.Ui;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DocFlowHub.Web.Pages.Projects;

/// <summary>Edit a project (decision S13-9). The stored icon and the archived state are kept as they are.</summary>
[Authorize]
public class EditModel : PageModel
{
    private readonly IProjectService _projectService;

    public EditModel(IProjectService projectService) => _projectService = projectService;

    [BindProperty(SupportsGet = true)]
    public int Id { get; set; }

    [BindProperty]
    [Required(ErrorMessage = "Enter a name")]
    [StringLength(100, ErrorMessage = "The name can have at most 100 characters")]
    public string Name { get; set; } = string.Empty;

    [BindProperty]
    [StringLength(500, ErrorMessage = "The description can have at most 500 characters")]
    public string? Description { get; set; }

    [BindProperty]
    public string? Color { get; set; }

    public ProjectDto Project { get; private set; } = new();
    public string? ErrorMessage { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var load = await LoadAsync();
        if (load != null) return load;
        Name = Project.Name;
        Description = Project.Description;
        Color = Project.Color;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var load = await LoadAsync();
        if (load != null) return load;
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var result = await _projectService.UpdateProjectAsync(Id, new UpdateProjectRequest
        {
            Name = Name.Trim(),
            Description = string.IsNullOrWhiteSpace(Description) ? null : Description.Trim(),
            Color = ProjectPalette.Clean(Color),
            Icon = Project.Icon,
            IsActive = Project.IsActive,
        }, User.GetUserId()!);
        if (!result.Succeeded)
        {
            ErrorMessage = result.Error ?? "The project could not be saved.";
            return Page();
        }

        TempData["ToastSuccess"] = "Project saved";
        return Redirect(WorkspaceLinks.Project(Id));
    }

    private async Task<IActionResult?> LoadAsync()
    {
        var userId = User.GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return RedirectToPage("/Account/Login");
        }
        var project = await _projectService.GetProjectByIdAsync(Id, userId);
        if (!project.Succeeded || project.Data == null)
        {
            return NotFound();
        }
        Project = project.Data;
        return null;
    }
}
