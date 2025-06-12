using System;
using System.Collections.Generic;

namespace DocFlowHub.Core.Models.Documents;

public class DocumentCategory
{
    public int Id { get; set; }
    
    public string Name { get; set; } = string.Empty;
    
    public string? Description { get; set; }
    
    public int? ParentId { get; set; }
    
    public virtual DocumentCategory? Parent { get; set; }
    
    public virtual ICollection<DocumentCategory> Children { get; set; } = new List<DocumentCategory>();
    
    public virtual ICollection<Document> Documents { get; set; } = new List<Document>();
    
    public DateTime CreatedAt { get; set; }
    
    public DateTime? UpdatedAt { get; set; }
    
    public bool IsActive { get; set; } = true;
} 