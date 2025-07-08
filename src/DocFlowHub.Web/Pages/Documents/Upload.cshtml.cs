using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using DocFlowHub.Core.Models;
using DocFlowHub.Core.Models.Documents;
using DocFlowHub.Core.Models.Teams;
using DocFlowHub.Core.Models.Teams.Dto;
using DocFlowHub.Core.Models.Documents.Dto;
using DocFlowHub.Core.Models.AI;
using DocFlowHub.Core.Services.Interfaces;
using DocFlowHub.Web.Extensions;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using DocFlowHub.Core.Models.Projects.Dto;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace DocFlowHub.Web.Pages.Documents;

[Authorize]
public class UploadModel : PageModel
{
    private readonly IDocumentService _documentService;
    private readonly IDocumentCategoryService _categoryService;
    private readonly ITeamService _teamService;
    private readonly IAISettingsService _aiSettingsService;
    private readonly IDocumentSummaryService _documentSummaryService;
    private readonly IProjectService _projectService;
    private readonly IFolderService _folderService;

    public UploadModel(
        IDocumentService documentService,
        IDocumentCategoryService categoryService,
        ITeamService teamService,
        IAISettingsService aiSettingsService,
        IDocumentSummaryService documentSummaryService,
        IProjectService projectService,
        IFolderService folderService)
    {
        _documentService = documentService;
        _categoryService = categoryService;
        _teamService = teamService;
        _aiSettingsService = aiSettingsService;
        _documentSummaryService = documentSummaryService;
        _projectService = projectService;
        _folderService = folderService;
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

    [BindProperty]
    public int? ProjectId { get; set; }

    [BindProperty]
    public int? FolderId { get; set; }

    // AI Settings Properties
    [BindProperty]
    [Display(Name = "Generate AI Summary")]
    public bool GenerateAISummary { get; set; } = true;

    [BindProperty]
    [Display(Name = "AI Model")]
    public AIModel SelectedAIModel { get; set; } = AIModel.Gpt4oMini;

    [BindProperty]
    [Display(Name = "AI Quality")]
    [Range(0.1, 1.0, ErrorMessage = "Quality must be between 0.1 and 1.0")]
    public double AIQuality { get; set; } = 0.7;

    public IEnumerable<DocumentCategoryDto> Categories { get; set; } = new List<DocumentCategoryDto>();
    public IEnumerable<TeamDto> Teams { get; set; } = new List<TeamDto>();
    public AISettings? UserAISettings { get; set; }
    public bool AIFeaturesEnabled { get; set; }
    public List<AIModelOption> AvailableAIModels { get; set; } = new List<AIModelOption>();
    public string? ErrorMessage { get; set; }
    public IEnumerable<ProjectDto> Projects { get; set; } = new List<ProjectDto>();
    public IEnumerable<FolderDto> Folders { get; set; } = new List<FolderDto>();

    // Cost Estimation Properties
    public string EstimatedCost => GetEstimatedCost();
    public string EstimatedProcessingTime => GetEstimatedProcessingTime();
    public string ModelCapabilities => GetModelCapabilities();

    public SelectList ProjectSelectList => new SelectList(Projects, "Id", "Name", ProjectId);
    public SelectList FolderSelectList => new SelectList(Folders, "Id", "Name", FolderId);

    public class AIModelOption
    {
        public AIModel Model { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsRecommended { get; set; }
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var userId = User.GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return RedirectToPage("/Account/Login");
        }

        // Load categories
        var categoriesResult = await _categoryService.GetAllCategoriesAsync();
        if (!categoriesResult.Succeeded)
        {
            ErrorMessage = categoriesResult.Error;
            return Page();
        }
        Categories = categoriesResult.Data;

        // Load teams
        var teamsResult = await _teamService.GetUserTeamsAsync(userId, new TeamFilter());
        if (!teamsResult.Succeeded)
        {
            ErrorMessage = teamsResult.Error;
            return Page();
        }
        Teams = teamsResult.Data.Items;

        // Load projects
        var projectsResult = await _projectService.GetActiveProjectsForUserAsync(userId);
        Projects = projectsResult.Succeeded ? projectsResult.Data : new List<ProjectDto>();
        if (Projects.Any())
        {
            if (!ProjectId.HasValue)
                ProjectId = Projects.First().Id;

            await LoadFoldersAsync(userId);
        }

        // Load AI settings and set defaults
        await LoadAISettingsAsync(userId);

        // Load available AI models
        LoadAvailableAIModels();

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
            await ReloadPageDataAsync(userId);
            return Page();
        }

        var request = new CreateDocumentRequest
        {
            Title = Title,
            Description = Description,
            CategoryIds = SelectedCategoryIds,
            TeamId = TeamId,
            ProjectId = ProjectId,
            FolderId = FolderId,
            OwnerId = userId
        };

        var result = await _documentService.CreateDocumentAsync(request, File);
        if (!result.Succeeded)
        {
            ErrorMessage = result.Error;
            await ReloadPageDataAsync(userId);
            return Page();
        }

        // Handle AI processing based on user preferences
        await HandleAIProcessingAsync(result.Data.Id, userId);

        // Update user's upload preferences for next time
        await UpdateUserUploadPreferencesAsync(userId);

        return RedirectToPage("./Index");
    }

