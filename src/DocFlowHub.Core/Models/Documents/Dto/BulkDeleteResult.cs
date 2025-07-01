namespace DocFlowHub.Core.Models.Documents.Dto;

public class BulkDeleteResult
{
    public int TotalRequested { get; set; }
    public int SuccessfulDeletions { get; set; }
    public int FailedDeletions { get; set; }
    public List<BulkDeleteItem> Results { get; set; } = new();
    public bool IsFullySuccessful => FailedDeletions == 0;
    public bool HasPartialFailure => SuccessfulDeletions > 0 && FailedDeletions > 0;
}

public class BulkDeleteItem
{
    public int DocumentId { get; set; }
    public string DocumentTitle { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
} 