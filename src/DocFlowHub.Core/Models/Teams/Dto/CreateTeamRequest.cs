using System.ComponentModel.DataAnnotations;

namespace DocFlowHub.Core.Models.Teams.Dto;

public class CreateTeamRequest
{
    [Required(ErrorMessage = "Team name is required")]
    [StringLength(100, ErrorMessage = "Team name cannot exceed 100 characters")]
    public string Name { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
    public string? Description { get; set; }
} 