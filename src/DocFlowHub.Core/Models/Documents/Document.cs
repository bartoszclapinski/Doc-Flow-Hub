using System;
using System.Collections.Generic;
using DocFlowHub.Core.Identity;
using DocFlowHub.Core.Models.Projects;

namespace DocFlowHub.Core.Models.Documents;

public class Document
{
    public int Id { get; set; }
    
    public string Title { get; set; } = string.Empty;
    
    public string Description { get; set; } = string.Empty;
    
    public string FilePath { get; set; } = string.Empty;
    
    public string FileType { get; set; } = string.Empty;
    
    public long FileSize { get; set; }
    
    public string OwnerId { get; set; } = string.Empty;
    
    public virtual ApplicationUser Owner { get; set; } = null!;
    
    public int? TeamId { get; set; }
    
    public virtual Team? Team { get; set; }
    
    // Project and Folder organization (nullable for backward compatibility)
    public int? ProjectId { get; set; }
    
    public virtual Project? Project { get; set; }
    
    public int? FolderId { get; set; }
    
    public virtual Folder? Folder { get; set; }
    
    public virtual ICollection<DocumentVersion> Versions { get; set; } = new List<DocumentVersion>();
    
    public virtual ICollection<DocumentCategory> Categories { get; set; } = new List<DocumentCategory>();
    
    public DateTime CreatedAt { get; set; }
    
    public DateTime? UpdatedAt { get; set; }
    
    public int? CurrentVersionId { get; set; }
    
    public virtual DocumentVersion? CurrentVersion { get; set; }
    
    public bool IsDeleted { get; set; }
} 