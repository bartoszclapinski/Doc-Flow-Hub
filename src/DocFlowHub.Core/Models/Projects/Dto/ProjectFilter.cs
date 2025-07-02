namespace DocFlowHub.Core.Models.Projects.Dto;

public class ProjectFilter
{
    public string? SearchTerm { get; set; }
    public bool? IsActive { get; set; }
    public string? OwnerId { get; set; }
    public DateTime? CreatedAfter { get; set; }
    public DateTime? CreatedBefore { get; set; }
    
    // Pagination properties
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    
    // Sorting properties
    public string SortBy { get; set; } = "CreatedAt";
    public bool SortDescending { get; set; } = true;
} 