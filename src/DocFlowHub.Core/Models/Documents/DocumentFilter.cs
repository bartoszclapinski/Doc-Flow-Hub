namespace DocFlowHub.Core.Models.Documents;

public class DocumentFilter
{
    public string? SearchTerm { get; set; }
    public string? FileType { get; set; }
    public int? CategoryId { get; set; }
    public bool IncludeTeamDocuments { get; set; }
    public string? OwnerId { get; set; }
    public int? TeamId { get; set; }
    public bool IncludeDeleted { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
} 