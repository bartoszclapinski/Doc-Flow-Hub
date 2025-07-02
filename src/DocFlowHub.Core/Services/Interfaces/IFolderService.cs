using DocFlowHub.Core.Models.Common;
using DocFlowHub.Core.Models.Documents.Dto;
using DocFlowHub.Core.Models.Projects.Dto;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DocFlowHub.Core.Services.Interfaces;

public interface IFolderService
{
    // Basic CRUD operations
    Task<ServiceResult<FolderDto>> CreateFolderAsync(CreateFolderRequest request, string userId);
    Task<ServiceResult<FolderDto>> GetFolderByIdAsync(int id);
    Task<ServiceResult<FolderDto>> UpdateFolderAsync(int id, UpdateFolderRequest request, string userId);
    Task<ServiceResult> DeleteFolderAsync(int id, string userId);
    
    // Hierarchical operations
    Task<ServiceResult<IEnumerable<FolderDto>>> GetProjectFoldersAsync(int projectId, string userId);
    Task<ServiceResult<IEnumerable<FolderDto>>> GetRootFoldersAsync(int projectId, string userId);
    Task<ServiceResult<IEnumerable<FolderDto>>> GetChildFoldersAsync(int parentFolderId, string userId);
    Task<ServiceResult<FolderDto>> GetFolderTreeAsync(int folderId, string userId);
    Task<ServiceResult<IEnumerable<FolderDto>>> GetFolderPathAsync(int folderId);
    
    // Folder organization
    Task<ServiceResult> MoveFolderAsync(int folderId, int? newParentFolderId, string userId);
    Task<ServiceResult> CopyFolderAsync(int folderId, int? targetParentFolderId, string userId);
    Task<ServiceResult<string>> GenerateUniqueFolderNameAsync(int projectId, int? parentFolderId, string baseName);
    
    // Access control
    Task<ServiceResult<bool>> CanUserAccessFolderAsync(int folderId, string userId);
    Task<ServiceResult<bool>> CanUserEditFolderAsync(int folderId, string userId);
    Task<ServiceResult<bool>> CanUserDeleteFolderAsync(int folderId, string userId);
    
    // Content management
    Task<ServiceResult<IEnumerable<DocumentDto>>> GetFolderDocumentsAsync(int folderId, string userId);
    Task<ServiceResult<FolderStatsDto>> GetFolderStatsAsync(int folderId);
    Task<ServiceResult> EmptyFolderAsync(int folderId, string userId); // Delete all contents
    
    // Bulk operations
    Task<ServiceResult> BulkDeleteFoldersAsync(IEnumerable<int> folderIds, string userId);
    Task<ServiceResult> BulkMoveFoldersAsync(IEnumerable<int> folderIds, int? targetParentFolderId, string userId);
} 