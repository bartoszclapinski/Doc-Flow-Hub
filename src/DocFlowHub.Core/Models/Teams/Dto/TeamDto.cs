namespace DocFlowHub.Core.Models.Teams.Dto;

public class TeamDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string OwnerId { get; set; } = string.Empty;
    public string? OwnerName { get; set; }
    public int MemberCount { get; set; }
} 