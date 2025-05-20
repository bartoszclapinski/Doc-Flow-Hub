using DocFlowHub.Core.Identity;
using DocFlowHub.Core.Models.Common;
using DocFlowHub.Core.Models.Profile;
using DocFlowHub.Core.Services.Interfaces;
using DocFlowHub.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DocFlowHub.Infrastructure.Services.Profile;

public class ProfileService : IProfileService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _environment;

    public ProfileService(
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext context,
        IWebHostEnvironment environment)
    {
        _userManager = userManager;
        _context = context;
        _environment = environment;
    }

    public async Task<ProfileDto> GetProfileAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId) 
            ?? throw new KeyNotFoundException($"User with ID {userId} not found.");
        return MapToProfileDto(user);
    }

    public async Task<ProfileDto> UpdateProfileAsync(string userId, UpdateProfileRequest request)
    {
        var user = await _userManager.FindByIdAsync(userId) 
            ?? throw new KeyNotFoundException($"User with ID {userId} not found.");
        user.FirstName = request.FirstName;
        user.LastName = request.LastName;
        user.Bio = request.Bio;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)        
            throw new InvalidOperationException($"Failed to update user profile: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        
        return MapToProfileDto(user);
    }

    public async Task<ServiceResult> UpdateProfilePictureAsync(string userId, IFormFile file)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId) 
                ?? throw new KeyNotFoundException($"User with ID {userId} not found.");

            // Create uploads directory if it doesn't exist
            var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "profile-pictures");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            // Generate unique filename
            var fileName = $"{userId}_{DateTime.UtcNow:yyyyMMddHHmmss}{Path.GetExtension(file.FileName)}";
            var filePath = Path.Combine(uploadsFolder, fileName);

            // Save the file
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Delete old profile picture if it exists
            if (!string.IsNullOrEmpty(user.ProfilePictureUrl))
            {
                var oldFilePath = Path.Combine(_environment.WebRootPath, user.ProfilePictureUrl.TrimStart('/'));
                if (File.Exists(oldFilePath))
                {
                    File.Delete(oldFilePath);
                }
            }

            // Update user's profile picture URL
            user.ProfilePictureUrl = $"/uploads/profile-pictures/{fileName}";
            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                // If update fails, delete the uploaded file
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
                return ServiceResult.Failure($"Failed to update profile picture: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }

            return ServiceResult.Success();
        }
        catch (Exception ex)
        {
            return ServiceResult.Failure($"An error occurred while updating profile picture: {ex.Message}");
        }
    }

    public async Task<ServiceResult> DeleteProfilePictureAsync(string userId)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId) 
                ?? throw new KeyNotFoundException($"User with ID {userId} not found.");

            if (!string.IsNullOrEmpty(user.ProfilePictureUrl))
            {
                var filePath = Path.Combine(_environment.WebRootPath, user.ProfilePictureUrl.TrimStart('/'));
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            }

            user.ProfilePictureUrl = null;
            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                return ServiceResult.Failure($"Failed to delete profile picture: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }

            return ServiceResult.Success();
        }
        catch (Exception ex)
        {
            return ServiceResult.Failure($"An error occurred while deleting profile picture: {ex.Message}");
        }
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