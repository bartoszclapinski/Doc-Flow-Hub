using System.Collections.Generic;
using System.Threading.Tasks;
using DocFlowHub.Core.Models.Common;
using DocFlowHub.Core.Models.Documents;
using DocFlowHub.Core.Models.Documents.Dto;

namespace DocFlowHub.Core.Services.Interfaces;

public interface IDocumentService
{
    Task<ServiceResult<DocumentDto>> CreateDocumentAsync(CreateDocumentRequest request, string userId);
    
    Task<ServiceResult<DocumentDto>> UpdateDocumentAsync(int id, CreateDocumentRequest request, string userId);
    
    Task<ServiceResult> DeleteDocumentAsync(int id, string userId);
    
    Task<ServiceResult<DocumentDto>> GetDocumentByIdAsync(int id, string userId);
    
    Task<ServiceResult<IEnumerable<DocumentDto>>> GetUserDocumentsAsync(string userId, int? teamId = null);
    
    Task<ServiceResult<DocumentDto>> RestoreDocumentVersionAsync(int documentId, int versionId, string userId);
    
    Task<ServiceResult<IEnumerable<DocumentVersion>>> GetDocumentVersionsAsync(int documentId, string userId);
    
    Task<ServiceResult<DocumentVersion>> GetDocumentVersionAsync(int documentId, int versionId, string userId);
    
    Task<ServiceResult<byte[]>> DownloadDocumentAsync(int documentId, int? versionId, string userId);
} 