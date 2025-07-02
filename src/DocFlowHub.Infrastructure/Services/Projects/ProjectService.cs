using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DocFlowHub.Core.Models.Common;
using DocFlowHub.Core.Models.Documents.Dto;
using DocFlowHub.Core.Models.Projects;
using DocFlowHub.Core.Models.Projects.Dto;
using DocFlowHub.Core.Services.Interfaces;
using DocFlowHub.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DocFlowHub.Infrastructure.Services.Projects;

public class ProjectService : IProjectService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ProjectService> _logger;

    public ProjectService(
        ApplicationDbContext context,
        ILogger<ProjectService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ServiceResult<ProjectDto>> CreateProjectAsync(CreateProjectRequest request, string ownerId)
    {
        try
        {
            // Validation
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return ServiceResult<ProjectDto>.Failure("Project name is required");
            }

            var project = new Project
            {
                Name = request.Name,
                Description = request.Description,
                Color = request.Color,
                Icon = request.Icon,
                OwnerId = ownerId,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            _context.Projects.Add(project);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Project {ProjectName} created successfully for user {UserId}", request.Name, ownerId);

            return ServiceResult<ProjectDto>.Success(await ConvertToProjectDtoAsync(project));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating project {ProjectName} for user {UserId}", request.Name, ownerId);
            return ServiceResult<ProjectDto>.Failure("An error occurred while creating the project");
        }
    }

    public async Task<ServiceResult<ProjectDto>> GetProjectByIdAsync(int id)
    {
        try
        {
            var project = await _context.Projects
                .Include(p => p.Owner)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (project == null)
                return ServiceResult<ProjectDto>.Failure("Project not found");

            return ServiceResult<ProjectDto>.Success(await ConvertToProjectDtoAsync(project));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting project {ProjectId}", id);
            return ServiceResult<ProjectDto>.Failure("An error occurred while getting the project");
        }
    }

    public async Task<ServiceResult<ProjectDto>> UpdateProjectAsync(int id, UpdateProjectRequest request, string userId)
    {
        try
        {
            var project = await _context.Projects
                .Include(p => p.Owner)
                .FirstOrDefaultAsync(p => p.Id == id && p.OwnerId == userId);

            if (project == null)
                return ServiceResult<ProjectDto>.Failure("Project not found or you don't have permission to update it");

            project.Name = request.Name;
            project.Description = request.Description;
            project.Color = request.Color;
            project.Icon = request.Icon;
            project.IsActive = request.IsActive;
            project.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Project {ProjectId} updated successfully by user {UserId}", id, userId);

            return ServiceResult<ProjectDto>.Success(await ConvertToProjectDtoAsync(project));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating project {ProjectId}", id);
            return ServiceResult<ProjectDto>.Failure("An error occurred while updating the project");
        }
    }

    public async Task<ServiceResult> DeleteProjectAsync(int id, string userId)
    {
        try
        {
            var project = await _context.Projects
                .FirstOrDefaultAsync(p => p.Id == id && p.OwnerId == userId);

            if (project == null)
                return ServiceResult.Failure("Project not found or you don't have permission to delete it");

            // Check if project has documents or folders
            var hasDocuments = await _context.Documents.AnyAsync(d => d.ProjectId == id);
            var hasFolders = await _context.Folders.AnyAsync(f => f.ProjectId == id);

            if (hasDocuments || hasFolders)
            {
                return ServiceResult.Failure("Cannot delete project that contains documents or folders. Please move or delete all contents first.");
            }

            _context.Projects.Remove(project);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Project {ProjectId} deleted successfully by user {UserId}", id, userId);

            return ServiceResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting project {ProjectId}", id);
            return ServiceResult.Failure("An error occurred while deleting the project");
        }
    }

    public async Task<ServiceResult<PagedResult<ProjectDto>>> GetUserProjectsAsync(string userId, ProjectFilter filter)
    {
        try
        {
            var query = _context.Projects
                .Include(p => p.Owner)
                .Where(p => p.OwnerId == userId);

            // Apply filters
            if (!string.IsNullOrEmpty(filter.SearchTerm))
            {
                query = query.Where(p => p.Name.Contains(filter.SearchTerm) || 
                                   (p.Description != null && p.Description.Contains(filter.SearchTerm)));
            }

            if (filter.IsActive.HasValue)
            {
                query = query.Where(p => p.IsActive == filter.IsActive.Value);
            }

            if (filter.CreatedAfter.HasValue)
            {
                query = query.Where(p => p.CreatedAt >= filter.CreatedAfter.Value);
            }

            if (filter.CreatedBefore.HasValue)
            {
                query = query.Where(p => p.CreatedAt <= filter.CreatedBefore.Value);
            }

            // Apply sorting
            query = filter.SortBy.ToLower() switch
            {
                "name" => filter.SortDescending ? query.OrderByDescending(p => p.Name) : query.OrderBy(p => p.Name),
                "createdat" => filter.SortDescending ? query.OrderByDescending(p => p.CreatedAt) : query.OrderBy(p => p.CreatedAt),
                "updatedat" => filter.SortDescending ? query.OrderByDescending(p => p.UpdatedAt) : query.OrderBy(p => p.UpdatedAt),
                _ => filter.SortDescending ? query.OrderByDescending(p => p.CreatedAt) : query.OrderBy(p => p.CreatedAt)
            };

            var totalItems = await query.CountAsync();
            var projects = await query
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            var projectDtos = new List<ProjectDto>();
            foreach (var project in projects)
            {
                projectDtos.Add(await ConvertToProjectDtoAsync(project));
            }

            var result = new PagedResult<ProjectDto>
            {
                Items = projectDtos,
                TotalItems = totalItems,
                PageNumber = filter.PageNumber,
                PageSize = filter.PageSize
            };

            return ServiceResult<PagedResult<ProjectDto>>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting projects for user {UserId}", userId);
            return ServiceResult<PagedResult<ProjectDto>>.Failure("An error occurred while getting user projects");
        }
    }

    public async Task<ServiceResult<IEnumerable<ProjectDto>>> GetActiveProjectsForUserAsync(string userId)
    {
        try
        {
            var projects = await _context.Projects
                .Include(p => p.Owner)
                .Where(p => p.OwnerId == userId && p.IsActive)
                .OrderBy(p => p.Name)
                .ToListAsync();

            var projectDtos = new List<ProjectDto>();
            foreach (var project in projects)
            {
                projectDtos.Add(await ConvertToProjectDtoAsync(project));
            }

            return ServiceResult<IEnumerable<ProjectDto>>.Success(projectDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active projects for user {UserId}", userId);
            return ServiceResult<IEnumerable<ProjectDto>>.Failure("An error occurred while getting active projects");
        }
    }

    public async Task<ServiceResult<bool>> CanUserAccessProjectAsync(int projectId, string userId)
    {
        try
        {
            var hasAccess = await _context.Projects
                .AnyAsync(p => p.Id == projectId && p.OwnerId == userId);

            return ServiceResult<bool>.Success(hasAccess);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking project access for user {UserId} on project {ProjectId}", userId, projectId);
            return ServiceResult<bool>.Failure("An error occurred while checking project access");
        }
    }

    public async Task<ServiceResult<bool>> CanUserEditProjectAsync(int projectId, string userId)
    {
        // For now, only owners can edit projects (same as access)
        return await CanUserAccessProjectAsync(projectId, userId);
    }

    public async Task<ServiceResult<ProjectStatsDto>> GetProjectStatsAsync(int projectId)
    {
        try
        {
            var project = await _context.Projects
                .FirstOrDefaultAsync(p => p.Id == projectId);

            if (project == null)
                return ServiceResult<ProjectStatsDto>.Failure("Project not found");

            // Get document statistics
            var documentStats = await _context.Documents
                .Where(d => d.ProjectId == projectId)
                .GroupBy(d => d.IsDeleted)
                .Select(g => new { IsDeleted = g.Key, Count = g.Count(), TotalSize = g.Sum(d => d.FileSize) })
                .ToListAsync();

            var activeDocuments = documentStats.FirstOrDefault(s => !s.IsDeleted);
            var deletedDocuments = documentStats.FirstOrDefault(s => s.IsDeleted);

            // Get folder statistics
            var folderCount = await _context.Folders.CountAsync(f => f.ProjectId == projectId);
            var rootFolderCount = await _context.Folders.CountAsync(f => f.ProjectId == projectId && f.ParentFolderId == null);
            var maxDepth = await _context.Folders
                .Where(f => f.ProjectId == projectId)
                .MaxAsync(f => (int?)f.Level) ?? 0;

            // Get activity statistics
            var lastDocumentActivity = await _context.Documents
                .Where(d => d.ProjectId == projectId)
                .MaxAsync(d => (DateTime?)d.UpdatedAt ?? d.CreatedAt);

            var lastFolderActivity = await _context.Folders
                .Where(f => f.ProjectId == projectId)
                .MaxAsync(f => (DateTime?)f.UpdatedAt ?? f.CreatedAt);

            var stats = new ProjectStatsDto
            {
                ProjectId = projectId,
                ProjectName = project.Name,
                TotalDocuments = activeDocuments?.Count ?? 0,
                ActiveDocuments = activeDocuments?.Count ?? 0,
                DeletedDocuments = deletedDocuments?.Count ?? 0,
                TotalFileSize = activeDocuments?.TotalSize ?? 0,
                TotalFolders = folderCount,
                RootFolders = rootFolderCount,
                MaxFolderDepth = maxDepth,
                LastActivity = new[] { lastDocumentActivity, lastFolderActivity, project.UpdatedAt }
                    .Where(d => d.HasValue)
                    .DefaultIfEmpty()
                    .Max(),
                LastDocumentUpload = lastDocumentActivity,
                LastFolderCreation = lastFolderActivity,
                SharedWithTeams = 0, // TODO: Implement when team sharing is added
                CollaboratorCount = 1 // Owner only for now
            };

            return ServiceResult<ProjectStatsDto>.Success(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting project stats for project {ProjectId}", projectId);
            return ServiceResult<ProjectStatsDto>.Failure("An error occurred while getting project statistics");
        }
    }

    public async Task<ServiceResult> ArchiveProjectAsync(int projectId, string userId)
    {
        try
        {
            var project = await _context.Projects
                .FirstOrDefaultAsync(p => p.Id == projectId && p.OwnerId == userId);

            if (project == null)
                return ServiceResult.Failure("Project not found or you don't have permission to archive it");

            project.IsActive = false;
            project.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Project {ProjectId} archived successfully by user {UserId}", projectId, userId);

            return ServiceResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error archiving project {ProjectId}", projectId);
            return ServiceResult.Failure("An error occurred while archiving the project");
        }
    }

    public async Task<ServiceResult> RestoreProjectAsync(int projectId, string userId)
    {
        try
        {
            var project = await _context.Projects
                .FirstOrDefaultAsync(p => p.Id == projectId && p.OwnerId == userId);

            if (project == null)
                return ServiceResult.Failure("Project not found or you don't have permission to restore it");

            project.IsActive = true;
            project.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Project {ProjectId} restored successfully by user {UserId}", projectId, userId);

            return ServiceResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error restoring project {ProjectId}", projectId);
            return ServiceResult.Failure("An error occurred while restoring the project");
        }
    }

    public async Task<ServiceResult> ShareProjectWithTeamAsync(int projectId, int teamId, string userId)
    {
        await Task.CompletedTask;
        return ServiceResult.Failure("Team sharing not yet implemented");
    }

    public async Task<ServiceResult> UnshareProjectFromTeamAsync(int projectId, int teamId, string userId)
    {
        await Task.CompletedTask;
        return ServiceResult.Failure("Team sharing not yet implemented");
    }

    public async Task<ServiceResult<IEnumerable<DocumentDto>>> GetProjectDocumentsAsync(int projectId, string userId)
    {
        try
        {
            var hasAccess = await CanUserAccessProjectAsync(projectId, userId);
            if (!hasAccess.Succeeded || !hasAccess.Data)
                return ServiceResult<IEnumerable<DocumentDto>>.Failure("You don't have access to this project");

            var documents = await _context.Documents
                .Include(d => d.Owner)
                .Include(d => d.Team)
                .Where(d => d.ProjectId == projectId && !d.IsDeleted)
                .OrderByDescending(d => d.CreatedAt)
                .ToListAsync();

            var documentDtos = documents.Select(d => new DocumentDto
            {
                Id = d.Id,
                Title = d.Title,
                Description = d.Description,
                FileType = d.FileType,
                FileSize = d.FileSize,
                CreatedAt = d.CreatedAt,
                UpdatedAt = d.UpdatedAt,
                OwnerId = d.OwnerId,
                OwnerName = d.Owner?.UserName ?? "Unknown",
                TeamId = d.TeamId,
                TeamName = d.Team?.Name,
                IsDeleted = d.IsDeleted,
                CurrentVersionId = d.CurrentVersionId
            });

            return ServiceResult<IEnumerable<DocumentDto>>.Success(documentDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting documents for project {ProjectId}", projectId);
            return ServiceResult<IEnumerable<DocumentDto>>.Failure("An error occurred while getting project documents");
        }
    }

    public async Task<ServiceResult<IEnumerable<FolderDto>>> GetProjectFoldersAsync(int projectId, string userId)
    {
        try
        {
            var hasAccess = await CanUserAccessProjectAsync(projectId, userId);
            if (!hasAccess.Succeeded || !hasAccess.Data)
                return ServiceResult<IEnumerable<FolderDto>>.Failure("You don't have access to this project");

            var folders = await _context.Folders
                .Include(f => f.CreatedByUser)
                .Include(f => f.Project)
                .Where(f => f.ProjectId == projectId)
                .OrderBy(f => f.Level)
                .ThenBy(f => f.Name)
                .ToListAsync();

            var folderDtos = folders.Select(f => new FolderDto
            {
                Id = f.Id,
                Name = f.Name,
                Description = f.Description,
                ParentFolderId = f.ParentFolderId,
                Path = f.Path,
                Level = f.Level,
                ProjectId = f.ProjectId,
                ProjectName = f.Project.Name,
                CreatedAt = f.CreatedAt,
                UpdatedAt = f.UpdatedAt,
                CreatedByUserId = f.CreatedByUserId,
                CreatedByUserName = f.CreatedByUser?.UserName ?? "Unknown"
            });

            return ServiceResult<IEnumerable<FolderDto>>.Success(folderDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting folders for project {ProjectId}", projectId);
            return ServiceResult<IEnumerable<FolderDto>>.Failure("An error occurred while getting project folders");
        }
    }

    private async Task<ProjectDto> ConvertToProjectDtoAsync(Project project)
    {
        // Get statistics for the project
        var documentCount = await _context.Documents.CountAsync(d => d.ProjectId == project.Id && !d.IsDeleted);
        var folderCount = await _context.Folders.CountAsync(f => f.ProjectId == project.Id);
        var lastActivity = await _context.Documents
            .Where(d => d.ProjectId == project.Id)
            .MaxAsync(d => (DateTime?)(d.UpdatedAt ?? d.CreatedAt));

        return new ProjectDto
        {
            Id = project.Id,
            Name = project.Name,
            Description = project.Description,
            Color = project.Color,
            Icon = project.Icon,
            IsActive = project.IsActive,
            CreatedAt = project.CreatedAt,
            UpdatedAt = project.UpdatedAt,
            OwnerId = project.OwnerId,
            OwnerName = project.Owner?.UserName ?? "Unknown",
            DocumentCount = documentCount,
            FolderCount = folderCount,
            LastActivity = lastActivity ?? project.UpdatedAt ?? project.CreatedAt
        };
    }
} 