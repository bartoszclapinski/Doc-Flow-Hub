using DocFlowHub.Core.Models.AI;
using DocFlowHub.Core.Services.Interfaces;
using DocFlowHub.Web.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace DocFlowHub.Web.Pages.Settings;

[Authorize]
public class AIModel : PageModel
{
    private readonly IAISettingsService _aiSettingsService;
    private readonly IAIUsageTrackingService _usageTrackingService;

    public AIModel(IAISettingsService aiSettingsService, IAIUsageTrackingService usageTrackingService)
    {
        _aiSettingsService = aiSettingsService;
        _usageTrackingService = usageTrackingService;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public AISettings? CurrentSettings { get; set; }
    public UserUsageStatistics? UsageStatistics { get; set; }
    public string? ErrorMessage { get; set; }
    public string? TestResult { get; set; }

    public class InputModel
    {
        [Display(Name = "Preferred AI Model")]
        public Core.Models.AI.AIModel PreferredModel { get; set; } = Core.Models.AI.AIModel.Gpt4oMini;

        [Display(Name = "Custom OpenAI API Key")]
        [StringLength(500, ErrorMessage = "API key cannot exceed 500 characters")]
        public string? CustomApiKey { get; set; }

        [Display(Name = "Use Custom API Key")]
        public bool UseCustomApiKey { get; set; }

        [Display(Name = "Enable AI Summarization")]
        public bool SummarizationEnabled { get; set; } = true;

        [Display(Name = "Enable Version Comparison")]
        public bool VersionComparisonEnabled { get; set; } = true;

        [Display(Name = "Auto-generate summaries on upload")]
        public bool AutoSummarizeOnUpload { get; set; } = true;

        [Display(Name = "Quality Preference")]
        [Range(0.1, 1.0, ErrorMessage = "Quality must be between 0.1 and 1.0")]
        public double QualityPreference { get; set; } = 0.7;

        [Display(Name = "Max Tokens per Operation")]
        [Range(100, 10000, ErrorMessage = "Token limit must be between 100 and 10000")]
        public int MaxTokensPerOperation { get; set; } = 2000;

        [Display(Name = "Comparison Sensitivity")]
        [Range(0.1, 1.0, ErrorMessage = "Sensitivity must be between 0.1 and 1.0")]
        public double ComparisonSensitivity { get; set; } = 0.5;

        [Display(Name = "Cache Duration (hours)")]
        [Range(1, 48, ErrorMessage = "Cache duration must be between 1 and 48 hours")]
        public int CacheDurationHours { get; set; } = 24;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var userId = User.GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return NotFound("User not found.");
        }

        try
        {
            // Load AI settings
            var settingsResult = await _aiSettingsService.GetUserSettingsAsync(userId);
            if (settingsResult.Succeeded && settingsResult.Data != null)
            {
                CurrentSettings = settingsResult.Data;
                Input = new InputModel
                {
                    PreferredModel = settingsResult.Data.PreferredModel,
                    CustomApiKey = settingsResult.Data.CustomApiKey,
                    UseCustomApiKey = settingsResult.Data.UseCustomApiKey,
                    SummarizationEnabled = settingsResult.Data.SummarizationEnabled,
                    VersionComparisonEnabled = settingsResult.Data.VersionComparisonEnabled,
                    AutoSummarizeOnUpload = settingsResult.Data.AutoSummarizeOnUpload,
                    QualityPreference = settingsResult.Data.QualityPreference,
                    MaxTokensPerOperation = settingsResult.Data.MaxTokensPerOperation,
                    ComparisonSensitivity = settingsResult.Data.ComparisonSensitivity,
                    CacheDurationHours = settingsResult.Data.CacheDurationHours
                };
            }
            else
            {
                ErrorMessage = settingsResult.Error ?? "Unable to load AI settings.";
            }

            // Load usage statistics
            var usageResult = await _usageTrackingService.GetUserUsageStatisticsAsync(userId, DateTime.UtcNow.AddDays(-30));
            if (usageResult.Succeeded && usageResult.Data != null)
            {
                UsageStatistics = usageResult.Data;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error loading settings: {ex.Message}";
        }

        return Page();
    }

    public async Task<IActionResult> OnPostSaveSettingsAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadCurrentSettingsAsync();
            return Page();
        }

