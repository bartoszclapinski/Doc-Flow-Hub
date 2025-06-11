using Microsoft.AspNetCore.Http;

namespace DocFlowHub.Core.Models.Documents.Dto;

public class UpdateDocumentRequest
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public IFormFile? File { get; set; }
    public string? ChangeSummary { get; set; }
    public List<int>? CategoryIds { get; set; }
} 