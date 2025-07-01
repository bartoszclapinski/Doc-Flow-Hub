using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using DocFlowHub.Core.Models.Documents.Dto;
using DocFlowHub.Core.Models.Documents;
using DocFlowHub.Core.Models.Common;
using DocFlowHub.Core.Models.Teams;
using DocFlowHub.Core.Models.Teams.Dto;
using DocFlowHub.Core.Models.AI;
using DocFlowHub.Core.Services.Interfaces;
using DocFlowHub.Web.Extensions;

namespace DocFlowHub.Web.Pages.Documents
{
    [Authorize]
    public class DetailsModel : PageModel
    {
        private readonly IDocumentService _documentService;
        private readonly IDocumentCategoryService _categoryService;
        private readonly ITeamService _teamService;
        private readonly IVersionComparisonService _versionComparisonService;
        private readonly IAISettingsService _aiSettingsService;

        public DetailsModel(
            IDocumentService documentService,
            IDocumentCategoryService categoryService,
            ITeamService teamService,
            IVersionComparisonService versionComparisonService,
            IAISettingsService aiSettingsService)
        {
            _documentService = documentService;
            _categoryService = categoryService;
            _teamService = teamService;
            _versionComparisonService = versionComparisonService;
            _aiSettingsService = aiSettingsService;
        }

        public DocumentDto Document { get; set; } = null!;
        public PagedResult<DocumentVersionDto> Versions { get; set; } = new();
        public IEnumerable<DocumentCategoryDto> Categories { get; set; } = new List<DocumentCategoryDto>();
        public IEnumerable<TeamDto> UserTeams { get; set; } = new List<TeamDto>();
        public string? ErrorMessage { get; set; }
        public string? SuccessMessage { get; set; }

        [BindProperty(SupportsGet = true)]
        public int Id { get; set; }

        [BindProperty(SupportsGet = true)]
        public int PageNumber { get; set; } = 1;

        [BindProperty]
        public int? ShareWithTeamId { get; set; }

        // Version Comparison Properties
        [BindProperty]
        public int? FromVersionNumber { get; set; }

        [BindProperty]
        public int? ToVersionNumber { get; set; }

        [BindProperty]
        public string? ComparisonAIModel { get; set; }

        [BindProperty]
        public string? ComparisonQuality { get; set; }

        public VersionComparison? ComparisonResult { get; set; }
        public bool IsComparing { get; set; }
        public AISettings? UserAISettings { get; set; }
        public IEnumerable<(string ApiString, string DisplayName)> AvailableAIModels { get; set; } = new List<(string, string)>();

        public async Task<IActionResult> OnGetAsync()
        {
            var userId = User.GetUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var documentResult = await _documentService.GetDocumentByIdAsync(Id);
            if (!documentResult.Succeeded)
            {
                ErrorMessage = documentResult.Error;
                return NotFound();
            }

            Document = documentResult.Data;

            // Verify user has access to this document using secure method
            var accessResult = await _documentService.CanUserAccessDocumentAsync(Id, userId);
            if (!accessResult.Succeeded || !accessResult.Data)
            {
                return Forbid();
            }

            // Load version history
            var versionsResult = await _documentService.GetDocumentVersionsAsync(Id, new DocumentFilter 
            { 
                PageNumber = PageNumber, 
                PageSize = 10 
            });
            
            if (versionsResult.Succeeded)
            {
                Versions = versionsResult.Data;
            }

            // Load categories
            var categoriesResult = await _categoryService.GetDocumentCategoriesAsync(Id);
            if (categoriesResult.Succeeded)
            {
                Categories = categoriesResult.Data;
            }

            // Load user teams for sharing
            var teamsResult = await _teamService.GetUserTeamsAsync(userId, new TeamFilter());
            if (teamsResult.Succeeded)
            {
                UserTeams = teamsResult.Data.Items;
            }

            // Load user AI settings for version comparison
            var aiSettingsResult = await _aiSettingsService.GetUserSettingsAsync(userId);
            if (aiSettingsResult.Succeeded)
            {
                UserAISettings = aiSettingsResult.Data;
            }

            // Load available AI models for dropdown
            AvailableAIModels = AIModelHelper.GetAllModels()
                .Select(m => (m.ToApiString(), m.ToDisplayName()))
                .ToList();

            return Page();
        }

