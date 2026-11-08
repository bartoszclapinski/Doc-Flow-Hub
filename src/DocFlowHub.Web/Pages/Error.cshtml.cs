using System.Diagnostics;
using DocFlowHub.Core.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DocFlowHub.Web.Pages;

[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
[IgnoreAntiforgeryToken]
public class ErrorModel : PageModel
{
    private readonly IDemoModeService _demoMode;

    public ErrorModel(IDemoModeService demoMode)
    {
        _demoMode = demoMode;
    }

    public string? RequestId { get; set; }

    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

    public bool IsDemo => _demoMode.IsEnabled;

    public void OnGet() => SetRequestId();

    // The exception handler re-runs a failed POST as a POST to this page.
    public void OnPost() => SetRequestId();

    private void SetRequestId()
    {
        RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
    }
}
