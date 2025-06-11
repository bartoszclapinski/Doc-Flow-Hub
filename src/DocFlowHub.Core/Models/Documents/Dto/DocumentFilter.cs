namespace DocFlowHub.Core.Models.Documents.Dto;

public class DocumentFilter
{
    public string? SearchTerm { get; set; }
    public string? FileType { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public List<int>? CategoryIds { get; set; }
    public int? TeamId { get; set; }
    public bool IncludeTeamDocuments { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
} 