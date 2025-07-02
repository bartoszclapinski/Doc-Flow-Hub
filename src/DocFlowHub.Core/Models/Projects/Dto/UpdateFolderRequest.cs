using System.ComponentModel.DataAnnotations;

namespace DocFlowHub.Core.Models.Projects.Dto;

public class UpdateFolderRequest
{
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [StringLength(500)]
    public string? Description { get; set; }
} 