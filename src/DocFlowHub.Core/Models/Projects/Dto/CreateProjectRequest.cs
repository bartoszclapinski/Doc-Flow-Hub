using System.ComponentModel.DataAnnotations;

namespace DocFlowHub.Core.Models.Projects.Dto;

public class CreateProjectRequest
{
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [StringLength(500)]
    public string? Description { get; set; }
    
    [StringLength(7)]
    public string? Color { get; set; }
    
    [StringLength(50)]
    public string? Icon { get; set; }
} 