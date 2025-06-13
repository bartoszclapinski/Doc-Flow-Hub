namespace DocFlowHub.Core.Models.Teams;

public class TeamFilter
{
    public string? SearchTerm { get; set; }
    public TeamRole? Role { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
} 