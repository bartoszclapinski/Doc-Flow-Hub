using DocFlowHub.Core.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace DocFlowHub.Web.Pages.Admin.Roles;

[Authorize(Roles = "Administrator")]
public class CreateModel : PageModel
{
    private readonly IRoleService _roleService;

    public CreateModel(IRoleService roleService)
    {
        _roleService = roleService;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        [Required]
        [StringLength(50, ErrorMessage = "The {0} must be at most {1} characters long.")]
        [Display(Name = "Role Name")]
        public string Name { get; set; } = string.Empty;

        [StringLength(200, ErrorMessage = "The {0} must be at most {1} characters long.")]
        [Display(Name = "Description")]
        public string? Description { get; set; }
    }

    public IActionResult OnGet()
    {
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var result = await _roleService.CreateRoleAsync(Input.Name, Input.Description);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Failed to create role.");
            return Page();
        }

        TempData["StatusMessage"] = $"Role '{Input.Name}' has been created successfully.";
        return RedirectToPage("./Index");
    }
} 