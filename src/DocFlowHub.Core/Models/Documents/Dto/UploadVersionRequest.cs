using Microsoft.AspNetCore.Http;

namespace DocFlowHub.Core.Models.Documents.Dto;

public class UploadVersionRequest
{
    public int DocumentId { get; set; }
    public IFormFile File { get; set; } = null!;
    public string? ChangeSummary { get; set; }
} 