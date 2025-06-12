using System.Collections.Generic;
using System.Threading.Tasks;
using DocFlowHub.Core.Models.Common;
using DocFlowHub.Core.Models;

namespace DocFlowHub.Core.Services.Interfaces;

public interface ITeamService
{
    Task<ServiceResult<Team>> CreateTeamAsync(string name, string? description, string ownerId);
    Task<ServiceResult<Team>> UpdateTeamAsync(int id, string name, string? description, string userId);
    Task<ServiceResult> DeleteTeamAsync(int id, string userId);
    Task<ServiceResult<Team>> GetTeamByIdAsync(int id);
    Task<ServiceResult<IEnumerable<Team>>> GetUserTeamsAsync(string userId);
    Task<ServiceResult> AddMemberToTeamAsync(int teamId, string userId, string addedByUserId);
    Task<ServiceResult> RemoveMemberFromTeamAsync(int teamId, string userId, string removedByUserId);
    Task<ServiceResult<IEnumerable<TeamMember>>> GetTeamMembersAsync(int teamId);
} 