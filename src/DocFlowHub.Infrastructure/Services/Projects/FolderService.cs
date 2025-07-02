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

public class FolderService : IFolderService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<FolderService> _logger;

    public FolderService(
        ApplicationDbContext context,
        ILogger<FolderService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ServiceResult<FolderDto>> CreateFolderAsync(CreateFolderRequest request, string userId)
    {
        try
        {
            // Validation
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return ServiceResult<FolderDto>.Failure("Folder name is required");
            }

            // Check if project exists and user has access
            var project = await _context.Projects
                .FirstOrDefaultAsync(p => p.Id == request.ProjectId && p.OwnerId == userId);

            if (project == null)
                return ServiceResult<FolderDto>.Failure("Project not found or you don't have access to it");

            // Validate parent folder if specified
            Folder? parentFolder = null;
            if (request.ParentFolderId.HasValue)
            {
                parentFolder = await _context.Folders
                    .FirstOrDefaultAsync(f => f.Id == request.ParentFolderId.Value && f.ProjectId == request.ProjectId);

                if (parentFolder == null)
                    return ServiceResult<FolderDto>.Failure("Parent folder not found");
            }

            // Calculate path and level
            var level = parentFolder?.Level + 1 ?? 0;
            var path = parentFolder != null ? $"{parentFolder.Path}/{request.Name}" : $"/{request.Name}";

            var folder = new Folder
            {
                Name = request.Name,
                Description = request.Description,
                ProjectId = request.ProjectId,
                ParentFolderId = request.ParentFolderId,
                Path = path,
                Level = level,
                CreatedByUserId = userId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Folders.Add(folder);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Folder {FolderName} created successfully in project {ProjectId} by user {UserId}", request.Name, request.ProjectId, userId);

            return ServiceResult<FolderDto>.Success(await ConvertToFolderDtoAsync(folder));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating folder {FolderName} in project {ProjectId} for user {UserId}", request.Name, request.ProjectId, userId);
            return ServiceResult<FolderDto>.Failure("An error occurred while creating the folder");
        }
    }

    public async Task<ServiceResult<FolderDto>> GetFolderByIdAsync(int id)
    {
        try
        {
            var folder = await _context.Folders
                .Include(f => f.Project)
                .Include(f => f.CreatedByUser)
                .Include(f => f.Parent)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (folder == null)
                return ServiceResult<FolderDto>.Failure("Folder not found");

            return ServiceResult<FolderDto>.Success(await ConvertToFolderDtoAsync(folder));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting folder {FolderId}", id);
            return ServiceResult<FolderDto>.Failure("An error occurred while getting the folder");
        }
    }

    // Placeholder implementations for remaining methods
    public async Task<ServiceResult<FolderDto>> UpdateFolderAsync(int id, UpdateFolderRequest request, string userId)
    {
        try
        {
            var canEdit = await CanUserEditFolderAsync(id, userId);
            if (!canEdit.Succeeded || !canEdit.Data)
                return ServiceResult<FolderDto>.Failure("You don't have permission to edit this folder");

            var folder = await _context.Folders
                .Include(f => f.Project)
                .Include(f => f.CreatedByUser)
                .Include(f => f.Parent)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (folder == null)
                return ServiceResult<FolderDto>.Failure("Folder not found");

            folder.Name = request.Name;
            folder.Description = request.Description;
            folder.UpdatedAt = DateTime.UtcNow;

            // If name changed, update path for this folder and all its children
            if (folder.Name != request.Name)
            {
                var oldPath = folder.Path;
                var newPath = folder.Parent != null ? $"{folder.Parent.Path}/{request.Name}" : $"/{request.Name}";
                folder.Path = newPath;

                // Update all child folder paths
                var childFolders = await _context.Folders
                    .Where(f => f.Path.StartsWith(oldPath + "/"))
                    .ToListAsync();

                foreach (var child in childFolders)
                {
                    child.Path = child.Path.Replace(oldPath, newPath);
                    child.UpdatedAt = DateTime.UtcNow;
                }
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Folder {FolderId} updated successfully by user {UserId}", id, userId);

            return ServiceResult<FolderDto>.Success(await ConvertToFolderDtoAsync(folder));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating folder {FolderId}", id);
            return ServiceResult<FolderDto>.Failure("An error occurred while updating the folder");
        }
    }

    public async Task<ServiceResult> DeleteFolderAsync(int id, string userId)
    {
        try
        {
            var canDelete = await CanUserDeleteFolderAsync(id, userId);
            if (!canDelete.Succeeded || !canDelete.Data)
                return ServiceResult.Failure("Cannot delete folder. Either you don't have permission or the folder is not empty.");

            var folder = await _context.Folders.FirstOrDefaultAsync(f => f.Id == id);
            if (folder == null)
                return ServiceResult.Failure("Folder not found");

            _context.Folders.Remove(folder);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Folder {FolderId} deleted successfully by user {UserId}", id, userId);

            return ServiceResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting folder {FolderId}", id);
            return ServiceResult.Failure("An error occurred while deleting the folder");
        }
    }

    public async Task<ServiceResult<IEnumerable<FolderDto>>> GetProjectFoldersAsync(int projectId, string userId)
    {
        try
        {
            // Check if user has access to the project
            var hasAccess = await _context.Projects.AnyAsync(p => p.Id == projectId && p.OwnerId == userId);
            if (!hasAccess)
                return ServiceResult<IEnumerable<FolderDto>>.Failure("You don't have access to this project");

            var folders = await _context.Folders
                .Include(f => f.Project)
                .Include(f => f.CreatedByUser)
                .Include(f => f.Parent)
                .Where(f => f.ProjectId == projectId)
                .OrderBy(f => f.Level)
                .ThenBy(f => f.Name)
                .ToListAsync();

            var folderDtos = new List<FolderDto>();
            foreach (var folder in folders)
            {
                folderDtos.Add(await ConvertToFolderDtoAsync(folder));
            }

            return ServiceResult<IEnumerable<FolderDto>>.Success(folderDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting folders for project {ProjectId}", projectId);
            return ServiceResult<IEnumerable<FolderDto>>.Failure("An error occurred while getting project folders");
        }
    }

    public async Task<ServiceResult<IEnumerable<FolderDto>>> GetRootFoldersAsync(int projectId, string userId)
    {
        try
        {
            // Check if user has access to the project
            var hasAccess = await _context.Projects.AnyAsync(p => p.Id == projectId && p.OwnerId == userId);
            if (!hasAccess)
                return ServiceResult<IEnumerable<FolderDto>>.Failure("You don't have access to this project");

            var folders = await _context.Folders
                .Include(f => f.Project)
                .Include(f => f.CreatedByUser)
                .Where(f => f.ProjectId == projectId && f.ParentFolderId == null)
                .OrderBy(f => f.Name)
                .ToListAsync();

            var folderDtos = new List<FolderDto>();
            foreach (var folder in folders)
            {
                folderDtos.Add(await ConvertToFolderDtoAsync(folder));
            }

            return ServiceResult<IEnumerable<FolderDto>>.Success(folderDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting root folders for project {ProjectId}", projectId);
            return ServiceResult<IEnumerable<FolderDto>>.Failure("An error occurred while getting root folders");
        }
    }

    public async Task<ServiceResult<IEnumerable<FolderDto>>> GetChildFoldersAsync(int parentFolderId, string userId)
    {
        try
        {
            var hasAccess = await CanUserAccessFolderAsync(parentFolderId, userId);
            if (!hasAccess.Succeeded || !hasAccess.Data)
                return ServiceResult<IEnumerable<FolderDto>>.Failure("You don't have access to this folder");

            var folders = await _context.Folders
                .Include(f => f.Project)
                .Include(f => f.CreatedByUser)
                .Include(f => f.Parent)
                .Where(f => f.ParentFolderId == parentFolderId)
                .OrderBy(f => f.Name)
                .ToListAsync();

            var folderDtos = new List<FolderDto>();
            foreach (var folder in folders)
            {
                folderDtos.Add(await ConvertToFolderDtoAsync(folder));
            }

            return ServiceResult<IEnumerable<FolderDto>>.Success(folderDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting child folders for folder {ParentFolderId}", parentFolderId);
            return ServiceResult<IEnumerable<FolderDto>>.Failure("An error occurred while getting child folders");
        }
    }

    public async Task<ServiceResult<FolderDto>> GetFolderTreeAsync(int folderId, string userId)
    {
        try
        {
            var hasAccess = await CanUserAccessFolderAsync(folderId, userId);
            if (!hasAccess.Succeeded || !hasAccess.Data)
                return ServiceResult<FolderDto>.Failure("You don't have access to this folder");

            var folder = await _context.Folders
                .Include(f => f.Project)
                .Include(f => f.CreatedByUser)
                .Include(f => f.Parent)
                .FirstOrDefaultAsync(f => f.Id == folderId);

            if (folder == null)
                return ServiceResult<FolderDto>.Failure("Folder not found");

            // For now, return the folder without building the full tree structure
            // In a future version, this could include children in a nested structure
            return ServiceResult<FolderDto>.Success(await ConvertToFolderDtoAsync(folder));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting folder tree for folder {FolderId}", folderId);
            return ServiceResult<FolderDto>.Failure("An error occurred while getting folder tree");
        }
    }

    public async Task<ServiceResult<IEnumerable<FolderDto>>> GetFolderPathAsync(int folderId)
    {
        try
        {
            var folder = await _context.Folders
                .Include(f => f.Project)
                .Include(f => f.CreatedByUser)
                .FirstOrDefaultAsync(f => f.Id == folderId);

            if (folder == null)
                return ServiceResult<IEnumerable<FolderDto>>.Failure("Folder not found");

            var pathParts = folder.Path.Split('/', StringSplitOptions.RemoveEmptyEntries);
            var pathFolders = new List<FolderDto>();

            // Build the path from root to current folder
            var currentPath = "";
            foreach (var part in pathParts)
            {
                currentPath = currentPath == "" ? $"/{part}" : $"{currentPath}/{part}";
                var pathFolder = await _context.Folders
                    .Include(f => f.Project)
                    .Include(f => f.CreatedByUser)
                    .Include(f => f.Parent)
                    .FirstOrDefaultAsync(f => f.Path == currentPath && f.ProjectId == folder.ProjectId);

                if (pathFolder != null)
                {
                    pathFolders.Add(await ConvertToFolderDtoAsync(pathFolder));
                }
            }

            return ServiceResult<IEnumerable<FolderDto>>.Success(pathFolders);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting folder path for folder {FolderId}", folderId);
            return ServiceResult<IEnumerable<FolderDto>>.Failure("An error occurred while getting folder path");
        }
    }

    public async Task<ServiceResult> MoveFolderAsync(int folderId, int? newParentFolderId, string userId)
    {
        try
        {
            var canEdit = await CanUserEditFolderAsync(folderId, userId);
            if (!canEdit.Succeeded || !canEdit.Data)
                return ServiceResult.Failure("You don't have permission to move this folder");

            var folder = await _context.Folders
                .Include(f => f.Parent)
                .FirstOrDefaultAsync(f => f.Id == folderId);

            if (folder == null)
                return ServiceResult.Failure("Folder not found");

            Folder? newParent = null;
            if (newParentFolderId.HasValue)
            {
                newParent = await _context.Folders.FirstOrDefaultAsync(f => f.Id == newParentFolderId.Value);
                if (newParent == null)
                    return ServiceResult.Failure("Target parent folder not found");

                // Prevent moving folder into itself or its descendants
                if (newParent.Path.StartsWith(folder.Path))
                    return ServiceResult.Failure("Cannot move folder into itself or its descendant");
            }

            var oldPath = folder.Path;
            var newPath = newParent != null ? $"{newParent.Path}/{folder.Name}" : $"/{folder.Name}";
            var newLevel = newParent?.Level + 1 ?? 0;

            folder.ParentFolderId = newParentFolderId;
            folder.Path = newPath;
            folder.Level = newLevel;
            folder.UpdatedAt = DateTime.UtcNow;

            // Update all descendant paths
            var descendants = await _context.Folders
                .Where(f => f.Path.StartsWith(oldPath + "/"))
                .ToListAsync();

            foreach (var descendant in descendants)
            {
                descendant.Path = descendant.Path.Replace(oldPath, newPath);
                descendant.Level = descendant.Level - folder.Level + newLevel;
                descendant.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Folder {FolderId} moved successfully by user {UserId}", folderId, userId);

            return ServiceResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error moving folder {FolderId}", folderId);
            return ServiceResult.Failure("An error occurred while moving the folder");
        }
    }

    public async Task<ServiceResult> CopyFolderAsync(int folderId, int? targetParentFolderId, string userId)
    {
        await Task.CompletedTask;
        return ServiceResult.Failure("Copy folder operation will be implemented in a future version");
    }

    public async Task<ServiceResult<string>> GenerateUniqueFolderNameAsync(int projectId, int? parentFolderId, string baseName)
    {
        try
        {
            var existingNames = await _context.Folders
                .Where(f => f.ProjectId == projectId && f.ParentFolderId == parentFolderId)
                .Select(f => f.Name)
                .ToListAsync();

            var uniqueName = baseName;
            var counter = 1;

            while (existingNames.Contains(uniqueName))
            {
                uniqueName = $"{baseName} ({counter})";
                counter++;
            }

            return ServiceResult<string>.Success(uniqueName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating unique folder name for project {ProjectId}", projectId);
            return ServiceResult<string>.Failure("An error occurred while generating unique folder name");
        }
    }

    public async Task<ServiceResult<bool>> CanUserAccessFolderAsync(int folderId, string userId)
    {
        try
        {
            var hasAccess = await _context.Folders
                .AnyAsync(f => f.Id == folderId && 
                              _context.Projects.Any(p => p.Id == f.ProjectId && p.OwnerId == userId));

            return ServiceResult<bool>.Success(hasAccess);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking folder access for user {UserId} on folder {FolderId}", userId, folderId);
            return ServiceResult<bool>.Failure("An error occurred while checking folder access");
        }
    }

    public async Task<ServiceResult<bool>> CanUserEditFolderAsync(int folderId, string userId)
    {
        // For now, same as access - only project owners can edit folders
        return await CanUserAccessFolderAsync(folderId, userId);
    }

    public async Task<ServiceResult<bool>> CanUserDeleteFolderAsync(int folderId, string userId)
    {
        try
        {
            // Check if user owns the project and folder is empty
            var folder = await _context.Folders
                .Include(f => f.Project)
                .FirstOrDefaultAsync(f => f.Id == folderId);

            if (folder == null)
                return ServiceResult<bool>.Success(false);

            // User must own the project
            if (folder.Project.OwnerId != userId)
                return ServiceResult<bool>.Success(false);

            // Check if folder has any content
            var hasDocuments = await _context.Documents.AnyAsync(d => d.FolderId == folderId);
            var hasSubfolders = await _context.Folders.AnyAsync(f => f.ParentFolderId == folderId);

            // Can only delete empty folders
            return ServiceResult<bool>.Success(!hasDocuments && !hasSubfolders);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking folder delete permission for user {UserId} on folder {FolderId}", userId, folderId);
            return ServiceResult<bool>.Failure("An error occurred while checking folder delete permission");
        }
    }

    public async Task<ServiceResult<IEnumerable<DocumentDto>>> GetFolderDocumentsAsync(int folderId, string userId)
    {
        try
        {
            var hasAccess = await CanUserAccessFolderAsync(folderId, userId);
            if (!hasAccess.Succeeded || !hasAccess.Data)
                return ServiceResult<IEnumerable<DocumentDto>>.Failure("You don't have access to this folder");

            var documents = await _context.Documents
                .Include(d => d.Owner)
                .Include(d => d.Team)
                .Where(d => d.FolderId == folderId && !d.IsDeleted)
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
            _logger.LogError(ex, "Error getting documents for folder {FolderId}", folderId);
            return ServiceResult<IEnumerable<DocumentDto>>.Failure("An error occurred while getting folder documents");
        }
    }

    public async Task<ServiceResult<FolderStatsDto>> GetFolderStatsAsync(int folderId)
    {
        try
        {
            var folder = await _context.Folders
                .Include(f => f.Project)
                .FirstOrDefaultAsync(f => f.Id == folderId);

            if (folder == null)
                return ServiceResult<FolderStatsDto>.Failure("Folder not found");

            // Get document statistics (direct documents in this folder)
            var directDocumentStats = await _context.Documents
                .Where(d => d.FolderId == folderId)
                .GroupBy(d => d.IsDeleted)
                .Select(g => new { IsDeleted = g.Key, Count = g.Count(), TotalSize = g.Sum(d => d.FileSize) })
                .ToListAsync();

            var activeDirectDocuments = directDocumentStats.FirstOrDefault(s => !s.IsDeleted);
            var deletedDirectDocuments = directDocumentStats.FirstOrDefault(s => s.IsDeleted);

            // Get all descendant folder paths
            var descendantFolders = await _context.Folders
                .Where(f => f.Path.StartsWith(folder.Path + "/"))
                .ToListAsync();

            var descendantFolderIds = descendantFolders.Select(f => f.Id).ToList();

            // Get total document statistics (including all descendants)
            var totalDocumentStats = await _context.Documents
                .Where(d => d.FolderId == folderId || descendantFolderIds.Contains(d.FolderId ?? 0))
                .GroupBy(d => d.IsDeleted)
                .Select(g => new { IsDeleted = g.Key, Count = g.Count(), TotalSize = g.Sum(d => d.FileSize) })
                .ToListAsync();

            var activeTotalDocuments = totalDocumentStats.FirstOrDefault(s => !s.IsDeleted);

            // Get direct and total subfolder counts
            var directSubfolders = await _context.Folders.CountAsync(f => f.ParentFolderId == folderId);
            var totalSubfolders = descendantFolders.Count;

            // Get activity statistics
            var lastDirectActivity = await _context.Documents
                .Where(d => d.FolderId == folderId)
                .MaxAsync(d => (DateTime?)(d.UpdatedAt ?? d.CreatedAt));

            var lastTotalActivity = await _context.Documents
                .Where(d => d.FolderId == folderId || descendantFolderIds.Contains(d.FolderId ?? 0))
                .MaxAsync(d => (DateTime?)(d.UpdatedAt ?? d.CreatedAt));

            var stats = new FolderStatsDto
            {
                FolderId = folderId,
                FolderName = folder.Name,
                Path = folder.Path,
                Level = folder.Level,
                DirectDocumentCount = activeDirectDocuments?.Count ?? 0,
                TotalDocumentCount = activeTotalDocuments?.Count ?? 0,
                DirectSubfolderCount = directSubfolders,
                TotalSubfolderCount = totalSubfolders,
                DirectFileSize = activeDirectDocuments?.TotalSize ?? 0,
                TotalFileSize = activeTotalDocuments?.TotalSize ?? 0,
                LastActivity = lastTotalActivity ?? lastDirectActivity,
                LastDocumentAdded = lastDirectActivity,
                LastSubfolderCreated = await _context.Folders
                    .Where(f => f.ParentFolderId == folderId)
                    .MaxAsync(f => (DateTime?)(f.UpdatedAt ?? f.CreatedAt)),
                MaxDepth = totalSubfolders > 0 ? 
                    await _context.Folders
                        .Where(f => f.Path.StartsWith(folder.Path + "/"))
                        .MaxAsync(f => (int?)(f.Level - folder.Level)) ?? 0 : 0,
                HasSubfolders = directSubfolders > 0,
                HasDocuments = (activeDirectDocuments?.Count ?? 0) > 0,
                IsEmpty = directSubfolders == 0 && (activeDirectDocuments?.Count ?? 0) == 0
            };

            return ServiceResult<FolderStatsDto>.Success(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting folder stats for folder {FolderId}", folderId);
            return ServiceResult<FolderStatsDto>.Failure("An error occurred while getting folder statistics");
        }
    }

    public async Task<ServiceResult> EmptyFolderAsync(int folderId, string userId)
    {
        try
        {
            var canEdit = await CanUserEditFolderAsync(folderId, userId);
            if (!canEdit.Succeeded || !canEdit.Data)
                return ServiceResult.Failure("You don't have permission to empty this folder");

            // Get all documents in this folder (not including subfolders)
            var documents = await _context.Documents
                .Where(d => d.FolderId == folderId)
                .ToListAsync();

            // Soft delete all documents
            foreach (var document in documents)
            {
                document.IsDeleted = true;
                document.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Folder {FolderId} emptied successfully by user {UserId}", folderId, userId);

            return ServiceResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error emptying folder {FolderId}", folderId);
            return ServiceResult.Failure("An error occurred while emptying the folder");
        }
    }

    public async Task<ServiceResult> BulkDeleteFoldersAsync(IEnumerable<int> folderIds, string userId)
    {
        await Task.CompletedTask;
        return ServiceResult.Failure("Bulk folder operations will be implemented in a future version");
    }

    public async Task<ServiceResult> BulkMoveFoldersAsync(IEnumerable<int> folderIds, int? targetParentFolderId, string userId)
    {
        await Task.CompletedTask;
        return ServiceResult.Failure("Bulk folder operations will be implemented in a future version");
    }

    private async Task<FolderDto> ConvertToFolderDtoAsync(Folder folder)
    {
        // Get statistics for the folder
        var documentCount = await _context.Documents.CountAsync(d => d.FolderId == folder.Id && !d.IsDeleted);
        var subfolderCount = await _context.Folders.CountAsync(f => f.ParentFolderId == folder.Id);
        var lastActivity = await _context.Documents
            .Where(d => d.FolderId == folder.Id)
            .MaxAsync(d => (DateTime?)(d.UpdatedAt ?? d.CreatedAt));

        return new FolderDto
        {
            Id = folder.Id,
            Name = folder.Name,
            Description = folder.Description,
            ParentFolderId = folder.ParentFolderId,
            ParentFolderName = folder.Parent?.Name,
            Path = folder.Path,
            Level = folder.Level,
            ProjectId = folder.ProjectId,
            ProjectName = folder.Project?.Name ?? "Unknown",
            CreatedAt = folder.CreatedAt,
            UpdatedAt = folder.UpdatedAt,
            CreatedByUserId = folder.CreatedByUserId,
            CreatedByUserName = folder.CreatedByUser?.UserName ?? "Unknown",
            DocumentCount = documentCount,
            SubfolderCount = subfolderCount,
            LastActivity = lastActivity ?? folder.UpdatedAt ?? folder.CreatedAt
        };
    }
} 