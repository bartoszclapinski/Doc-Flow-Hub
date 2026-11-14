using DocFlowHub.Core.Models.Admin.Browser;
using DocFlowHub.Core.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DocFlowHub.Web.Pages.Admin;

/// <summary>
/// Analytics (design 8c, decision S14-7): stat line and four panels drawn with CSS, for 7, 30 or 90
/// days. Replaces the Chart.js page whose "usage trends" and "error statistics" were never filled.
/// </summary>
[Authorize(Roles = "Administrator")]
public class AnalyticsModel : PageModel
{
    private readonly IAdminBrowserService _admin;

    public AnalyticsModel(IAdminBrowserService admin) => _admin = admin;

    [BindProperty(SupportsGet = true)]
    public int Period { get; set; } = 30;

    public AdminAnalytics? Data { get; private set; }
    public string? ErrorMessage { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        Period = Period is 7 or 90 ? Period : 30;
        var result = await _admin.GetAnalyticsAsync(Period);
        if (result.Succeeded) Data = result.Data;
        else ErrorMessage = result.Error;
        return Page();
    }

    /// <summary>"OperationType" → readable label ("DocumentSummarization" → "Summaries").</summary>
    public static string OperationLabel(string operation) => operation switch
    {
        "Summarization" or "DocumentSummarization" => "Summaries",
        "VersionComparison" => "Version compare",
        "TextExtraction" => "Text extraction",
        _ => operation,
    };
}
