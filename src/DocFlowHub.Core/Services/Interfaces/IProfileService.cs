using DocFlowHub.Core.Models.Common;
using DocFlowHub.Core.Models.Profile;
using Microsoft.AspNetCore.Http;

namespace DocFlowHub.Core.Services.Interfaces;

public interface IProfileService
{
    Task<ProfileDto> GetProfileAsync(string userId);
    Task<ProfileDto> UpdateProfileAsync(string userId, UpdateProfileRequest request);
    Task<ServiceResult> UpdateProfilePictureAsync(string userId, IFormFile file);
    Task<ServiceResult> DeleteProfilePictureAsync(string userId);
} 