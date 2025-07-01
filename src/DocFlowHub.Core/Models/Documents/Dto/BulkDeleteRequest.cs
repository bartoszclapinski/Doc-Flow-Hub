using System.ComponentModel.DataAnnotations;

namespace DocFlowHub.Core.Models.Documents.Dto;

public class BulkDeleteRequest
{
    [Required]
    [MinLength(1, ErrorMessage = "At least one document must be selected for deletion.")]
    [MaxLength(100, ErrorMessage = "Cannot delete more than 100 documents at once.")]
    public List<int> DocumentIds { get; set; } = new();
} 