using System.ComponentModel.DataAnnotations;

namespace DocFlowHub.Core.Models.Projects.Dto;

public class CreateFolderRequest
{
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [StringLength(500)]
    public string? Description { get; set; }
    
    public int ProjectId { get; set; }
    
    public int? ParentFolderId { get; set; }
} 