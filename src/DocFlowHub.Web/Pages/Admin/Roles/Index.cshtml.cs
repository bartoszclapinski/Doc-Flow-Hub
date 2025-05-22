using DocFlowHub.Core.Models.Role;
using DocFlowHub.Core.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DocFlowHub.Web.Pages.Admin.Roles;

[Authorize(Roles = "Administrator")]
public class IndexModel : PageModel
{
    private readonly IRoleService _roleService;

    public IndexModel(IRoleService roleService)
    {
        _roleService = roleService;
    }

    public IEnumerable<RoleDto> Roles { get; set; } = Enumerable.Empty<RoleDto>();

    public async Task<IActionResult> OnGetAsync()
    {
        Roles = await _roleService.GetRolesAsync();
        return Page();
    }
} 