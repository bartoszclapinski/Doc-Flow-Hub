using System.Collections.Generic;
using System.Threading.Tasks;
using DocFlowHub.Core.Models.Common;
using DocFlowHub.Core.Models.Documents;
using DocFlowHub.Core.Models.Documents.Dto;

namespace DocFlowHub.Core.Services.Interfaces;

public interface IDocumentService
{
    Task<ServiceResult<DocumentDto>> CreateDocumentAsync(CreateDocumentRequest request);
    Task<ServiceResult<DocumentDto>> UpdateDocumentAsync(UpdateDocumentRequest request);
    Task<ServiceResult> DeleteDocumentAsync(int id, string userId);
    Task<ServiceResult<DocumentDto>> GetDocumentByIdAsync(int id, string userId);
    Task<ServiceResult<IEnumerable<DocumentDto>>> GetUserDocumentsAsync(string userId, DocumentFilter? filter = null);
    Task<ServiceResult<DocumentDto>> UploadNewVersionAsync(UploadVersionRequest request);
    Task<ServiceResult<DocumentVersionDto>> GetVersionByIdAsync(int versionId, string userId);
    Task<ServiceResult<IEnumerable<DocumentVersionDto>>> GetDocumentVersionsAsync(int documentId, string userId);
    Task<ServiceResult<DocumentDto>> RestoreVersionAsync(int documentId, int versionId, string userId);
    Task<ServiceResult<IEnumerable<DocumentDto>>> GetTeamDocumentsAsync(int teamId, string userId, DocumentFilter? filter = null);
    Task<ServiceResult<DocumentDto>> ShareDocumentWithTeamAsync(int documentId, int teamId, string userId);
    Task<ServiceResult> RemoveDocumentFromTeamAsync(int documentId, int teamId, string userId);
} 