        var userId = User.GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return NotFound("User not found.");
        }

        try
        {
            var settings = new AISettings
            {
                UserId = userId,
                PreferredModel = Input.PreferredModel,
                CustomApiKey = Input.CustomApiKey,
                UseCustomApiKey = Input.UseCustomApiKey,
                SummarizationEnabled = Input.SummarizationEnabled,
                VersionComparisonEnabled = Input.VersionComparisonEnabled,
                AutoSummarizeOnUpload = Input.AutoSummarizeOnUpload,
                QualityPreference = Input.QualityPreference,
                MaxTokensPerOperation = Input.MaxTokensPerOperation,
                ComparisonSensitivity = Input.ComparisonSensitivity,
                CacheDurationHours = Input.CacheDurationHours,
                IsActive = true,
                UpdatedAt = DateTime.UtcNow
            };

            var result = await _aiSettingsService.UpdateUserSettingsAsync(userId, settings);
            if (result.Succeeded)
            {
                TempData["StatusMessage"] = "AI settings have been saved successfully.";
                return RedirectToPage();
            }
            else
            {
                ErrorMessage = result.Error ?? "Failed to save settings.";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error saving settings: {ex.Message}";
        }

        await LoadCurrentSettingsAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostTestApiKeyAsync()
    {
        var userId = User.GetUserId();
        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(Input.CustomApiKey))
        {
            return new JsonResult(new { 
                success = false, 
                message = "Invalid API key or user.",
                details = "Please provide a valid API key starting with 'sk-'"
            });
        }

        // Basic format validation
        if (!Input.CustomApiKey.StartsWith("sk-") || Input.CustomApiKey.Length < 20)
        {
            return new JsonResult(new { 
                success = false, 
                message = "Invalid API key format.",
                details = "OpenAI API keys should start with 'sk-' and be at least 20 characters long."
            });
        }

        try
        {
            var result = await _aiSettingsService.ValidateApiKeyAsync(Input.CustomApiKey);
            if (result.Succeeded && result.Data)
            {
                // Get available models from the actual enum
                var availableModels = GetAvailableModels()
                    .Select(m => new { 
                        name = m.Model.ToApiString(), 
                        displayName = m.Name, 
                        description = m.Description,
                        isRecommended = m.Model == Core.Models.AI.AIModel.Gpt4oMini || m.Model == Core.Models.AI.AIModel.Gpt4o
                    })
                    .ToList();

                return new JsonResult(new { 
                    success = true, 
                    message = "API key is valid and working!",
                    details = "Connection to OpenAI established successfully.",
                    connectionStatus = "Connected",
                    modelCount = availableModels.Count,
                    availableModels = availableModels,
                    timestamp = DateTime.UtcNow.ToString("HH:mm:ss")
                });
            }
            else
            {
                return new JsonResult(new { 
                    success = false, 
                    message = result.Error ?? "API key validation failed.",
                    details = "Please verify your API key and ensure it has proper permissions.",
                    connectionStatus = "Failed"
                });
            }
        }
        catch (Exception ex)
        {
            return new JsonResult(new { 
                success = false, 
                message = $"Connection error: {ex.Message}",
                details = "Unable to connect to OpenAI API. Please check your internet connection and API key.",
                connectionStatus = "Error"
            });
        }
    }

    public async Task<IActionResult> OnPostResetToDefaultsAsync()
    {
        var userId = User.GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return NotFound("User not found.");
        }

        try
        {
            var result = await _aiSettingsService.ResetToDefaultsAsync(userId);
            if (result.Succeeded)
            {
                TempData["StatusMessage"] = "AI settings have been reset to defaults.";
                return RedirectToPage();
            }
            else
            {
                ErrorMessage = result.Error ?? "Failed to reset settings.";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error resetting settings: {ex.Message}";
        }

        await LoadCurrentSettingsAsync();
        return Page();
    }

    public async Task<IActionResult> OnGetUsageTrendsAsync(int days = 30)
    {
        var userId = User.GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return new JsonResult(new { success = false, message = "User not found." });
        }

        try
        {
            var result = await _usageTrackingService.GetUsageTrendsAsync(userId, days);
            if (result.Succeeded && result.Data != null)
            {
                return new JsonResult(new
                {
                    success = true,
                    data = new
                    {
                        labels = result.Data.TrendLabels.ToArray(),
                        datasets = new object[]
                        {
                            new
                            {
                                label = "API Calls",
                                data = result.Data.DailyOperations.Values.ToArray(),
                                borderColor = "rgb(75, 192, 192)",
                                backgroundColor = "rgba(75, 192, 192, 0.2)",
                                tension = 0.1
                            },
                            new
                            {
                                label = "Daily Cost ($)",
                                data = result.Data.DailyCosts.Values.ToArray(),
                                borderColor = "rgb(255, 99, 132)",
                                backgroundColor = "rgba(255, 99, 132, 0.2)",
                                tension = 0.1,
                                yAxisID = "y1"
                            }
                        }
                    }
                });
            }
            else
            {
                return new JsonResult(new { success = false, message = result.Error ?? "Failed to load usage trends." });
            }
        }
        catch (Exception ex)
        {
            return new JsonResult(new { success = false, message = $"Error: {ex.Message}" });
        }
    }

    public async Task<IActionResult> OnGetCostBreakdownAsync(int days = 30)
    {
        var userId = User.GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return new JsonResult(new { success = false, message = "User not found." });
        }

        try
        {
            var result = await _usageTrackingService.GetUserCostBreakdownAsync(userId, DateTime.UtcNow.AddDays(-days));
            if (result.Succeeded && result.Data != null)
            {
                return new JsonResult(new
                {
                    success = true,
                    data = new
                    {
                        labels = result.Data.CostByOperationType.Keys.ToArray(),
                        datasets = new object[]
                        {
                            new
                            {
                                data = result.Data.CostByOperationType.Values.ToArray(),
                                backgroundColor = new[]
                                {
                                    "rgba(255, 99, 132, 0.8)",
                                    "rgba(54, 162, 235, 0.8)",
                                    "rgba(255, 205, 86, 0.8)",
                                    "rgba(75, 192, 192, 0.8)",
                                    "rgba(153, 102, 255, 0.8)"
                                },
                                borderWidth = 1
                            }
                        }
                    },
                    totalCost = result.Data.TotalCost,
                    period = $"Last {days} days"
                });
            }
            else
            {
                return new JsonResult(new { success = false, message = result.Error ?? "Failed to load cost breakdown." });
            }
        }
        catch (Exception ex)
        {
            return new JsonResult(new { success = false, message = $"Error: {ex.Message}" });
        }
    }

    private async Task LoadCurrentSettingsAsync()
    {
        var userId = User.GetUserId();
        if (!string.IsNullOrEmpty(userId))
        {
            var settingsResult = await _aiSettingsService.GetUserSettingsAsync(userId);
            if (settingsResult.Succeeded)
            {
                CurrentSettings = settingsResult.Data;
            }
        }
    }

    public static List<(Core.Models.AI.AIModel Model, string Name, string Description)> GetAvailableModels()
    {
        return AIModelHelper.GetAllModels()
            .Select(model => (model, model.ToDisplayName(), model.GetCostDescription()))
            .ToList();
    }
} 