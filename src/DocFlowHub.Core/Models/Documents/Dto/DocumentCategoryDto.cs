using System.Collections.Generic;

namespace DocFlowHub.Core.Models.Documents.Dto;

public class DocumentCategoryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int? ParentId { get; set; }
    public List<DocumentCategoryDto> Children { get; set; } = new();
} 