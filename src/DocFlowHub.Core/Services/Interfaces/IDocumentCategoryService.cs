using System.Collections.Generic;
using System.Threading.Tasks;
using DocFlowHub.Core.Models.Common;
using DocFlowHub.Core.Models.Documents;

namespace DocFlowHub.Core.Services.Interfaces;

public interface IDocumentCategoryService
{
    Task<ServiceResult<DocumentCategory>> CreateCategoryAsync(string name, string? description, int? parentCategoryId);
    
    Task<ServiceResult<DocumentCategory>> UpdateCategoryAsync(int id, string name, string? description);
    
    Task<ServiceResult> DeleteCategoryAsync(int id);
    
    Task<ServiceResult<DocumentCategory>> GetCategoryByIdAsync(int id);
    
    Task<ServiceResult<IEnumerable<DocumentCategory>>> GetAllCategoriesAsync();
    
    Task<ServiceResult<IEnumerable<DocumentCategory>>> GetRootCategoriesAsync();
    
    Task<ServiceResult<IEnumerable<DocumentCategory>>> GetSubcategoriesAsync(int parentId);
    
    Task<ServiceResult> AssignCategoriesToDocumentAsync(int documentId, IEnumerable<int> categoryIds);
    
    Task<ServiceResult<IEnumerable<DocumentCategory>>> GetDocumentCategoriesAsync(int documentId);
} 