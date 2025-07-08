using System.ComponentModel.DataAnnotations;

namespace DocFlowHub.Core.Models.Projects.Dto;

public class UpdateFolderRequest
{
    public int Id { get; set; }
    
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [StringLength(500)]
    public string? Description { get; set; }
    
    public int? ParentFolderId { get; set; }
} 