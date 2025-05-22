using DocFlowHub.Core.Identity;
using DocFlowHub.Core.Models.Common;
using DocFlowHub.Core.Models.Role;
using DocFlowHub.Core.Services.Interfaces;
using DocFlowHub.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DocFlowHub.Infrastructure.Services.Role;

public class RoleService : IRoleService
{
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _context;

    public RoleService(
        RoleManager<IdentityRole> roleManager,
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext context)
    {
        _roleManager = roleManager;
        _userManager = userManager;
        _context = context;
    }

    public async Task<IEnumerable<RoleDto>> GetRolesAsync()
    {
        var roles = await _roleManager.Roles.ToListAsync();
        var roleDtos = new List<RoleDto>();

        foreach (var role in roles)
        {
            var usersCount = await _userManager.GetUsersInRoleAsync(role.Name!);
            roleDtos.Add(new RoleDto
            {
                Id = role.Id,
                Name = role.Name!,
                UsersCount = usersCount.Count,
                CreatedAt = DateTime.UtcNow // You might want to add CreatedAt to IdentityRole
            });
        }

        return roleDtos;
    }

    public async Task<RoleDto?> GetRoleByIdAsync(string roleId)
    {
        var role = await _roleManager.FindByIdAsync(roleId);
        if (role == null) return null;

        var usersCount = await _userManager.GetUsersInRoleAsync(role.Name!);
        return new RoleDto
        {
            Id = role.Id,
            Name = role.Name!,
            UsersCount = usersCount.Count,
            CreatedAt = DateTime.UtcNow // You might want to add CreatedAt to IdentityRole
        };
    }

    public async Task<ServiceResult> CreateRoleAsync(string name, string? description)
    {
        try
        {
            if (await _roleManager.RoleExistsAsync(name))
            {
                return ServiceResult.Failure($"Role '{name}' already exists.");
            }

            var role = new IdentityRole(name);
            var result = await _roleManager.CreateAsync(role);

            if (!result.Succeeded)
            {
                return ServiceResult.Failure(
                    $"Failed to create role: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }

            return ServiceResult.Success();
        }
        catch (Exception ex)
        {
            return ServiceResult.Failure($"An error occurred while creating the role: {ex.Message}");
        }
    }

    public async Task<ServiceResult> UpdateRoleAsync(string roleId, string name, string? description)
    {
        try
        {
            var role = await _roleManager.FindByIdAsync(roleId);
            if (role == null)
            {
                return ServiceResult.Failure($"Role with ID '{roleId}' not found.");
            }

            if (role.Name != name && await _roleManager.RoleExistsAsync(name))
            {
                return ServiceResult.Failure($"Role name '{name}' is already taken.");
            }

            role.Name = name;
            var result = await _roleManager.UpdateAsync(role);

            if (!result.Succeeded)
            {
                return ServiceResult.Failure(
                    $"Failed to update role: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }

            return ServiceResult.Success();
        }
        catch (Exception ex)
        {
            return ServiceResult.Failure($"An error occurred while updating the role: {ex.Message}");
        }
    }

    public async Task<ServiceResult> DeleteRoleAsync(string roleId)
    {
        try
        {
            var role = await _roleManager.FindByIdAsync(roleId);
            if (role == null)
            {
                return ServiceResult.Failure($"Role with ID '{roleId}' not found.");
            }

            var usersInRole = await _userManager.GetUsersInRoleAsync(role.Name!);
            if (usersInRole.Any())
            {
                return ServiceResult.Failure($"Cannot delete role '{role.Name}' because it has {usersInRole.Count} users assigned.");
            }

            var result = await _roleManager.DeleteAsync(role);
            if (!result.Succeeded)
            {
                return ServiceResult.Failure(
                    $"Failed to delete role: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }

            return ServiceResult.Success();
        }
        catch (Exception ex)
        {
            return ServiceResult.Failure($"An error occurred while deleting the role: {ex.Message}");
        }
    }

    public async Task<ServiceResult> AssignUserToRoleAsync(string userId, string roleId)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return ServiceResult.Failure($"User with ID '{userId}' not found.");
            }

            var role = await _roleManager.FindByIdAsync(roleId);
            if (role == null)
            {
                return ServiceResult.Failure($"Role with ID '{roleId}' not found.");
            }

            if (await _userManager.IsInRoleAsync(user, role.Name!))
            {
                return ServiceResult.Failure($"User is already in role '{role.Name}'.");
            }

            var result = await _userManager.AddToRoleAsync(user, role.Name!);
            if (!result.Succeeded)
            {
                return ServiceResult.Failure(
                    $"Failed to assign role: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }

            return ServiceResult.Success();
        }
        catch (Exception ex)
        {
            return ServiceResult.Failure($"An error occurred while assigning the role: {ex.Message}");
        }
    }

    public async Task<ServiceResult> RemoveUserFromRoleAsync(string userId, string roleId)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return ServiceResult.Failure($"User with ID '{userId}' not found.");
            }

            var role = await _roleManager.FindByIdAsync(roleId);
            if (role == null)
            {
                return ServiceResult.Failure($"Role with ID '{roleId}' not found.");
            }

            if (!await _userManager.IsInRoleAsync(user, role.Name!))
            {
                return ServiceResult.Failure($"User is not in role '{role.Name}'.");
            }

            var result = await _userManager.RemoveFromRoleAsync(user, role.Name!);
            if (!result.Succeeded)
            {
                return ServiceResult.Failure(
                    $"Failed to remove role: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }

            return ServiceResult.Success();
        }
        catch (Exception ex)
        {
            return ServiceResult.Failure($"An error occurred while removing the role: {ex.Message}");
        }
    }

    public async Task<IEnumerable<string>> GetUserRolesAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return Enumerable.Empty<string>();
        }

        return await _userManager.GetRolesAsync(user);
    }

    public async Task<IEnumerable<RoleDto>> GetAvailableRolesForUserAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return Enumerable.Empty<RoleDto>();
        }

        var userRoles = await _userManager.GetRolesAsync(user);
        var allRoles = await _roleManager.Roles.ToListAsync();

        return allRoles
            .Where(r => !userRoles.Contains(r.Name!))
            .Select(r => new RoleDto
            {
                Id = r.Id,
                Name = r.Name!,
                CreatedAt = DateTime.UtcNow // You might want to add CreatedAt to IdentityRole
            });
    }
} 