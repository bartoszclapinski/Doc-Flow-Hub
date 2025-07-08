namespace DocFlowHub.Core.Models.Documents.Dto;

public class BulkMoveRequest
{
    public List<int> DocumentIds { get; set; } = new();
    public int? ProjectId { get; set; }
    public int? FolderId { get; set; }
} 