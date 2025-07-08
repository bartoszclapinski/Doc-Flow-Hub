namespace DocFlowHub.Core.Models.Projects.Dto;

public class FolderFilter
{
    public string? SearchTerm { get; set; }
    public string? Status { get; set; } // "Active", "Archived", or null for all
    public int? ProjectId { get; set; }
    public int? ParentFolderId { get; set; }
    public int? Level { get; set; }
    public string? CreatedByUserId { get; set; }
    public DateTime? CreatedAfter { get; set; }
    public DateTime? CreatedBefore { get; set; }
    
    // Pagination properties
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    
    // Sorting properties
    public string SortBy { get; set; } = "Name"; // "Name", "CreatedAt", "UpdatedAt", "Level"
    public bool SortDescending { get; set; } = false;
} 