    private async Task HandleAIProcessingAsync(int documentId, string userId)
    {
        try
        {
            // Only process AI features if user has them enabled and requested
            if (!AIFeaturesEnabled || !GenerateAISummary)
            {
                return;
            }

            // Update user's AI settings to reflect their choices (for next time)
            if (UserAISettings != null)
            {
                // Update settings to match user's current choices
                await _aiSettingsService.UpdateSettingAsync(userId, nameof(AISettings.PreferredModel), SelectedAIModel);
                await _aiSettingsService.UpdateSettingAsync(userId, nameof(AISettings.QualityPreference), AIQuality);
                
                // Note: We don't update AutoSummarizeOnUpload here as that's a persistent preference
                // GenerateAISummary is the per-upload choice
            }

            // Trigger AI summarization manually for this specific upload
            // This ensures it happens with the user's chosen settings
            _ = Task.Run(async () =>
            {
                try
                {
                    // Give the document creation a moment to fully complete
                    await Task.Delay(1000);
                    
                    // Generate AI summary with user's preferred settings
                    var summaryResult = await _documentSummaryService.RegenerateSummaryAsync(documentId);
                    if (!summaryResult.Succeeded)
                    {
                        // Log the error but don't fail the upload
                        System.Diagnostics.Debug.WriteLine($"AI summary generation failed for document {documentId}: {summaryResult.Error}");
                    }
                }
                catch (Exception ex)
                {
                    // Log error but don't fail the upload
                    System.Diagnostics.Debug.WriteLine($"AI processing error for document {documentId}: {ex.Message}");
                }
            });
        }
        catch (Exception ex)
        {
            // Log error but don't fail the upload
            // AI processing failure shouldn't break the document upload
        }
    }

    private async Task UpdateUserUploadPreferencesAsync(string userId)
    {
        try
        {
            if (UserAISettings == null || !AIFeaturesEnabled)
            {
                return;
            }

            // Remember user's choices for future uploads
            var updateNeeded = false;

            // Update preferred model if changed
            if (UserAISettings.PreferredModel != SelectedAIModel)
            {
                await _aiSettingsService.UpdateSettingAsync(userId, nameof(AISettings.PreferredModel), SelectedAIModel);
                updateNeeded = true;
            }

            // Update quality preference if changed
            if (Math.Abs(UserAISettings.QualityPreference - AIQuality) > 0.01)
            {
                await _aiSettingsService.UpdateSettingAsync(userId, nameof(AISettings.QualityPreference), AIQuality);
                updateNeeded = true;
            }

            if (updateNeeded)
            {
                // Reload settings for next upload
                UserAISettings = (await _aiSettingsService.GetUserSettingsAsync(userId)).Data;
            }
        }
        catch (Exception ex)
        {
            // Log error but don't fail the upload
            // Preference updates are nice-to-have, not critical
        }
    }

