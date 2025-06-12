using DocFlowHub.Core.Models.Documents.Dto;
using System.Collections.Generic;
using System.Linq;

namespace DocFlowHub.Core.Models.Documents.Extensions;

public static class DocumentExtensions
{
    public static DocumentDto ToDto(this Document document)
    {
        return new DocumentDto
        {
            Id = document.Id,
            Title = document.Title,
            Description = document.Description,
            FileType = document.FileType,
            FileSize = document.FileSize,
            CreatedAt = document.CreatedAt,
            UpdatedAt = document.UpdatedAt,
            OwnerId = document.OwnerId,
            OwnerName = document.Owner?.UserName ?? string.Empty,
            TeamId = document.TeamId,
            TeamName = document.Team?.Name,
            IsDeleted = document.IsDeleted,
            CurrentVersionId = document.Versions.OrderByDescending(v => v.VersionNumber).FirstOrDefault()?.Id,
            Versions = document.Versions.Select(v => v.ToDto()).ToList(),
            Categories = document.Categories.Select(c => c.ToDto()).ToList()
        };
    }

    public static DocumentVersionDto ToDto(this DocumentVersion version)
    {
        return new DocumentVersionDto
        {
            Id = version.Id,
            DocumentId = version.DocumentId,
            VersionNumber = version.VersionNumber,
            FilePath = version.FilePath,
            FileType = version.FileType,
            FileSize = version.FileSize,
            CreatedAt = version.CreatedAt,
            UserId = version.UserId,
            UserName = version.UserName,
            ChangeSummary = version.ChangeSummary
        };
    }

    public static DocumentCategoryDto ToDto(this DocumentCategory category)
    {
        return new DocumentCategoryDto
        {
            Id = category.Id,
            Name = category.Name,
            ParentId = category.ParentId
        };
    }
} 