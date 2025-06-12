using System;
using DocFlowHub.Core.Identity;

namespace DocFlowHub.Core.Models.Documents;

public class DocumentVersion
{
    public int Id { get; set; }
    
    public int DocumentId { get; set; }
    
    public virtual Document Document { get; set; } = null!;
    
    public string FilePath { get; set; } = string.Empty;
    
    public int VersionNumber { get; set; }
    
    public string FileType { get; set; } = string.Empty;
    
    public long FileSize { get; set; }
    
    public DateTime CreatedAt { get; set; }
    
    public string UserId { get; set; } = string.Empty;
    
    public string UserName { get; set; } = string.Empty;
    
    public string ChangeSummary { get; set; } = string.Empty;
    
    public string FileHash { get; set; } = string.Empty;
    
    public string CreatedBy { get; set; } = string.Empty;
    
    public string StoragePath { get; set; } = string.Empty;
} 