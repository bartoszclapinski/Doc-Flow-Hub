namespace DocFlowHub.Core.Models.Teams.Dto;

public class TeamMemberDto
{
    public int Id { get; set; }
    public int TeamId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string? UserName { get; set; }
    public string? UserEmail { get; set; }
    public TeamRole Role { get; set; }
    public DateTime JoinedAt { get; set; }
} 