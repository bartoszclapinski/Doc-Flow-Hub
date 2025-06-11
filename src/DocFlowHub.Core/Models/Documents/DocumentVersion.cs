using System;
using DocFlowHub.Core.Identity;

namespace DocFlowHub.Core.Models.Documents;

public class DocumentVersion
{
    public int Id { get; set; }
    
    public int? DocumentId { get; set; }
    
    public virtual Document? Document { get; set; }
    
    public string FilePath { get; set; } = string.Empty;
    
    public int VersionNumber { get; set; }
    
    public string? ChangeSummary { get; set; }
    
    public string UserId { get; set; } = string.Empty;
    
    public virtual ApplicationUser User { get; set; } = null!;
    
    public DateTime CreatedAt { get; set; }
    
    public string FileHash { get; set; } = string.Empty;
    
    public long FileSize { get; set; }
} 