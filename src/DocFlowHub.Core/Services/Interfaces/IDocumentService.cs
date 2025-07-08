using DocFlowHub.Core.Models;
using DocFlowHub.Core.Models.Common;
using DocFlowHub.Core.Models.Documents;
using DocFlowHub.Core.Models.Documents.Dto;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DocFlowHub.Core.Services.Interfaces;

public interface IDocumentService
{
    Task<ServiceResult<DocumentDto>> GetDocumentByIdAsync(int id);
    Task<ServiceResult<PagedResult<DocumentDto>>> GetDocumentsAsync(string userId, DocumentFilter filter);
    Task<ServiceResult<IEnumerable<DocumentDto>>> GetUserDocumentsAsync(string userId, DocumentFilter filter);
    Task<ServiceResult<PagedResult<DocumentDto>>> GetDocumentsForUserAsync(string userId, DocumentFilter filter);
    Task<ServiceResult<bool>> CanUserAccessDocumentAsync(int documentId, string userId);
    Task<ServiceResult<DocumentDto>> CreateDocumentAsync(CreateDocumentRequest request, IFormFile file);
    Task<ServiceResult<DocumentDto>> UpdateDocumentAsync(int id, UpdateDocumentRequest request);
    Task<ServiceResult<DocumentDto>> UpdateDocumentContentAsync(int id, IFormFile file);
    Task<ServiceResult<DocumentVersionDto>> GetDocumentVersionAsync(int documentId, int versionId);
    Task<ServiceResult<PagedResult<DocumentVersionDto>>> GetDocumentVersionsAsync(int documentId, DocumentFilter filter);
    Task<ServiceResult<byte[]>> DownloadDocumentVersionAsync(int documentId, int versionId);
    Task<ServiceResult> DeleteDocumentAsync(int id);
    Task<ServiceResult> DeleteDocumentVersionAsync(int documentId, int versionId, string userId);
    Task<ServiceResult<BulkDeleteResult>> BulkDeleteDocumentsAsync(IEnumerable<int> documentIds, string userId);
    Task<ServiceResult> RestoreDocumentAsync(int id);
    Task<ServiceResult<DocumentDto>> ShareDocumentWithTeamAsync(int documentId, int teamId);
    Task<ServiceResult> UnshareDocumentFromTeamAsync(int documentId);
    Task<ServiceResult<DocumentDto>> UploadNewVersionAsync(UploadVersionRequest request);
    Task<ServiceResult<DocumentDto>> MoveDocumentAsync(MoveDocumentRequest request);
} 