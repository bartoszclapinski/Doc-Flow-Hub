using DocFlowHub.Core.Models.Admin.Dto;
using DocFlowHub.Core.Services.Interfaces;
using DocFlowHub.Web.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DocFlowHub.Web.Pages.Admin.Settings;

[Authorize(Roles = "Administrator")]
public class IndexModel : PageModel
{
    private readonly ISystemSettingsService _systemSettingsService;

    public IndexModel(ISystemSettingsService systemSettingsService)
    {
        _systemSettingsService = systemSettingsService;
    }

    [BindProperty]
    public UpdateSystemSettingsRequest SettingsRequest { get; set; } = new();

    public List<SystemSettingsGroupDto> SettingsGroups { get; set; } = new();
    public string ErrorMessage { get; set; } = string.Empty;
    public string SuccessMessage { get; set; } = string.Empty;

    public async Task<IActionResult> OnGetAsync()
    {
        await LoadSettingsAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostUpdateSettingsAsync()
    {
        var userId = User.GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            TempData["ErrorMessage"] = "User not authenticated.";
            return RedirectToPage();
        }

        var result = await _systemSettingsService.UpdateSettingsAsync(SettingsRequest, userId);
        
        if (result.Succeeded)
        {
            TempData["SuccessMessage"] = "Settings updated successfully.";
        }
        else
        {
            TempData["ErrorMessage"] = result.Error;
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostResetCategoryAsync(string category)
    {
        var userId = User.GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            TempData["ErrorMessage"] = "User not authenticated.";
            return RedirectToPage();
        }

        var result = await _systemSettingsService.ResetCategoryToDefaultAsync(category, userId);
        
        if (result.Succeeded)
        {
            TempData["SuccessMessage"] = $"Category '{category}' reset to default values.";
        }
        else
        {
            TempData["ErrorMessage"] = result.Error;
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostResetSettingAsync(string key)
    {
        var userId = User.GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            TempData["ErrorMessage"] = "User not authenticated.";
            return RedirectToPage();
        }

        var result = await _systemSettingsService.ResetSettingToDefaultAsync(key, userId);
        
        if (result.Succeeded)
        {
            TempData["SuccessMessage"] = $"Setting '{key}' reset to default value.";
        }
        else
        {
            TempData["ErrorMessage"] = result.Error;
        }

        return RedirectToPage();
    }

    private async Task LoadSettingsAsync()
    {
        var result = await _systemSettingsService.GetSettingsGroupedByCategoryAsync();
        if (result.Succeeded && result.Data != null)
        {
            SettingsGroups = result.Data;
            
            // Initialize the settings request with current values
            SettingsRequest.Settings = SettingsGroups
                .SelectMany(g => g.Settings)
                .ToDictionary(s => s.Key, s => s.Value);
        }
        else
        {
            ErrorMessage = result.Error ?? "Failed to load settings.";
        }

        // Set messages from TempData
        if (TempData["SuccessMessage"] != null)
        {
            SuccessMessage = TempData["SuccessMessage"].ToString()!;
        }
        if (TempData["ErrorMessage"] != null)
        {
            ErrorMessage = TempData["ErrorMessage"].ToString()!;
        }
    }
} 