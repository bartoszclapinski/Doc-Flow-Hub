using System.Collections.Generic;

namespace DocFlowHub.Core.Models.Common;

public class PagedResult<T>
{
    public IEnumerable<T> Items { get; set; } = new List<T>();
    public int TotalItems { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (TotalItems + PageSize - 1) / PageSize;
} 