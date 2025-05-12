using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using DocFlowHub.Core.Models;

namespace DocFlowHub.Core.Identity;

public class ApplicationUser : IdentityUser
{
    [Required]
    [StringLength(50)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string LastName { get; set; } = string.Empty;

    [StringLength(200)]
    public string? ProfilePictureUrl { get; set; }

    [StringLength(500)]
    public string? Bio { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? LastLoginAt { get; set; }
    
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public virtual ICollection<TeamMember> TeamMemberships { get; set; } = new List<TeamMember>();
    
    public virtual ICollection<Team> OwnedTeams { get; set; } = new List<Team>();
} 