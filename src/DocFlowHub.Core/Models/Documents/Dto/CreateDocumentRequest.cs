using Microsoft.AspNetCore.Http;
using System.Collections.Generic;

namespace DocFlowHub.Core.Models.Documents.Dto;

public class CreateDocumentRequest
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public IFormFile File { get; set; } = null!;
    public int? TeamId { get; set; }
    public List<int> CategoryIds { get; set; } = new();
} 