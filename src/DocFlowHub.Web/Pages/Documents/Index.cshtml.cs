using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using DocFlowHub.Core.Models;
using DocFlowHub.Core.Models.Common;
using DocFlowHub.Core.Models.Documents;
using DocFlowHub.Core.Models.Documents.Dto;
using DocFlowHub.Core.Services.Interfaces;
using DocFlowHub.Web.Extensions;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DocFlowHub.Web.Pages.Documents;

[Authorize]
public class IndexModel : PageModel
{
    private readonly IDocumentService _documentService;
    private readonly IDocumentCategoryService _categoryService;

    public IndexModel(
        IDocumentService documentService,
        IDocumentCategoryService categoryService)
    {
        _documentService = documentService;
        _categoryService = categoryService;
    }

    public PagedResult<DocumentDto> Documents { get; set; } = new();
    public IEnumerable<DocumentCategoryDto> Categories { get; set; } = new List<DocumentCategoryDto>();
    public string? ErrorMessage { get; set; }

    [BindProperty(SupportsGet = true)]
    public DocumentFilter Filter { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        var categoriesResult = await _categoryService.GetAllCategoriesAsync();
        if (!categoriesResult.Succeeded)
        {
            ErrorMessage = categoriesResult.Error;
            return Page();
        }
        Categories = categoriesResult.Data;

        var documentsResult = await _documentService.GetDocumentsAsync(Filter);
        if (!documentsResult.Succeeded)
        {
            ErrorMessage = documentsResult.Error;
            return Page();
        }
        Documents = documentsResult.Data;

        return Page();
    }
} 