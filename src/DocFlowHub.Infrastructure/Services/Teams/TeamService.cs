using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DocFlowHub.Core.Models;
using DocFlowHub.Core.Models.Common;
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

    public async Task<ServiceResult<Team>> CreateTeamAsync(string name, string? description, string ownerId)
    {
        try
        {
            var team = new Team
            {
                Name = name,
                Description = description,
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

            return ServiceResult<Team>.Success(team);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating team");
            return ServiceResult<Team>.Failure("Error creating team: " + ex.Message);
        }
    }

    public async Task<ServiceResult<Team>> UpdateTeamAsync(int id, string name, string? description, string userId)
    {
        try
        {
            var team = await _context.Teams
                .Include(t => t.Members)
                .FirstOrDefaultAsync(t => t.Id == id && t.OwnerId == userId);

            if (team == null)
                return ServiceResult<Team>.Failure("Team not found or you don't have permission to update it");

            team.Name = name;
            team.Description = description;
            team.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return ServiceResult<Team>.Success(team);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating team {TeamId}", id);
            return ServiceResult<Team>.Failure("Error updating team: " + ex.Message);
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

    public async Task<ServiceResult<Team>> GetTeamByIdAsync(int id)
    {
        try
        {
            var team = await _context.Teams
                .Include(t => t.Members)
                .ThenInclude(m => m.User)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (team == null)
                return ServiceResult<Team>.Failure("Team not found");

            return ServiceResult<Team>.Success(team);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting team {TeamId}", id);
            return ServiceResult<Team>.Failure("Error getting team: " + ex.Message);
        }
    }

    public async Task<ServiceResult<IEnumerable<Team>>> GetUserTeamsAsync(string userId)
    {
        try
        {
            var teams = await _context.Teams
                .Include(t => t.Members)
                .Where(t => t.Members.Any(m => m.UserId == userId))
                .ToListAsync();

            return ServiceResult<IEnumerable<Team>>.Success(teams);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting teams for user {UserId}", userId);
            return ServiceResult<IEnumerable<Team>>.Failure("Error getting user teams: " + ex.Message);
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

    public async Task<ServiceResult<IEnumerable<TeamMember>>> GetTeamMembersAsync(int teamId)
    {
        try
        {
            var team = await _context.Teams
                .Include(t => t.Members)
                .ThenInclude(m => m.User)
                .FirstOrDefaultAsync(t => t.Id == teamId);

            if (team == null)
                return ServiceResult<IEnumerable<TeamMember>>.Failure("Team not found");

            return ServiceResult<IEnumerable<TeamMember>>.Success(team.Members);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting members for team {TeamId}", teamId);
            return ServiceResult<IEnumerable<TeamMember>>.Failure("Error getting team members: " + ex.Message);
        }
    }
} 