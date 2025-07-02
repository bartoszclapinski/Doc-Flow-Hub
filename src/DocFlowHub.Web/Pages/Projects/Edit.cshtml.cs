using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using DocFlowHub.Core.Models.Projects.Dto;
using DocFlowHub.Core.Services.Interfaces;
using DocFlowHub.Web.Extensions;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace DocFlowHub.Web.Pages.Projects;

[Authorize]
public class EditModel : PageModel
{
    private readonly IProjectService _projectService;

    public EditModel(IProjectService projectService)
    {
        _projectService = projectService;
    }

    [BindProperty]
    public int Id { get; set; }

    [BindProperty]
    [Required(ErrorMessage = "Project name is required")]
    [StringLength(100, ErrorMessage = "Project name cannot exceed 100 characters")]
    public string Name { get; set; } = string.Empty;

    [BindProperty]
    [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
    public string? Description { get; set; }

    [BindProperty]
    [StringLength(7, ErrorMessage = "Color must be a valid hex color")]
    public string? Color { get; set; } = "#0d6efd";

    [BindProperty]
    [StringLength(50, ErrorMessage = "Icon name cannot exceed 50 characters")]
    public string? Icon { get; set; } = "kanban";

    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var userId = User.GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return RedirectToPage("/Account/Login");
        }

        // Load the project
        var projectResult = await _projectService.GetProjectByIdAsync(id);
        if (!projectResult.Succeeded)
        {
            ErrorMessage = projectResult.Error ?? "Project not found.";
            return Page();
        }

        var project = projectResult.Data;
        if (project == null)
        {
            ErrorMessage = "Project not found.";
            return Page();
        }

        // Check if user is the owner
        if (project.OwnerId != userId)
        {
            ErrorMessage = "You don't have permission to edit this project.";
            return Page();
        }

        // Populate form fields
        Id = project.Id;
        Name = project.Name;
        Description = project.Description;
        Color = project.Color ?? "#0d6efd";
        Icon = project.Icon ?? "kanban";

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var userId = User.GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return RedirectToPage("/Account/Login");
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var request = new UpdateProjectRequest
        {
            Name = Name,
            Description = Description,
            Color = Color,
            Icon = Icon
        };

        var result = await _projectService.UpdateProjectAsync(Id, request, userId);
        if (!result.Succeeded)
        {
            ErrorMessage = result.Error;
            return Page();
        }

        TempData["SuccessMessage"] = $"Project '{Name}' has been updated successfully.";
        return RedirectToPage("./Details", new { id = Id });
    }
} 