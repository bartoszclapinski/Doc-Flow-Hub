using System.ComponentModel.DataAnnotations;

namespace DocFlowHub.Core.Models.Role;

public class RoleDto
{
    public string Id { get; set; } = string.Empty;
    
    [Required]
    [StringLength(50)]
    public string Name { get; set; } = string.Empty;
    
    [StringLength(200)]
    public string? Description { get; set; }
    
    public int UsersCount { get; set; }
    
    public DateTime CreatedAt { get; set; }
    
    public DateTime? UpdatedAt { get; set; }
} 