using System.ComponentModel.DataAnnotations;
using DocFlowHub.Core.Identity;

namespace DocFlowHub.Core.Models;

public class Team
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public string OwnerId { get; set; } = string.Empty;
    public virtual ApplicationUser Owner { get; set; } = null!;

    public virtual ICollection<TeamMember> Members { get; set; } = new List<TeamMember>();
} 