using DocFlowHub.Core.Models.Common;
using DocFlowHub.Core.Models.Role;

namespace DocFlowHub.Core.Services.Interfaces;

public interface IRoleService
{
    Task<IEnumerable<RoleDto>> GetRolesAsync();
    Task<RoleDto?> GetRoleByIdAsync(string roleId);
    Task<ServiceResult> CreateRoleAsync(string name, string? description);
    Task<ServiceResult> UpdateRoleAsync(string roleId, string name, string? description);
    Task<ServiceResult> DeleteRoleAsync(string roleId);
    Task<ServiceResult> AssignUserToRoleAsync(string userId, string roleId);
    Task<ServiceResult> RemoveUserFromRoleAsync(string userId, string roleId);
    Task<IEnumerable<string>> GetUserRolesAsync(string userId);
    Task<IEnumerable<RoleDto>> GetAvailableRolesForUserAsync(string userId);
} 