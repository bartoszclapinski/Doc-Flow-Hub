using DocFlowHub.Core.Models.Profile;
using System.Threading.Tasks;

namespace DocFlowHub.Core.Services.Interfaces;

public interface IProfileService
{
    Task<ProfileDto> GetProfileAsync(string userId);
    Task<ProfileDto> UpdateProfileAsync(string userId, UpdateProfileRequest request);
    Task<bool> UpdateProfilePictureAsync(string userId, string imageUrl);
    Task<bool> DeleteProfilePictureAsync(string userId);
} 