        public async Task<IActionResult> OnPostDownloadAsync(int versionId)
        {
            var userId = User.GetUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            // Verify user has access to this document using secure method
            var accessResult = await _documentService.CanUserAccessDocumentAsync(Id, userId);
            if (!accessResult.Succeeded || !accessResult.Data)
            {
                return Forbid();
            }

            // Get version details
            var versionResult = await _documentService.GetDocumentVersionAsync(Id, versionId);
            if (!versionResult.Succeeded)
            {
                ErrorMessage = "Version not found";
                return Page();
            }

            var version = versionResult.Data;

            // Get document details for filename
            var documentResult = await _documentService.GetDocumentByIdAsync(Id);
            if (!documentResult.Succeeded)
            {
                ErrorMessage = documentResult.Error;
                return Page();
            }

            var document = documentResult.Data;

            // Download the file
            var downloadResult = await _documentService.DownloadDocumentVersionAsync(Id, versionId);
            if (!downloadResult.Succeeded)
            {
                ErrorMessage = downloadResult.Error;
                return Page();
            }

            var contentType = GetContentType(version.FileType);
            var fileName = $"{document.Title}_v{version.VersionNumber}{version.FileType}";

            return File(downloadResult.Data, contentType, fileName);
        }

        public async Task<IActionResult> OnPostDeleteAsync()
        {
            var userId = User.GetUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            // Get document details first to check ownership
            var documentResult = await _documentService.GetDocumentByIdAsync(Id);
            if (!documentResult.Succeeded)
            {
                ErrorMessage = documentResult.Error;
                return Page();
            }

            var document = documentResult.Data;

            // Verify user owns this document (only owners can delete)
            if (document.OwnerId != userId)
            {
                ErrorMessage = "You can only delete documents that you own.";
                return Page();
            }

            // Delete the document
            var deleteResult = await _documentService.DeleteDocumentAsync(Id);
            if (!deleteResult.Succeeded)
            {
                ErrorMessage = deleteResult.Error;
                return Page();
            }

            // Redirect to documents index with success message
            TempData["SuccessMessage"] = $"Document '{document.Title}' has been successfully deleted.";
            return RedirectToPage("./Index");
        }

        public async Task<IActionResult> OnPostDeleteVersionAsync(int versionId)
        {
            var userId = User.GetUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            // Get document details first to check ownership
            var documentResult = await _documentService.GetDocumentByIdAsync(Id);
            if (!documentResult.Succeeded)
            {
                ErrorMessage = documentResult.Error;
                await LoadPageDataAsync(userId);
                return Page();
            }

            var document = documentResult.Data;

            // Verify user owns this document (only owners can delete versions)
            if (document.OwnerId != userId)
            {
                ErrorMessage = "You can only delete versions of documents that you own.";
                await LoadPageDataAsync(userId);
                return Page();
            }

            // Prevent deletion of current version
            if (versionId == document.CurrentVersionId)
            {
                ErrorMessage = "Cannot delete the current active version of the document.";
                await LoadPageDataAsync(userId);
                return Page();
            }

            // Delete the version using the service
            var deleteResult = await _documentService.DeleteDocumentVersionAsync(Id, versionId, userId);
            if (!deleteResult.Succeeded)
            {
                ErrorMessage = deleteResult.Error;
                await LoadPageDataAsync(userId);
                return Page();
            }
            
            SuccessMessage = "Document version has been successfully deleted.";
            await LoadPageDataAsync(userId);
            return Page();
        }

        private static string GetContentType(string fileExtension)
        {
            return fileExtension.ToLower() switch
            {
                ".pdf" => "application/pdf",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".md" => "text/markdown",
                ".txt" => "text/plain",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                _ => "application/octet-stream"
            };
        }

