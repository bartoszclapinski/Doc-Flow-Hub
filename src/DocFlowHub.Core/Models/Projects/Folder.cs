using System.ComponentModel.DataAnnotations;
using DocFlowHub.Core.Identity;
using DocFlowHub.Core.Models.Documents;

namespace DocFlowHub.Core.Models.Projects;

public class Folder
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    // Hierarchical relationships
    public int? ParentFolderId { get; set; }
    public virtual Folder? Parent { get; set; }

    public virtual ICollection<Folder> Children { get; set; } = new List<Folder>();

    // Project relationship
    public int ProjectId { get; set; }
    public virtual Project Project { get; set; } = null!;

    // Path for efficient querying (e.g., "/Project1/Folder1/SubFolder2")
    [StringLength(1000)]
    public string Path { get; set; } = string.Empty;

    // Level depth for query optimization (0 = root level)
    public int Level { get; set; } = 0;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    // Status
    public bool IsArchived { get; set; } = false;

    // Navigation properties
    public string CreatedByUserId { get; set; } = string.Empty;
    public virtual ApplicationUser CreatedByUser { get; set; } = null!;

    // Document collection
    public virtual ICollection<Document> Documents { get; set; } = new List<Document>();
} 