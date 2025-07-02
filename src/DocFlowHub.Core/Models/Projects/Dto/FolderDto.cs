using System;
using System.Collections.Generic;

namespace DocFlowHub.Core.Models.Projects.Dto;

public class FolderDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    
    // Hierarchical properties
    public int? ParentFolderId { get; set; }
    public string? ParentFolderName { get; set; }
    public string Path { get; set; } = string.Empty;
    public int Level { get; set; }
    
    // Project relationship
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    
    // Audit information
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string CreatedByUserId { get; set; } = string.Empty;
    public string CreatedByUserName { get; set; } = string.Empty;
    
    // Statistics (optional, for efficiency)
    public int DocumentCount { get; set; }
    public int SubfolderCount { get; set; }
    public DateTime? LastActivity { get; set; }
    
    // Navigation collections (optional)
    public List<FolderDto> Children { get; set; } = new();
} 