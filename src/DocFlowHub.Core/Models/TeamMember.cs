using System.ComponentModel.DataAnnotations;
using DocFlowHub.Core.Identity;

namespace DocFlowHub.Core.Models;

public class TeamMember
{
    public int Id { get; set; }

    public int TeamId { get; set; }
    public virtual Team Team { get; set; } = null!;

    public string UserId { get; set; } = string.Empty;
    public virtual ApplicationUser User { get; set; } = null!;

    [Required]
    public TeamRole Role { get; set; }

    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
}

public enum TeamRole
{
    Member,
    Admin
} 