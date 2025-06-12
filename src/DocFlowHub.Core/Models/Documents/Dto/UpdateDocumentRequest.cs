using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DocFlowHub.Core.Models.Documents.Dto;

public class UpdateDocumentRequest
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Title { get; set; } = string.Empty;

    [StringLength(500)]
    public string Description { get; set; } = string.Empty;

    public IFormFile? File { get; set; }

    public List<int>? CategoryIds { get; set; }

    public string UserId { get; set; } = string.Empty;
} 