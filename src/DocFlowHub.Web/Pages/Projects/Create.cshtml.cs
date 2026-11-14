using System.ComponentModel.DataAnnotations;
using DocFlowHub.Core.Models.Projects.Dto;
using DocFlowHub.Core.Services.Interfaces;
using DocFlowHub.Web.Extensions;
using DocFlowHub.Web.Ui;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DocFlowHub.Web.Pages.Projects;

/// <summary>New project (decision S13-9): name, description and a colour from the design palette.</summary>
[Authorize]
public class CreateModel : PageModel
{
    private readonly IProjectService _projectService;

    public CreateModel(IProjectService projectService) => _projectService = projectService;

    [BindProperty]
    [Required(ErrorMessage = "Enter a name")]
    [StringLength(100, ErrorMessage = "The name can have at most 100 characters")]
    public string Name { get; set; } = string.Empty;

    [BindProperty]
    [StringLength(500, ErrorMessage = "The description can have at most 500 characters")]
    public string? Description { get; set; }

    [BindProperty]
    public string? Color { get; set; } = ProjectPalette.Colors[0].Hex;

    public string? ErrorMessage { get; private set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var result = await _projectService.CreateProjectAsync(new CreateProjectRequest
        {
            Name = Name.Trim(),
            Description = string.IsNullOrWhiteSpace(Description) ? null : Description.Trim(),
            Color = ProjectPalette.Clean(Color),
        }, User.GetUserId()!);
        if (!result.Succeeded || result.Data == null)
        {
            ErrorMessage = result.Error ?? "The project could not be created.";
            return Page();
        }

        TempData["ToastSuccess"] = $"Created project \"{result.Data.Name}\"";
        return Redirect(WorkspaceLinks.Project(result.Data.Id));
    }
}
