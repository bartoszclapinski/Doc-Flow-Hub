using System.Collections.Generic;
using System.Threading.Tasks;
using DocFlowHub.Core.Models.Common;
using DocFlowHub.Core.Models.Documents.Dto;

namespace DocFlowHub.Core.Services.Interfaces;

public interface IDocumentCategoryService
{
    Task<ServiceResult<DocumentCategoryDto>> CreateCategoryAsync(string name, string? description, int? parentId);
    
    Task<ServiceResult<DocumentCategoryDto>> UpdateCategoryAsync(int id, string name, string? description);
    
    Task<ServiceResult> DeleteCategoryAsync(int id);
    
    Task<ServiceResult<DocumentCategoryDto>> GetCategoryByIdAsync(int id);
    
    Task<ServiceResult<IEnumerable<DocumentCategoryDto>>> GetAllCategoriesAsync();
    
    Task<ServiceResult<IEnumerable<DocumentCategoryDto>>> GetRootCategoriesAsync();
    
    Task<ServiceResult<IEnumerable<DocumentCategoryDto>>> GetSubcategoriesAsync(int parentId);
    
    Task<ServiceResult> AssignCategoriesToDocumentAsync(int documentId, IEnumerable<int> categoryIds);
    
    Task<ServiceResult<IEnumerable<DocumentCategoryDto>>> GetDocumentCategoriesAsync(int documentId);
} 