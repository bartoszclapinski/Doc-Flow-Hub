namespace DocFlowHub.Core.Models.Documents.Dto;

public class MoveDocumentRequest
{
    public int DocumentId { get; set; }
    public int? ProjectId { get; set; }
    public int? FolderId { get; set; }
    public string UserId { get; set; } = string.Empty;
} 