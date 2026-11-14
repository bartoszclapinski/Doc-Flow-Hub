using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DocFlowHub.Web.Pages;

/// <summary>
/// Shown for error status codes without a body (404 and friends), via
/// UseStatusCodePagesWithReExecute("/StatusCode/{0}").
/// </summary>
[IgnoreAntiforgeryToken]
public class StatusCodeModel : PageModel
{
    public int Code { get; private set; }
    public string CodeName { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public string Message { get; private set; } = string.Empty;

    public void OnGet(int code) => Fill(code);

    // Status-code pages re-execute the original method, so a failed POST lands here as a POST.
    public void OnPost(int code) => Fill(code);

    private void Fill(int code)
    {
        Code = code;
        (CodeName, Title, Message) = code switch
        {
            404 => ("not found", "Page not found", "The page doesn't exist or was moved. Check the address, or go back."),
            403 => ("forbidden", "You don't have access to this page", "Your account doesn't have permission to open it."),
            400 => ("bad request", "The request couldn't be processed", "Try again from the previous page."),
            _ => ("error", "Something went wrong", "The request couldn't be completed."),
        };
        Response.StatusCode = code;
    }
}
