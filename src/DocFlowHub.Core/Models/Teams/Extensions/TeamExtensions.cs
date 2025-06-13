using DocFlowHub.Core.Models.Teams.Dto;
using System.Collections.Generic;
using System.Linq;

namespace DocFlowHub.Core.Models.Teams.Extensions;

public static class TeamExtensions
{
    public static TeamDto ToDto(this Team team)
    {
        return new TeamDto
        {
            Id = team.Id,
            Name = team.Name,
            Description = team.Description,
            CreatedAt = team.CreatedAt,
            UpdatedAt = team.UpdatedAt,
            OwnerId = team.OwnerId,
            OwnerName = team.Owner?.UserName ?? string.Empty,
            MemberCount = team.Members?.Count ?? 0
        };
    }

    public static TeamMemberDto ToDto(this TeamMember member)
    {
        return new TeamMemberDto
        {
            Id = member.Id,
            TeamId = member.TeamId,
            UserId = member.UserId,
            UserName = member.User?.UserName ?? string.Empty,
            UserEmail = member.User?.Email ?? string.Empty,
            Role = member.Role,
            JoinedAt = member.JoinedAt
        };
    }

    public static IEnumerable<TeamDto> ToDto(this IEnumerable<Team> teams)
    {
        return teams.Select(team => team.ToDto());
    }

    public static IEnumerable<TeamMemberDto> ToDto(this IEnumerable<TeamMember> members)
    {
        return members.Select(member => member.ToDto());
    }
} 