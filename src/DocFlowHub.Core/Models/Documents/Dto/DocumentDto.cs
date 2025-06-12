using System;
using System.Collections.Generic;

namespace DocFlowHub.Core.Models.Documents.Dto;

public class DocumentDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string OwnerId { get; set; } = string.Empty;
    public string OwnerName { get; set; } = string.Empty;
    public int? TeamId { get; set; }
    public string? TeamName { get; set; }
    public bool IsDeleted { get; set; }
    public int? CurrentVersionId { get; set; }
    public List<DocumentVersionDto> Versions { get; set; } = new();
    public List<DocumentCategoryDto> Categories { get; set; } = new();
} 