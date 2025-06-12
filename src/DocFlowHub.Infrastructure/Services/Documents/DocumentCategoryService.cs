using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DocFlowHub.Core.Models.Common;
using DocFlowHub.Core.Models.Documents;
using DocFlowHub.Core.Models.Documents.Dto;
using DocFlowHub.Core.Services.Interfaces;
using DocFlowHub.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DocFlowHub.Infrastructure.Services.Documents;

public class DocumentCategoryService : IDocumentCategoryService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DocumentCategoryService> _logger;

    public DocumentCategoryService(
        ApplicationDbContext context,
        ILogger<DocumentCategoryService> logger)
    {
        _context = context;
        _logger = logger;
    }

    private static DocumentCategoryDto MapToDto(DocumentCategory category)
    {
        return new DocumentCategoryDto
        {
            Id = category.Id,
            Name = category.Name,
            Description = category.Description ?? string.Empty,
            ParentId = category.ParentId
        };
    }

    public async Task<ServiceResult<DocumentCategoryDto>> CreateCategoryAsync(string name, string? description, int? parentId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(name))
                return ServiceResult<DocumentCategoryDto>.Failure("Category name is required");

            if (parentId.HasValue)
            {
                var parentExists = await _context.DocumentCategories.AnyAsync(c => c.Id == parentId.Value);
                if (!parentExists)
                    return ServiceResult<DocumentCategoryDto>.Failure("Parent category not found");
            }

            var category = new DocumentCategory
            {
                Name = name,
                Description = description,
                ParentId = parentId
            };

            _context.DocumentCategories.Add(category);
            await _context.SaveChangesAsync();

            return ServiceResult<DocumentCategoryDto>.Success(MapToDto(category));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating category");
            return ServiceResult<DocumentCategoryDto>.Failure("Error creating category: " + ex.Message);
        }
    }

    public async Task<ServiceResult<DocumentCategoryDto>> UpdateCategoryAsync(int id, string name, string? description)
    {
        try
        {
            var category = await _context.DocumentCategories
                .Include(c => c.Parent)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (category == null)
                return ServiceResult<DocumentCategoryDto>.Failure("Category not found");

            if (string.IsNullOrWhiteSpace(name))
                return ServiceResult<DocumentCategoryDto>.Failure("Category name is required");

            category.Name = name;
            category.Description = description;

            await _context.SaveChangesAsync();

            return ServiceResult<DocumentCategoryDto>.Success(MapToDto(category));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating category {CategoryId}", id);
            return ServiceResult<DocumentCategoryDto>.Failure("Error updating category: " + ex.Message);
        }
    }

    public async Task<ServiceResult> DeleteCategoryAsync(int id)
    {
        try
        {
            var category = await _context.DocumentCategories
                .Include(c => c.Children)
                .Include(c => c.Documents)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (category == null)
                return ServiceResult.Failure("Category not found");

            if (category.Children.Any())
                return ServiceResult.Failure("Cannot delete category with subcategories");

            if (category.Documents.Any())
                return ServiceResult.Failure("Cannot delete category with associated documents");

            _context.DocumentCategories.Remove(category);
            await _context.SaveChangesAsync();

            return ServiceResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting category {CategoryId}", id);
            return ServiceResult.Failure("Error deleting category: " + ex.Message);
        }
    }

    public async Task<ServiceResult<DocumentCategoryDto>> GetCategoryByIdAsync(int id)
    {
        try
        {
            var category = await _context.DocumentCategories
                .Include(c => c.Parent)
                .Include(c => c.Children)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (category == null)
                return ServiceResult<DocumentCategoryDto>.Failure("Category not found");

            return ServiceResult<DocumentCategoryDto>.Success(MapToDto(category));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting category {CategoryId}", id);
            return ServiceResult<DocumentCategoryDto>.Failure("Error getting category: " + ex.Message);
        }
    }

    public async Task<ServiceResult<IEnumerable<DocumentCategoryDto>>> GetAllCategoriesAsync()
    {
        try
        {
            var categories = await _context.DocumentCategories
                .Include(c => c.Children)
                .ToListAsync();

            return ServiceResult<IEnumerable<DocumentCategoryDto>>.Success(
                categories.Select(MapToDto));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting categories");
            return ServiceResult<IEnumerable<DocumentCategoryDto>>.Failure("Error getting categories: " + ex.Message);
        }
    }

    public async Task<ServiceResult<IEnumerable<DocumentCategoryDto>>> GetChildCategoriesAsync(int parentId)
    {
        try
        {
            var categories = await _context.DocumentCategories
                .Where(c => c.ParentId == parentId)
                .ToListAsync();

            return ServiceResult<IEnumerable<DocumentCategoryDto>>.Success(
                categories.Select(MapToDto));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting child categories for parent {ParentId}", parentId);
            return ServiceResult<IEnumerable<DocumentCategoryDto>>.Failure("Error getting child categories: " + ex.Message);
        }
    }

    public async Task<ServiceResult> AssignCategoriesToDocumentAsync(int documentId, IEnumerable<int> categoryIds)
    {
        try
        {
            var document = await _context.Documents
                .Include(d => d.Categories)
                .FirstOrDefaultAsync(d => d.Id == documentId);

            if (document == null)
                return ServiceResult.Failure("Document not found");

            var categories = await _context.DocumentCategories
                .Where(c => categoryIds.Contains(c.Id))
                .ToListAsync();

            document.Categories.Clear();
            foreach (var category in categories)
            {
                document.Categories.Add(category);
            }

            await _context.SaveChangesAsync();

            return ServiceResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning categories to document {DocumentId}", documentId);
            return ServiceResult.Failure("Error assigning categories: " + ex.Message);
        }
    }

    public async Task<ServiceResult<IEnumerable<DocumentCategoryDto>>> GetDocumentCategoriesAsync(int documentId)
    {
        try
        {
            var document = await _context.Documents
                .Include(d => d.Categories)
                .FirstOrDefaultAsync(d => d.Id == documentId);

            if (document == null)
                return ServiceResult<IEnumerable<DocumentCategoryDto>>.Failure("Document not found");

            return ServiceResult<IEnumerable<DocumentCategoryDto>>.Success(document.Categories.Select(MapToDto));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting categories for document {DocumentId}", documentId);
            return ServiceResult<IEnumerable<DocumentCategoryDto>>.Failure("Error getting document categories: " + ex.Message);
        }
    }

    public async Task<ServiceResult<IEnumerable<DocumentCategoryDto>>> GetRootCategoriesAsync()
    {
        try
        {
            var categories = await _context.DocumentCategories
                .Include(c => c.Children)
                .Where(c => c.ParentId == null)
                .ToListAsync();

            return ServiceResult<IEnumerable<DocumentCategoryDto>>.Success(
                categories.Select(MapToDto));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting root categories");
            return ServiceResult<IEnumerable<DocumentCategoryDto>>.Failure("Error getting root categories: " + ex.Message);
        }
    }

    public async Task<ServiceResult<IEnumerable<DocumentCategoryDto>>> GetSubcategoriesAsync(int parentId)
    {
        try
        {
            var categories = await _context.DocumentCategories
                .Include(c => c.Children)
                .Where(c => c.ParentId == parentId)
                .ToListAsync();

            return ServiceResult<IEnumerable<DocumentCategoryDto>>.Success(
                categories.Select(MapToDto));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting subcategories for parent {ParentId}", parentId);
            return ServiceResult<IEnumerable<DocumentCategoryDto>>.Failure("Error getting subcategories: " + ex.Message);
        }
    }
} 