    private async Task ReloadPageDataAsync(string userId)
    {
        // Reload categories
        var categoriesResult = await _categoryService.GetAllCategoriesAsync();
        if (categoriesResult.Succeeded)
        {
            Categories = categoriesResult.Data;
        }

        // Reload teams
        var teamsResult = await _teamService.GetUserTeamsAsync(userId, new TeamFilter());
        if (teamsResult.Succeeded)
        {
            Teams = teamsResult.Data.Items;
        }

        // Reload AI settings
        await LoadAISettingsAsync(userId);

        // Reload available AI models
        LoadAvailableAIModels();

        // Reload projects
        var projectsResult = await _projectService.GetActiveProjectsForUserAsync(userId);
        Projects = projectsResult.Succeeded ? projectsResult.Data : new List<ProjectDto>();
        if (Projects.Any())
        {
            if (!ProjectId.HasValue)
                ProjectId = Projects.First().Id;

            await LoadFoldersAsync(userId);
        }
    }

    private async Task LoadAISettingsAsync(string userId)
    {
        try
        {
            // Check if AI features are enabled for this user
            var featuresEnabledResult = await _aiSettingsService.AreAIFeaturesEnabledAsync(userId);
            AIFeaturesEnabled = featuresEnabledResult.Succeeded && featuresEnabledResult.Data;

            if (AIFeaturesEnabled)
            {
                // Load user's AI settings
                var settingsResult = await _aiSettingsService.GetUserSettingsAsync(userId);
                if (settingsResult.Succeeded && settingsResult.Data != null)
                {
                    UserAISettings = settingsResult.Data;
                    
                    // Set defaults from user settings
                    GenerateAISummary = settingsResult.Data.SummarizationEnabled && settingsResult.Data.AutoSummarizeOnUpload;
                    SelectedAIModel = settingsResult.Data.PreferredModel;
                    AIQuality = settingsResult.Data.QualityPreference;
                }
                else
                {
                    // User doesn't have settings yet - create them with defaults
                    var defaultResult = await _aiSettingsService.CreateDefaultSettingsAsync(userId);
                    if (defaultResult.Succeeded)
                    {
                        // Reload the newly created settings
                        var newSettingsResult = await _aiSettingsService.GetUserSettingsAsync(userId);
                        if (newSettingsResult.Succeeded && newSettingsResult.Data != null)
                        {
                            UserAISettings = newSettingsResult.Data;
                            GenerateAISummary = newSettingsResult.Data.AutoSummarizeOnUpload;
                            SelectedAIModel = newSettingsResult.Data.PreferredModel;
                            AIQuality = newSettingsResult.Data.QualityPreference;
                        }
                        else
                        {
                            // Fallback to hardcoded defaults
                            SetFallbackDefaults();
                        }
                    }
                    else
                    {
                        // Failed to create settings, use fallback
                        SetFallbackDefaults();
                    }
                }
            }
            else
            {
                // AI features disabled, set conservative defaults
                SetFallbackDefaults();
                GenerateAISummary = false; // Override to disabled when AI features are off
            }
        }
        catch (Exception)
        {
            // Graceful fallback if AI settings fail
            AIFeaturesEnabled = false;
            SetFallbackDefaults();
            GenerateAISummary = false;
        }
    }

    private void SetFallbackDefaults()
    {
        GenerateAISummary = true; // Default to enabled (will be overridden if AI features disabled)
        SelectedAIModel = AIModel.Gpt4oMini; // Most cost-effective option
        AIQuality = 0.7; // Balanced quality setting
    }

    private void LoadAvailableAIModels()
    {
        AvailableAIModels = AIModelHelper.GetAllModels()
            .Select(model => new AIModelOption
            {
                Model = model,
                Name = model.ToDisplayName(),
                Description = model.GetCostDescription(),
                IsRecommended = model == AIModel.Gpt4oMini || model == AIModel.Gpt4o
            })
            .ToList();
    }

    private string GetEstimatedCost()
    {
        if (!GenerateAISummary || !AIFeaturesEnabled)
        {
            return "No AI processing - Free";
        }

        // Estimate based on average document size and selected model
        var estimatedTokens = GetEstimatedTokens();
        var costPerToken = GetCostPerToken(SelectedAIModel);
        var estimatedCost = estimatedTokens * costPerToken;

        if (estimatedCost < 0.01)
        {
            return "< $0.01";
        }
        else if (estimatedCost < 0.10)
        {
            return $"≈ ${estimatedCost:F3}";
        }
        else
        {
            return $"≈ ${estimatedCost:F2}";
        }
    }

