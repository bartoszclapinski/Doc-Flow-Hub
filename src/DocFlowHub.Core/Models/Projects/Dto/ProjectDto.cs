using System;
using System.Collections.Generic;

namespace DocFlowHub.Core.Models.Projects.Dto;

public class ProjectDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Color { get; set; }
    public string? Icon { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string OwnerId { get; set; } = string.Empty;
    public string OwnerName { get; set; } = string.Empty;
    
    // Statistics (optional, for efficiency)
    public int DocumentCount { get; set; }
    public int FolderCount { get; set; }
    public DateTime? LastActivity { get; set; }
} 