using System.ComponentModel.DataAnnotations;

namespace DocFlowHub.Core.Models.Profile;

public class UpdateProfileRequest
{
    [Required]
    [StringLength(50)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string LastName { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Bio { get; set; }
} 