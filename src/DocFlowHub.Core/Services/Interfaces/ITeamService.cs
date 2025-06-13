using System.Collections.Generic;
using System.Threading.Tasks;
using DocFlowHub.Core.Models.Common;
using DocFlowHub.Core.Models;
using DocFlowHub.Core.Models.Teams;
using DocFlowHub.Core.Models.Teams.Dto;

namespace DocFlowHub.Core.Services.Interfaces;

public interface ITeamService
{
    Task<ServiceResult<TeamDto>> CreateTeamAsync(CreateTeamRequest request, string ownerId);
    Task<ServiceResult<TeamDto>> UpdateTeamAsync(int id, string name, string? description, string userId);
    Task<ServiceResult> DeleteTeamAsync(int id, string userId);
    Task<ServiceResult<TeamDto>> GetTeamByIdAsync(int id);
    Task<ServiceResult<PagedResult<TeamDto>>> GetUserTeamsAsync(string userId, TeamFilter filter);
    Task<ServiceResult> AddMemberToTeamAsync(int teamId, string userId, string addedByUserId);
    Task<ServiceResult> RemoveMemberFromTeamAsync(int teamId, string userId, string removedByUserId);
    Task<ServiceResult<PagedResult<TeamMemberDto>>> GetTeamMembersAsync(int teamId);
} 