using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using DocFlowHub.Core.Models.Documents.Dto;
using DocFlowHub.Core.Models.Documents;
using DocFlowHub.Core.Models.Common;
using DocFlowHub.Core.Services.Interfaces;
using DocFlowHub.Web.Extensions;
using System.ComponentModel.DataAnnotations;

namespace DocFlowHub.Web.Pages.Documents
{
    [Authorize]
    public class EditModel : PageModel
    {
        private readonly IDocumentService _documentService;
        private readonly IDocumentCategoryService _categoryService;

        public EditModel(
            IDocumentService documentService,
            IDocumentCategoryService categoryService)
        {
            _documentService = documentService;
            _categoryService = categoryService;
        }

        public DocumentDto Document { get; set; } = null!;
        public IEnumerable<DocumentCategoryDto> AvailableCategories { get; set; } = new List<DocumentCategoryDto>();
        public IEnumerable<DocumentCategoryDto> CurrentCategories { get; set; } = new List<DocumentCategoryDto>();
        public string? ErrorMessage { get; set; }
        public string? SuccessMessage { get; set; }

        [BindProperty(SupportsGet = true)]
        public int Id { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "Title is required")]
        [StringLength(200, ErrorMessage = "Title cannot exceed 200 characters")]
        public string Title { get; set; } = string.Empty;

        [BindProperty]
        [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
        public string Description { get; set; } = string.Empty;

        [BindProperty]
        public List<int> SelectedCategoryIds { get; set; } = new();

        [BindProperty]
        public IFormFile? NewVersionFile { get; set; }

        [BindProperty]
        [StringLength(500, ErrorMessage = "Change summary cannot exceed 500 characters")]
        public string ChangeSummary { get; set; } = string.Empty;

        public async Task<IActionResult> OnGetAsync()
        {
            var userId = User.GetUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            // Load document details
            var documentResult = await _documentService.GetDocumentByIdAsync(Id);
            if (!documentResult.Succeeded)
            {
                ErrorMessage = documentResult.Error;
                return NotFound();
            }

            Document = documentResult.Data;

            // Verify user has permission to edit
            if (Document.OwnerId != userId && Document.TeamId == null)
            {
                return Forbid();
            }

            // Initialize form fields with current document data
            Title = Document.Title;
            Description = Document.Description;

            // Load available categories
            var categoriesResult = await _categoryService.GetAllCategoriesAsync();
            if (categoriesResult.Succeeded)
            {
                AvailableCategories = categoriesResult.Data;
            }

            // Load current document categories
            var currentCategoriesResult = await _categoryService.GetDocumentCategoriesAsync(Id);
            if (currentCategoriesResult.Succeeded)
            {
                CurrentCategories = currentCategoriesResult.Data;
                SelectedCategoryIds = CurrentCategories.Select(c => c.Id).ToList();
            }

            return Page();
        }

        public async Task<IActionResult> OnPostUpdateMetadataAsync()
        {
            // Only validate title and description for metadata updates
            ModelState.Remove(nameof(ChangeSummary));
            ModelState.Remove(nameof(NewVersionFile));
            
            if (!ModelState.IsValid)
            {
                await LoadPageDataAsync();
                return Page();
            }

            var userId = User.GetUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            // Verify document exists and user has permission
            var documentResult = await _documentService.GetDocumentByIdAsync(Id);
            if (!documentResult.Succeeded)
            {
                ErrorMessage = documentResult.Error;
                await LoadPageDataAsync();
                return Page();
            }

            var document = documentResult.Data;
            if (document.OwnerId != userId && document.TeamId == null)
            {
                return Forbid();
            }

            // Update document metadata
            var updateRequest = new UpdateDocumentRequest
            {
                Title = Title,
                Description = Description
            };

            var updateResult = await _documentService.UpdateDocumentAsync(Id, updateRequest);
            if (!updateResult.Succeeded)
            {
                ErrorMessage = updateResult.Error;
                await LoadPageDataAsync();
                return Page();
            }

            // Update categories
            var categoryUpdateResult = await _categoryService.AssignCategoriesToDocumentAsync(Id, SelectedCategoryIds);
            if (!categoryUpdateResult.Succeeded)
            {
                ErrorMessage = categoryUpdateResult.Error;
                await LoadPageDataAsync();
                return Page();
            }

            SuccessMessage = "Document metadata updated successfully!";
            await LoadPageDataAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostUploadNewVersionAsync()
        {
            // Only validate file upload fields for new version uploads
            ModelState.Remove(nameof(Title));
            ModelState.Remove(nameof(Description));
            ModelState.Remove(nameof(SelectedCategoryIds));
            
            if (NewVersionFile == null)
            {
                ModelState.AddModelError(nameof(NewVersionFile), "Please select a file to upload");
                await LoadPageDataAsync();
                return Page();
            }

            if (!ModelState.IsValid)
            {
                await LoadPageDataAsync();
                return Page();
            }

            var userId = User.GetUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            // Verify document exists and user has permission
            var documentResult = await _documentService.GetDocumentByIdAsync(Id);
            if (!documentResult.Succeeded)
            {
                ErrorMessage = documentResult.Error;
                await LoadPageDataAsync();
                return Page();
            }

            var document = documentResult.Data;
            if (document.OwnerId != userId && document.TeamId == null)
            {
                return Forbid();
            }

            // Upload new version
            var uploadRequest = new UploadVersionRequest
            {
                DocumentId = Id,
                File = NewVersionFile,
                ChangeSummary = ChangeSummary,
                UserId = userId
            };
            
            var uploadResult = await _documentService.UploadNewVersionAsync(uploadRequest);
            if (!uploadResult.Succeeded)
            {
                ErrorMessage = uploadResult.Error;
                await LoadPageDataAsync();
                return Page();
            }

            SuccessMessage = "New document version uploaded successfully!";
            
            // Clear form fields after successful upload
            ChangeSummary = string.Empty;
            NewVersionFile = null;
            
            await LoadPageDataAsync();
            return Page();
        }

        private async Task LoadPageDataAsync()
        {
            // Reload document details
            var documentResult = await _documentService.GetDocumentByIdAsync(Id);
            if (documentResult.Succeeded)
            {
                Document = documentResult.Data;
            }

            // Reload available categories
            var categoriesResult = await _categoryService.GetAllCategoriesAsync();
            if (categoriesResult.Succeeded)
            {
                AvailableCategories = categoriesResult.Data;
            }

            // Reload current document categories
            var currentCategoriesResult = await _categoryService.GetDocumentCategoriesAsync(Id);
            if (currentCategoriesResult.Succeeded)
            {
                CurrentCategories = currentCategoriesResult.Data;
            }
        }
    }
} 