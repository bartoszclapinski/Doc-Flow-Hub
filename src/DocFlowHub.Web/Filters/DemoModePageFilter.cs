using DocFlowHub.Core.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace DocFlowHub.Web.Filters;

/// <summary>
/// When <c>DemoMode:Enabled</c> is on, blocks every mutating (POST) page handler
/// except a small allowlist (login/logout, document downloads). This is the
/// server-side guarantee that the public demo is genuinely read-only — it holds
/// regardless of which buttons the UI happens to show or hide.
/// </summary>
/// <remarks>
/// A Razor Pages page filter (not middleware) is used deliberately: it runs
/// before the handler body executes and it can see the selected handler NAME,
/// so it can allow <c>Download</c> while blocking <c>Delete</c> on the same page
/// — something middleware (which only sees the HTTP method + path) cannot do.
/// </remarks>
public class DemoModePageFilter : IAsyncPageFilter
{
    private readonly IDemoModeService _demo;

    // Pages whose POSTs are always allowed (authentication).
    private static readonly HashSet<string> AllowedPages = new(StringComparer.OrdinalIgnoreCase)
    {
        "/Account/Login",
        "/Account/Logout",
    };

    // POST handler names allowed on any page (safe, non-mutating actions).
    private static readonly HashSet<string> AllowedHandlers = new(StringComparer.OrdinalIgnoreCase)
    {
        "Download", // Documents/Index + Documents/Details file download (POST, read-only)
    };

    public DemoModePageFilter(IDemoModeService demo) => _demo = demo;

    public Task OnPageHandlerSelectionAsync(PageHandlerSelectedContext context) => Task.CompletedTask;

    public async Task OnPageHandlerExecutionAsync(PageHandlerExecutingContext context, PageHandlerExecutionDelegate next)
    {
        if (!_demo.IsEnabled || !HttpMethods.IsPost(context.HttpContext.Request.Method))
        {
            await next();
            return;
        }

        var pagePath = (context.ActionDescriptor as CompiledPageActionDescriptor)?.ViewEnginePath;
        var handlerName = context.HandlerMethod?.Name;

        var allowed = (pagePath != null && AllowedPages.Contains(pagePath))
                      || (handlerName != null && AllowedHandlers.Contains(handlerName));

        if (allowed)
        {
            await next();
            return;
        }

        // Blocked — the mutating handler never runs.
        context.Result = BuildBlockedResult(context.HttpContext);
    }

    private static IActionResult BuildBlockedResult(HttpContext http)
    {
        const string message = "This is a read-only demo — changes are disabled.";

        if (IsApiRequest(http))
        {
            return new JsonResult(new { success = false, message })
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
        }

        // Flash a message, then send the user back where they came from (same-origin only).
        var tempFactory = http.RequestServices.GetService<ITempDataDictionaryFactory>();
        if (tempFactory != null)
        {
            var tempData = tempFactory.GetTempData(http);
            tempData["DemoReadOnlyMessage"] = message;
        }

        return new RedirectResult(GetSafeReturnTarget(http));
    }

    /// <summary>Referer, but only if it points at our own host — otherwise "/". Avoids open redirect.</summary>
    private static string GetSafeReturnTarget(HttpContext http)
    {
        var referer = http.Request.Headers.Referer.ToString();
        if (!string.IsNullOrWhiteSpace(referer)
            && Uri.TryCreate(referer, UriKind.Absolute, out var refUri)
            && string.Equals(refUri.Host, http.Request.Host.Host, StringComparison.OrdinalIgnoreCase))
        {
            return refUri.PathAndQuery;
        }

        return "/";
    }

    private static bool IsApiRequest(HttpContext http)
    {
        if (string.Equals(http.Request.Headers.XRequestedWith, "XMLHttpRequest", StringComparison.OrdinalIgnoreCase))
            return true;

        if ((http.Request.ContentType ?? string.Empty).Contains("application/json", StringComparison.OrdinalIgnoreCase))
            return true;

        return http.Request.Headers.Accept.ToString().Contains("application/json", StringComparison.OrdinalIgnoreCase);
    }
}
