using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DocFlowHub.Core.Models;
using DocFlowHub.Core.Models.Common;
using DocFlowHub.Core.Models.Teams;
using DocFlowHub.Core.Models.Teams.Dto;
using DocFlowHub.Core.Models.Teams.Extensions;
using DocFlowHub.Core.Services.Interfaces;
using DocFlowHub.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DocFlowHub.Infrastructure.Services.Teams;

public class TeamService : ITeamService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<TeamService> _logger;

    public TeamService(
        ApplicationDbContext context,
        ILogger<TeamService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ServiceResult<TeamDto>> CreateTeamAsync(CreateTeamRequest request, string ownerId)
    {
        try
        {
            // Validation
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return ServiceResult<TeamDto>.Failure("Team name is required");
            }

            var team = new Team
            {
                Name = request.Name,
                Description = request.Description,
                OwnerId = ownerId,
                CreatedAt = DateTime.UtcNow
            };

            var member = new TeamMember
            {
                Team = team,
                UserId = ownerId,
                Role = TeamRole.Admin,
                JoinedAt = DateTime.UtcNow
            };

            team.Members.Add(member);
            _context.Teams.Add(team);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Team {TeamName} created successfully for user {UserId}", request.Name, ownerId);

            return ServiceResult<TeamDto>.Success(team.ToDto());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating team {TeamName} for user {UserId}", request.Name, ownerId);
            return ServiceResult<TeamDto>.Failure("An error occurred while creating the team");
        }
    }

    public async Task<ServiceResult<TeamDto>> UpdateTeamAsync(int id, string name, string? description, string userId)
    {
        try
        {
            var team = await _context.Teams
                .Include(t => t.Members)
                .Include(t => t.Owner)
                .FirstOrDefaultAsync(t => t.Id == id && t.OwnerId == userId);

            if (team == null)
                return ServiceResult<TeamDto>.Failure("Team not found or you don't have permission to update it");

            team.Name = name;
            team.Description = description;
            team.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return ServiceResult<TeamDto>.Success(team.ToDto());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating team {TeamId}", id);
            return ServiceResult<TeamDto>.Failure("An error occurred while updating the team");
        }
    }

    public async Task<ServiceResult> DeleteTeamAsync(int id, string userId)
    {
        try
        {
            var team = await _context.Teams
                .FirstOrDefaultAsync(t => t.Id == id && t.OwnerId == userId);

            if (team == null)
                return ServiceResult.Failure("Team not found or you don't have permission to delete it");

            _context.Teams.Remove(team);
            await _context.SaveChangesAsync();

            return ServiceResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting team {TeamId}", id);
            return ServiceResult.Failure("Error deleting team: " + ex.Message);
        }
    }

    public async Task<ServiceResult<TeamDto>> GetTeamByIdAsync(int id)
    {
        try
        {
            var team = await _context.Teams
                .Include(t => t.Members)
                .ThenInclude(m => m.User)
                .Include(t => t.Owner)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (team == null)
                return ServiceResult<TeamDto>.Failure("Team not found");

            return ServiceResult<TeamDto>.Success(team.ToDto());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting team {TeamId}", id);
            return ServiceResult<TeamDto>.Failure("An error occurred while getting the team");
        }
    }

    public async Task<ServiceResult<PagedResult<TeamDto>>> GetUserTeamsAsync(string userId, TeamFilter filter)
    {
        try
        {
            var query = _context.Teams
                .Include(t => t.Members)
                .Include(t => t.Owner)
                .Where(t => t.Members.Any(m => m.UserId == userId));

            if (!string.IsNullOrEmpty(filter.SearchTerm))
            {
                query = query.Where(t => t.Name.Contains(filter.SearchTerm) || 
                                   (t.Description != null && t.Description.Contains(filter.SearchTerm)));
            }

            var totalItems = await query.CountAsync();
            var teams = await query
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            var result = new PagedResult<TeamDto>
            {
                Items = teams.ToDto(),
                TotalItems = totalItems,
                PageNumber = filter.PageNumber,
                PageSize = filter.PageSize
            };

            return ServiceResult<PagedResult<TeamDto>>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting teams for user {UserId}", userId);
            return ServiceResult<PagedResult<TeamDto>>.Failure("An error occurred while getting user teams");
        }
    }

    public async Task<ServiceResult> AddMemberToTeamAsync(int teamId, string userId, string addedByUserId)
    {
        try
        {
            var team = await _context.Teams
                .Include(t => t.Members)
                .FirstOrDefaultAsync(t => t.Id == teamId);

            if (team == null)
                return ServiceResult.Failure("Team not found");

            var addedByMember = team.Members.FirstOrDefault(m => m.UserId == addedByUserId);
            if (addedByMember == null || addedByMember.Role != TeamRole.Admin)
                return ServiceResult.Failure("You don't have permission to add members to this team");

            if (team.Members.Any(m => m.UserId == userId))
                return ServiceResult.Failure("User is already a member of this team");

            var member = new TeamMember
            {
                TeamId = teamId,
                UserId = userId,
                Role = TeamRole.Member,
                JoinedAt = DateTime.UtcNow
            };

            team.Members.Add(member);
            await _context.SaveChangesAsync();

            return ServiceResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding member {UserId} to team {TeamId}", userId, teamId);
            return ServiceResult.Failure("Error adding member to team: " + ex.Message);
        }
    }

    public async Task<ServiceResult> RemoveMemberFromTeamAsync(int teamId, string userId, string removedByUserId)
    {
        try
        {
            var team = await _context.Teams
                .Include(t => t.Members)
                .FirstOrDefaultAsync(t => t.Id == teamId);

            if (team == null)
                return ServiceResult.Failure("Team not found");

            var removedByMember = team.Members.FirstOrDefault(m => m.UserId == removedByUserId);
            if (removedByMember == null || removedByMember.Role != TeamRole.Admin)
                return ServiceResult.Failure("You don't have permission to remove members from this team");

            var member = team.Members.FirstOrDefault(m => m.UserId == userId);
            if (member == null)
                return ServiceResult.Failure("User is not a member of this team");

            if (member.Role == TeamRole.Admin && team.Members.Count(m => m.Role == TeamRole.Admin) == 1)
                return ServiceResult.Failure("Cannot remove the last admin from the team");

            team.Members.Remove(member);
            await _context.SaveChangesAsync();

            return ServiceResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing member {UserId} from team {TeamId}", userId, teamId);
            return ServiceResult.Failure("Error removing member from team: " + ex.Message);
        }
    }

    public async Task<ServiceResult> UpdateMemberRoleAsync(int teamId, string userId, TeamRole newRole, string updatedByUserId)
    {
        try
        {
            var team = await _context.Teams
                .Include(t => t.Members)
                .FirstOrDefaultAsync(t => t.Id == teamId);

            if (team == null)
                return ServiceResult.Failure("Team not found");

            var updatedByMember = team.Members.FirstOrDefault(m => m.UserId == updatedByUserId);
            if (updatedByMember == null || updatedByMember.Role != TeamRole.Admin)
                return ServiceResult.Failure("You don't have permission to update member roles in this team");

            var member = team.Members.FirstOrDefault(m => m.UserId == userId);
            if (member == null)
                return ServiceResult.Failure("User is not a member of this team");

            // Prevent changing team owner's role
            if (team.OwnerId == userId)
                return ServiceResult.Failure("Cannot change the team owner's role");

            // If demoting the last admin (other than owner), prevent it
            if (member.Role == TeamRole.Admin && newRole == TeamRole.Member)
            {
                var adminCount = team.Members.Count(m => m.Role == TeamRole.Admin);
                if (adminCount <= 1)
                    return ServiceResult.Failure("Cannot demote the last admin from the team");
            }

            member.Role = newRole;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Member {UserId} role updated to {Role} in team {TeamId} by {UpdatedBy}", 
                userId, newRole, teamId, updatedByUserId);

            return ServiceResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating member {UserId} role in team {TeamId}", userId, teamId);
            return ServiceResult.Failure("Error updating member role: " + ex.Message);
        }
    }

    public async Task<ServiceResult<PagedResult<TeamMemberDto>>> GetTeamMembersAsync(int teamId)
    {
        try
        {
            var team = await _context.Teams
                .Include(t => t.Members)
                .ThenInclude(m => m.User)
                .FirstOrDefaultAsync(t => t.Id == teamId);

            if (team == null)
                return ServiceResult<PagedResult<TeamMemberDto>>.Failure("Team not found");

            var totalItems = team.Members.Count;
            var result = new PagedResult<TeamMemberDto>
            {
                Items = team.Members.ToDto(),
                TotalItems = totalItems,
                PageNumber = 1,
                PageSize = totalItems // Return all members for now
            };

            return ServiceResult<PagedResult<TeamMemberDto>>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting members for team {TeamId}", teamId);
            return ServiceResult<PagedResult<TeamMemberDto>>.Failure("An error occurred while getting team members");
        }
    }
} 