using System.ComponentModel.DataAnnotations;
using DocFlowHub.Core.Identity;
using DocFlowHub.Core.Models.Documents;

namespace DocFlowHub.Core.Models.Projects;

public class Project
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    [StringLength(7)] // For hex color codes like #FF5733
    public string? Color { get; set; }

    [StringLength(50)] // For icon names/classes
    public string? Icon { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public string OwnerId { get; set; } = string.Empty;
    public virtual ApplicationUser Owner { get; set; } = null!;

    // Collections - will be populated when we add Folder and Document relationships
    public virtual ICollection<Folder> Folders { get; set; } = new List<Folder>();
    public virtual ICollection<Document> Documents { get; set; } = new List<Document>();
} 