using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using DocFlowHub.Core.Models;
using DocFlowHub.Core.Models.Documents;
using DocFlowHub.Core.Models.Documents.Dto;
using DocFlowHub.Core.Services.Interfaces;
using DocFlowHub.Web.Extensions;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace DocFlowHub.Web.Pages.Documents;

[Authorize]
public class UploadModel : PageModel
{
    private readonly IDocumentService _documentService;
    private readonly IDocumentCategoryService _categoryService;
    private readonly ITeamService _teamService;

    public UploadModel(
        IDocumentService documentService,
        IDocumentCategoryService categoryService,
        ITeamService teamService)
    {
        _documentService = documentService;
        _categoryService = categoryService;
        _teamService = teamService;
    }

    [BindProperty]
    public string Title { get; set; } = string.Empty;

    [BindProperty]
    public string Description { get; set; } = string.Empty;

    [BindProperty]
    public IFormFile File { get; set; } = null!;

    [BindProperty]
    public List<int> SelectedCategoryIds { get; set; } = new();

    [BindProperty]
    public int? TeamId { get; set; }

    public IEnumerable<DocumentCategoryDto> Categories { get; set; } = new List<DocumentCategoryDto>();
    public IEnumerable<Team> Teams { get; set; } = new List<Team>();
    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var userId = User.GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return RedirectToPage("/Account/Login");
        }

        var categoriesResult = await _categoryService.GetAllCategoriesAsync();
        if (!categoriesResult.Succeeded)
        {
            ErrorMessage = categoriesResult.Error;
            return Page();
        }
        Categories = categoriesResult.Data;

        var teamsResult = await _teamService.GetUserTeamsAsync(userId);
        if (!teamsResult.Succeeded)
        {
            ErrorMessage = teamsResult.Error;
            return Page();
        }
        Teams = teamsResult.Data;

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var userId = User.GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return RedirectToPage("/Account/Login");
        }

        if (!ModelState.IsValid)
        {
            var categoriesResult = await _categoryService.GetAllCategoriesAsync();
            if (categoriesResult.Succeeded)
            {
                Categories = categoriesResult.Data;
            }

            var teamsResult = await _teamService.GetUserTeamsAsync(userId);
            if (teamsResult.Succeeded)
            {
                Teams = teamsResult.Data;
            }

            return Page();
        }

        var request = new CreateDocumentRequest
        {
            Title = Title,
            Description = Description,
            CategoryIds = SelectedCategoryIds,
            TeamId = TeamId,
            OwnerId = userId
        };

        var result = await _documentService.CreateDocumentAsync(request, File);
        if (!result.Succeeded)
        {
            ErrorMessage = result.Error;
            var categoriesResult = await _categoryService.GetAllCategoriesAsync();
            if (categoriesResult.Succeeded)
            {
                Categories = categoriesResult.Data;
            }

            var teamsResult = await _teamService.GetUserTeamsAsync(userId);
            if (teamsResult.Succeeded)
            {
                Teams = teamsResult.Data;
            }

            return Page();
        }

        return RedirectToPage("./Index");
    }
} 