        public async Task<IActionResult> OnPostShareWithTeamAsync()
        {
            var userId = User.GetUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            // Verify user owns this document (only owners can share)
            var documentResult = await _documentService.GetDocumentByIdAsync(Id);
            if (!documentResult.Succeeded)
            {
                ErrorMessage = documentResult.Error;
                await LoadPageDataAsync(userId);
                return Page();
            }

            var document = documentResult.Data;
            if (document.OwnerId != userId)
            {
                ErrorMessage = "Only document owners can share documents with teams";
                await LoadPageDataAsync(userId);
                return Page();
            }

            if (!ShareWithTeamId.HasValue)
            {
                ErrorMessage = "Please select a team to share with";
                await LoadPageDataAsync(userId);
                return Page();
            }

            // Validate that the user is a member of the selected team
            var userTeamsResult = await _teamService.GetUserTeamsAsync(userId, new TeamFilter());
            if (!userTeamsResult.Succeeded || !userTeamsResult.Data.Items.Any(t => t.Id == ShareWithTeamId.Value))
            {
                ErrorMessage = "You can only share documents with teams you are a member of";
                await LoadPageDataAsync(userId);
                return Page();
            }

            var shareResult = await _documentService.ShareDocumentWithTeamAsync(Id, ShareWithTeamId.Value);
            if (shareResult.Succeeded)
            {
                SuccessMessage = "Document successfully shared with the team!";
            }
            else
            {
                ErrorMessage = shareResult.Error;
            }

            return RedirectToPage(new { id = Id });
        }

        public async Task<IActionResult> OnPostUnshareFromTeamAsync()
        {
            var userId = User.GetUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            // Verify user owns this document (only owners can unshare)
            var documentResult = await _documentService.GetDocumentByIdAsync(Id);
            if (!documentResult.Succeeded)
            {
                ErrorMessage = documentResult.Error;
                await LoadPageDataAsync(userId);
                return Page();
            }

            var document = documentResult.Data;
            if (document.OwnerId != userId)
            {
                ErrorMessage = "Only document owners can unshare documents from teams";
                await LoadPageDataAsync(userId);
                return Page();
            }

            var unshareResult = await _documentService.UnshareDocumentFromTeamAsync(Id);
            if (unshareResult.Succeeded)
            {
                SuccessMessage = "Document successfully unshared from the team!";
            }
            else
            {
                ErrorMessage = unshareResult.Error;
            }

            return RedirectToPage(new { id = Id });
        }

