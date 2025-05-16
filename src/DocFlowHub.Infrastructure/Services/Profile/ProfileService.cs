using DocFlowHub.Core.Identity;
using DocFlowHub.Core.Models.Profile;
using DocFlowHub.Core.Services.Interfaces;
using DocFlowHub.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DocFlowHub.Infrastructure.Services.Profile;

public class ProfileService : IProfileService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _context;

    public ProfileService(
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext context)
    {
        _userManager = userManager;
        _context = context;
    }

    public async Task<ProfileDto> GetProfileAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            throw new KeyNotFoundException($"User with ID {userId} not found.");
        }

        return MapToProfileDto(user);
    }

    public async Task<ProfileDto> UpdateProfileAsync(string userId, UpdateProfileRequest request)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            throw new KeyNotFoundException($"User with ID {userId} not found.");
        }

        user.FirstName = request.FirstName;
        user.LastName = request.LastName;
        user.Bio = request.Bio;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException($"Failed to update user profile: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

        return MapToProfileDto(user);
    }

    public async Task<bool> UpdateProfilePictureAsync(string userId, string imageUrl)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            throw new KeyNotFoundException($"User with ID {userId} not found.");
        }

        user.ProfilePictureUrl = imageUrl;
        var result = await _userManager.UpdateAsync(user);
        
        return result.Succeeded;
    }

    public async Task<bool> DeleteProfilePictureAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            throw new KeyNotFoundException($"User with ID {userId} not found.");
        }

        user.ProfilePictureUrl = null;
        var result = await _userManager.UpdateAsync(user);
        
        return result.Succeeded;
    }

    private static ProfileDto MapToProfileDto(ApplicationUser user)
    {
        return new ProfileDto
        {
            Id = user.Id,
            UserName = user.UserName ?? string.Empty,
            Email = user.Email ?? string.Empty,
            FirstName = user.FirstName,
            LastName = user.LastName,
            ProfilePictureUrl = user.ProfilePictureUrl,
            Bio = user.Bio,
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt,
            IsActive = user.IsActive
        };
    }
} 