    private string GetEstimatedProcessingTime()
    {
        if (!GenerateAISummary || !AIFeaturesEnabled)
        {
            return "No AI processing";
        }

        // Estimate processing time based on model and quality settings
        var baseTime = GetBaseProcessingTime(SelectedAIModel);
        var qualityMultiplier = AIQuality switch
        {
            <= 0.3 => 0.7, // Speed optimized
            <= 0.7 => 1.0, // Balanced
            _ => 1.4 // Quality optimized
        };

        var estimatedSeconds = baseTime * qualityMultiplier;

        if (estimatedSeconds < 10)
        {
            return $"≈ {estimatedSeconds:F0} seconds";
        }
        else if (estimatedSeconds < 60)
        {
            return $"≈ {estimatedSeconds:F0} seconds";
        }
        else
        {
            return $"≈ {estimatedSeconds/60:F1} minutes";
        }
    }

    private string GetModelCapabilities()
    {
        return SelectedAIModel switch
        {
            AIModel.Gpt4oMini => "Fast, cost-effective, good for basic summaries",
            AIModel.Gpt4o => "Balanced speed and quality, excellent for most documents",
            AIModel.Gpt41Mini => "High quality analysis, ideal for important documents",
            AIModel.Gpt41 => "Highest quality insights, best for complex or critical content",
            _ => "AI-powered document analysis"
        };
    }

    private int GetEstimatedTokens()
    {
        // Rough estimation - in reality this would depend on file size and content
        // For now, use conservative estimates based on typical document processing
        return UserAISettings?.MaxTokensPerOperation ?? 2000;
    }

    private double GetCostPerToken(AIModel model)
    {
        // Rough cost estimates per 1000 tokens (as of 2024)
        // These would ideally come from a configuration or real-time pricing API
        return model switch
        {
            AIModel.Gpt4oMini => 0.00015 / 1000, // $0.15 per 1M tokens
            AIModel.Gpt4o => 0.005 / 1000,       // $5.00 per 1M tokens
            AIModel.Gpt41Mini => 0.003 / 1000,   // $3.00 per 1M tokens
            AIModel.Gpt41 => 0.01 / 1000,        // $10.00 per 1M tokens
            _ => 0.001 / 1000                     // Default fallback
        };
    }

    private double GetBaseProcessingTime(AIModel model)
    {
        // Estimated base processing time in seconds
        return model switch
        {
            AIModel.Gpt4oMini => 8,   // Fast model
            AIModel.Gpt4o => 12,      // Balanced model
            AIModel.Gpt41Mini => 18,  // Higher quality, slower
            AIModel.Gpt41 => 25,      // Highest quality, slowest
            _ => 15                   // Default
        };
    }

    private async Task LoadFoldersAsync(string userId)
    {
        if (!ProjectId.HasValue)
        {
            Folders = new List<FolderDto>();
            return;
        }
        var foldersResult = await _folderService.GetProjectFoldersAsync(ProjectId.Value, userId);
        if (!foldersResult.Succeeded)
        {
            Folders = new List<FolderDto>();
            return;
        }
        // Flatten hierarchy for dropdown display
        var flattened = new List<FolderDto>();
        void Flatten(IEnumerable<FolderDto> list, int depth)
        {
            foreach (var f in list.OrderBy(x => x.Name))
            {
                var clone = new FolderDto
                {
                    Id = f.Id,
                    Name = new string('›', depth) + " " + f.Name // use chevron character for indentation
                };
                flattened.Add(clone);
                if (f.Children?.Any() == true)
                    Flatten(f.Children, depth + 1);
            }
        }
        Flatten(foldersResult.Data, 0);
        Folders = flattened;
    }

    public async Task<JsonResult> OnGetFoldersAsync(int projectId)
    {
        var userId = User.GetUserId();
        if(string.IsNullOrEmpty(userId))
            return new JsonResult(new { success=false, error="User not authenticated"});

        var result = await _folderService.GetProjectFoldersAsync(projectId, userId);
        if(!result.Succeeded)
            return new JsonResult(new { success=false, error=result.Error});

        return new JsonResult(new { success=true, folders=result.Data.Select(f=> new {f.Id, f.Name})});
    }
} 