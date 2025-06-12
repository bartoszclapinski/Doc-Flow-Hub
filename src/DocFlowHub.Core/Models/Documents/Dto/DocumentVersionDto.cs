using System;

namespace DocFlowHub.Core.Models.Documents.Dto;

public class DocumentVersionDto
{
    public int Id { get; set; }
    public int DocumentId { get; set; }
    public int VersionNumber { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public DateTime CreatedAt { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string ChangeSummary { get; set; } = string.Empty;
} 