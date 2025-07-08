using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using DocFlowHub.Core.Models.Projects.Dto;
using DocFlowHub.Core.Models.Documents.Dto;
using DocFlowHub.Core.Services.Interfaces;
using DocFlowHub.Core.Models.Documents;
using System.Security.Claims;

namespace DocFlowHub.Web.Pages.Folders;

[Authorize]
public class DetailsModel : PageModel
{
    private readonly IFolderService _folderService;
    private readonly IProjectService _projectService;
    private readonly IDocumentService _documentService;

    public DetailsModel(
        IFolderService folderService, 
        IProjectService projectService,
        IDocumentService documentService)
    {
        _folderService = folderService;
        _projectService = projectService;
        _documentService = documentService;
    }

    [BindProperty(SupportsGet = true)]
    public int Id { get; set; }

    [BindProperty(SupportsGet = true)]
    public int ProjectId { get; set; }

    public FolderDto? Folder { get; set; }
    public ProjectDto? Project { get; set; }
    public List<FolderDto> FolderPath { get; set; } = new();
    public IEnumerable<DocumentDto> Documents { get; set; } = new List<DocumentDto>();
    public string? ErrorMessage { get; set; }

    // Statistics
    public int TotalDocuments { get; set; }
    public int TotalSubfolders { get; set; }
    public long TotalSize { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (Id <= 0)
        {
            return RedirectToPage("/Projects/Index");
        }

        try
        {
            // Get folder details
            var folderResult = await _folderService.GetFolderByIdAsync(Id, User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            if (!folderResult.Succeeded)
            {
                ErrorMessage = "Folder not found or access denied.";
                return RedirectToPage("/Projects/Index");
            }

            Folder = folderResult.Data;

            // Get project details
            var projectResult = await _projectService.GetProjectByIdAsync(Folder.ProjectId, User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            if (projectResult.Succeeded)
            {
                Project = projectResult.Data;
            }

            // Get folder path
            if (Folder.ParentFolderId.HasValue)
            {
                var pathResult = await _folderService.GetFolderPathAsync(Folder.Id, User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                if (pathResult.Succeeded)
                {
                    FolderPath = pathResult.Data.ToList();
                }
            }

            // Get documents in this folder
            var documentsFilter = new DocumentFilter
            {
                FolderId = Id,
                PageNumber = 1,
                PageSize = 50 // Show first 50 documents
            };

            var documentsResult = await _documentService.GetDocumentsAsync(User.FindFirstValue(ClaimTypes.NameIdentifier)!, documentsFilter);
            if (documentsResult.Succeeded)
            {
                Documents = documentsResult.Data.Items;
            }

            // Get folder statistics
            var statsResult = await _folderService.GetFolderStatsAsync(Folder.Id);
            if (statsResult.Succeeded)
            {
                var stats = statsResult.Data;
                TotalDocuments = stats.TotalDocumentCount;
                TotalSubfolders = stats.TotalSubfolderCount;
                TotalSize = stats.TotalFileSize;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = "An error occurred while loading the folder.";
            // Log the exception in production
        }

        return Page();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        try
        {
            var result = await _folderService.DeleteFolderAsync(id, User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "Folder deleted successfully.";
                return RedirectToPage("/Projects/Index");
            }
            else
            {
                TempData["ErrorMessage"] = result.Error ?? "Failed to delete folder.";
            }
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = "An error occurred while deleting the folder.";
            // Log the exception in production
        }

        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostArchiveAsync(int id)
    {
        try
        {
            var result = await _folderService.ArchiveFolderAsync(id, User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "Folder archived successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = result.Error ?? "Failed to archive folder.";
            }
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = "An error occurred while archiving the folder.";
            // Log the exception in production
        }

        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostRestoreAsync(int id)
    {
        try
        {
            var result = await _folderService.RestoreFolderAsync(id, User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "Folder restored successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = result.Error ?? "Failed to restore folder.";
            }
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = "An error occurred while restoring the folder.";
            // Log the exception in production
        }

        return RedirectToPage(new { id });
    }

    // Helper method to format file size
    public string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
} 