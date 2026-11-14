using DocFlowHub.Core.Models.Common;
using DocFlowHub.Core.Models.Workspace;

namespace DocFlowHub.Core.Services.Interfaces;

/// <summary>
/// Read-only queries behind the Projects, project/folder and Teams screens. Projects are owner-only;
/// documents follow the usual rule (owned, or shared with one of the user's teams).
/// </summary>
public interface IWorkspaceBrowserService
{
    /// <summary>The user's projects: active ones, or archived ones.</summary>
    Task<ServiceResult<List<ProjectRow>>> GetProjectsAsync(string userId, bool archived);

    /// <summary>One project with its folders and AI coverage (fails when it is not the user's).</summary>
    Task<ServiceResult<ProjectInspector>> GetProjectAsync(string userId, int projectId);

    /// <summary>The project root (folderId null) or a folder: subfolders and documents directly inside.</summary>
    Task<ServiceResult<FolderView>> GetFolderViewAsync(string userId, int projectId, int? folderId);

    /// <summary>Teams the user owns or belongs to.</summary>
    Task<ServiceResult<List<TeamRow>>> GetTeamsAsync(string userId);

    /// <summary>One team with its members (fails when the user is neither owner nor member).</summary>
    Task<ServiceResult<TeamView>> GetTeamAsync(string userId, int teamId);
}
