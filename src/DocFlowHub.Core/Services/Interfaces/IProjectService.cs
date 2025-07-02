using DocFlowHub.Core.Models.Common;
using DocFlowHub.Core.Models.Documents.Dto;
using DocFlowHub.Core.Models.Projects;
using DocFlowHub.Core.Models.Projects.Dto;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DocFlowHub.Core.Services.Interfaces;

public interface IProjectService
{
    // Basic CRUD operations
    Task<ServiceResult<ProjectDto>> CreateProjectAsync(CreateProjectRequest request, string ownerId);
    Task<ServiceResult<ProjectDto>> GetProjectByIdAsync(int id);
    Task<ServiceResult<ProjectDto>> UpdateProjectAsync(int id, UpdateProjectRequest request, string userId);
    Task<ServiceResult> DeleteProjectAsync(int id, string userId);
    
    // User-specific queries
    Task<ServiceResult<PagedResult<ProjectDto>>> GetUserProjectsAsync(string userId, ProjectFilter filter);
    Task<ServiceResult<IEnumerable<ProjectDto>>> GetActiveProjectsForUserAsync(string userId);
    
    // Access control
    Task<ServiceResult<bool>> CanUserAccessProjectAsync(int projectId, string userId);
    Task<ServiceResult<bool>> CanUserEditProjectAsync(int projectId, string userId);
    
    // Project statistics and management
    Task<ServiceResult<ProjectStatsDto>> GetProjectStatsAsync(int projectId);
    Task<ServiceResult> ArchiveProjectAsync(int projectId, string userId);
    Task<ServiceResult> RestoreProjectAsync(int projectId, string userId);
    
    // Team collaboration (for future implementation)
    Task<ServiceResult> ShareProjectWithTeamAsync(int projectId, int teamId, string userId);
    Task<ServiceResult> UnshareProjectFromTeamAsync(int projectId, int teamId, string userId);
    
    // Project organization
    Task<ServiceResult<IEnumerable<DocumentDto>>> GetProjectDocumentsAsync(int projectId, string userId);
    Task<ServiceResult<IEnumerable<FolderDto>>> GetProjectFoldersAsync(int projectId, string userId);
} 