        public async Task<IActionResult> OnPostCompareVersionsAsync()
        {
            var userId = User.GetUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            // Verify user has access to this document
            var accessResult = await _documentService.CanUserAccessDocumentAsync(Id, userId);
            if (!accessResult.Succeeded || !accessResult.Data)
            {
                return Forbid();
            }

            // Validate version selection
            if (!FromVersionNumber.HasValue || !ToVersionNumber.HasValue)
            {
                ErrorMessage = "Please select two versions to compare";
                await LoadPageDataAsync(userId);
                return Page();
            }

            if (FromVersionNumber.Value == ToVersionNumber.Value)
            {
                ErrorMessage = "Please select two different versions to compare";
                await LoadPageDataAsync(userId);
                return Page();
            }

            try
            {
                IsComparing = true;
                
                // Ensure we compare from older to newer version for consistency
                var fromVersion = Math.Min(FromVersionNumber.Value, ToVersionNumber.Value);
                var toVersion = Math.Max(FromVersionNumber.Value, ToVersionNumber.Value);

                // Determine AI model to use - priority: selected model > user preference > default
                var aiModel = GetSelectedAIModel();

                // Generate AI comparison with usage tracking and selected model
                var comparisonResult = await _versionComparisonService.CompareDocumentVersionsAsync(Id, fromVersion, toVersion, aiModel, userId);
                
                if (comparisonResult.Succeeded && comparisonResult.Data != null)
                {
                    ComparisonResult = comparisonResult.Data;
                    var modelDisplayName = aiModel switch
                    {
                        AIModel.Gpt41 => "GPT-4.1",
                        AIModel.Gpt41Mini => "GPT-4.1 Mini", 
                        AIModel.Gpt4o => "GPT-4o",
                        AIModel.Gpt4oMini => "GPT-4o Mini",
                        _ => "GPT-4o Mini"
                    };
                    SuccessMessage = $"Successfully compared version {fromVersion} to version {toVersion} using {modelDisplayName}";
                }
                else
                {
                    ErrorMessage = comparisonResult.Error ?? "Failed to generate version comparison. Please try again.";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error comparing versions: {ex.Message}";
            }
            finally
            {
                IsComparing = false;
            }

            await LoadPageDataAsync(userId);
            return Page();
        }

        private async Task LoadPageDataAsync(string userId)
        {
            // Load document data
            var documentResult = await _documentService.GetDocumentByIdAsync(Id);
            if (documentResult.Succeeded)
            {
                Document = documentResult.Data;
            }

            // Load version history
            var versionsResult = await _documentService.GetDocumentVersionsAsync(Id, new DocumentFilter 
            { 
                PageNumber = PageNumber, 
                PageSize = 10 
            });
            
            if (versionsResult.Succeeded)
            {
                Versions = versionsResult.Data;
            }

            // Load categories
            var categoriesResult = await _categoryService.GetDocumentCategoriesAsync(Id);
            if (categoriesResult.Succeeded)
            {
                Categories = categoriesResult.Data;
            }

            // Load user teams for sharing
            var teamsResult = await _teamService.GetUserTeamsAsync(userId, new TeamFilter());
            if (teamsResult.Succeeded)
            {
                UserTeams = teamsResult.Data.Items;
            }

            // Load user AI settings
            var aiSettingsResult = await _aiSettingsService.GetUserSettingsAsync(userId);
            if (aiSettingsResult.Succeeded)
            {
                UserAISettings = aiSettingsResult.Data;
            }

            // Load available AI models for dropdown
            AvailableAIModels = AIModelHelper.GetAllModels()
                .Select(m => (m.ToApiString(), m.ToDisplayName()))
                .ToList();
        }

        /// <summary>
        /// Helper method to determine which AI model to use for comparison
        /// Priority: 1) User selection 2) User preferences 3) Default
        /// </summary>
        private AIModel GetSelectedAIModel()
        {
            // If user explicitly selected a model, use that
            if (!string.IsNullOrEmpty(ComparisonAIModel))
            {
                return ComparisonAIModel.ToLower() switch
                {
                    "gpt-4o" => AIModel.Gpt4o,
                    "gpt-4o-mini" => AIModel.Gpt4oMini,
                    "gpt-4-turbo" => AIModel.Gpt4o, // Map turbo to 4o for now
                    "gpt-4.1" => AIModel.Gpt41,
                    "gpt-4.1-mini" => AIModel.Gpt41Mini,
                    _ => GetUserPreferredModel()
                };
            }

            return GetUserPreferredModel();
        }

        /// <summary>
        /// Get the user's preferred AI model from their settings
        /// </summary>
        private AIModel GetUserPreferredModel()
        {
            // PreferredModel is already an AIModel enum, so just return it directly
            return UserAISettings?.PreferredModel ?? AIModel.Gpt4oMini;
        }

        /// <summary>
        /// Get display string for the user's preferred model (for view)
        /// </summary>
        public string GetPreferredModelDisplay()
        {
            var preferredModel = UserAISettings?.PreferredModel ?? AIModel.Gpt4oMini;
            return preferredModel switch
            {
                AIModel.Gpt41 => "gpt-4.1",
                AIModel.Gpt41Mini => "gpt-4.1-mini",
                AIModel.Gpt4o => "gpt-4o",
                AIModel.Gpt4oMini => "gpt-4o-mini",
                _ => "gpt-4o-mini"
            };
        }
    }
} 