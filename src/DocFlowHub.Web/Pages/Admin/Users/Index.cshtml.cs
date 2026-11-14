using DocFlowHub.Core.Identity;
using DocFlowHub.Core.Models.Admin;
using DocFlowHub.Core.Models.Admin.Browser;
using DocFlowHub.Core.Models.Common;
using DocFlowHub.Core.Services.Interfaces;
using DocFlowHub.Web.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DocFlowHub.Web.Pages.Admin.Users;

/// <summary>
/// Admin · Users (design 3e, decision S14-3): search with status / role filters, a table with
/// multi-select and a bulk bar, quick actions per row. Results are toasts after a redirect.
/// </summary>
[Authorize(Roles = "Administrator")]
public class IndexModel : PageModel
{
    private const int PageSize = 25;

    // Operations an admin may not run on their own account.
    private static readonly HashSet<BulkOperationType> NotOnSelf = new()
    {
        BulkOperationType.Deactivate, BulkOperationType.Delete, BulkOperationType.LockAccount, BulkOperationType.RemoveRole,
    };

    private readonly IUserManagementService _users;
    private readonly IUserCommunicationService _messages;
    private readonly IAdminBrowserService _admin;
    private readonly IDemoModeService _demoMode;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(IUserManagementService users, IUserCommunicationService messages, IAdminBrowserService admin,
        IDemoModeService demoMode, ILogger<IndexModel> logger)
    {
        _users = users;
        _messages = messages;
        _admin = admin;
        _demoMode = demoMode;
        _logger = logger;
    }

    [BindProperty(SupportsGet = true, Name = "q")]
    public string? Search { get; set; }

    /// <summary>active, locked, unconfirmed or inactive (no sign-in for 30 days).</summary>
    [BindProperty(SupportsGet = true)]
    public string? Status { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Role { get; set; }

    /// <summary>Bound from the query in OnGetAsync: a "page" property would bind Razor Pages' own route value.</summary>
    public int PageNumber { get; private set; } = 1;

    public PagedResult<ApplicationUser> Users { get; private set; } = new();
    public Dictionary<string, AdminUserRow> Rows { get; private set; } = new();
    public List<string> AvailableRoles { get; private set; } = new();
    public bool IsDemo => _demoMode.IsEnabled;
    public string? CurrentUserId => User.GetUserId();

    public static readonly (string Value, string Label, UserStatus Status)[] Statuses =
    {
        ("active", "Active", UserStatus.Active),
        ("locked", "Locked", UserStatus.Locked),
        ("unconfirmed", "E-mail not confirmed", UserStatus.EmailNotConfirmed),
        ("inactive", "No sign-in for 30 days", UserStatus.Inactive),
    };

    public async Task<IActionResult> OnGetAsync([FromQuery] int? page)
    {
        PageNumber = Math.Max(1, page ?? 1);
        var status = Statuses.Where(s => s.Value == Status).Select(s => (UserStatus?)s.Status).FirstOrDefault();
        Status = status == null ? null : Status;
        AvailableRoles = await _users.GetAvailableRolesAsync();
        Role = AvailableRoles.Contains(Role ?? string.Empty) ? Role : null;

        Users = await _users.SearchUsersAsync(new UserManagementFilter
        {
            SearchTerm = string.IsNullOrWhiteSpace(Search) ? null : Search.Trim(),
            Status = status,
            Role = Role,
            SortBy = "Name",
            Page = PageNumber,
            PageSize = PageSize,
        });

        var rows = await _admin.GetUserRowsAsync(Users.Items.Select(u => u.Id).ToList());
        if (rows.Succeeded && rows.Data != null) Rows = rows.Data;
        return Page();
    }

    /// <summary>Runs one operation on the checked users.</summary>
    public async Task<IActionResult> OnPostBulkAsync(string operation, List<string> userIds, string? role,
        string? subject, string? message, string? returnUrl)
    {
        var adminId = User.GetUserId()!;
        if (!Enum.TryParse<BulkOperationType>(operation, out var type))
        {
            return Back(returnUrl, error: "Unknown operation.");
        }

        var ids = userIds.Where(id => !string.IsNullOrEmpty(id)).Distinct().ToList();
        if (NotOnSelf.Contains(type) && ids.Remove(adminId))
        {
            TempData["ToastError"] = "Your own account was left out: you can't lock, delete or demote yourself.";
        }
        if (ids.Count == 0)
        {
            return Back(returnUrl, error: TempData.Peek("ToastError") as string ?? "Select at least one user.");
        }

        if (type == BulkOperationType.SendMessage)
        {
            if (string.IsNullOrWhiteSpace(subject) || string.IsNullOrWhiteSpace(message))
            {
                return Back(returnUrl, error: "Enter a subject and a message.");
            }
            var sent = await _messages.SendBulkMessageAsync(ids, adminId, subject.Trim(), message.Trim());
            return sent.Succeeded
                ? Back(returnUrl, success: $"Message sent to {ids.Count} {(ids.Count == 1 ? "user" : "users")}")
                : Back(returnUrl, error: sent.Error ?? "The message could not be sent.");
        }

        if (type is BulkOperationType.AssignRole or BulkOperationType.RemoveRole && string.IsNullOrWhiteSpace(role))
        {
            return Back(returnUrl, error: "Choose a role.");
        }

        var result = await _users.ExecuteBulkOperationAsync(new BulkUserOperation
        {
            Operation = type,
            UserIds = ids,
            NewRole = role,
        }, adminId);
        if (!result.Succeeded || result.Data == null)
        {
            return Back(returnUrl, error: result.Error ?? "The operation failed.");
        }

        var r = result.Data;
        var done = $"{Verb(type)} {r.SuccessfulOperations} {(r.SuccessfulOperations == 1 ? "user" : "users")}";
        if (r.FailedOperations > 0)
        {
            _logger.LogWarning("Bulk {Operation}: {Failed} failed: {Errors}", type, r.FailedOperations, string.Join("; ", r.Errors));
            return Back(returnUrl, success: r.SuccessfulOperations > 0 ? done : null,
                error: $"{r.FailedOperations} failed: {string.Join("; ", r.Errors.Take(2))}");
        }
        return Back(returnUrl, success: done);
    }

    /// <summary>One action on one user (row menu, user page).</summary>
    public async Task<IActionResult> OnPostQuickAsync(string userId, string action, string? returnUrl)
    {
        var adminId = User.GetUserId()!;
        if (userId == adminId && action is "deactivate" or "lock")
        {
            return Back(returnUrl, error: "You can't lock your own account.");
        }

        var ids = new List<string> { userId };
        ServiceResult result = action switch
        {
            "activate" => await _users.ActivateUsersAsync(ids, adminId),
            "deactivate" => await _users.DeactivateUsersAsync(ids, adminId),
            "lock" => await _users.LockUsersAsync(ids, adminId),
            "unlock" => await _users.UnlockUsersAsync(ids, adminId),
            "reset-password" => await _users.ResetUserPasswordAsync(userId, adminId),
            "confirm-email" => await _users.ConfirmUserEmailAsync(userId, adminId),
            _ => ServiceResult.Failure("Unknown action."),
        };
        var label = action switch
        {
            "activate" => "User activated",
            "deactivate" => "User locked",
            "lock" => "User locked",
            "unlock" => "User unlocked",
            "reset-password" => "Password reset requested",
            "confirm-email" => "E-mail marked as confirmed",
            _ => "Done",
        };
        return result.Succeeded ? Back(returnUrl, success: label) : Back(returnUrl, error: result.Error ?? "The action failed.");
    }

    /// <summary>The list URL with one value changed (filters are kept, the page resets).</summary>
    public string Link(string? q = "\0", string? status = "\0", string? role = "\0", int? page = null)
    {
        var query = new Dictionary<string, string?>
        {
            ["q"] = q == "\0" ? Search : q,
            ["status"] = status == "\0" ? Status : status,
            ["role"] = role == "\0" ? Role : role,
            ["page"] = page is > 1 ? page.ToString() : null,
        };
        var parts = query.Where(p => !string.IsNullOrEmpty(p.Value)).Select(p => $"{p.Key}={Uri.EscapeDataString(p.Value!)}");
        var qs = string.Join("&", parts);
        return "/Admin/Users" + (qs.Length > 0 ? "?" + qs : "");
    }

    private static string Verb(BulkOperationType type) => type switch
    {
        BulkOperationType.Activate => "Activated",
        BulkOperationType.Deactivate => "Locked",
        BulkOperationType.Delete => "Deleted",
        BulkOperationType.AssignRole => "Gave the role to",
        BulkOperationType.RemoveRole => "Removed the role from",
        BulkOperationType.LockAccount => "Locked",
        BulkOperationType.UnlockAccount => "Unlocked",
        BulkOperationType.SendPasswordReset => "Sent a password reset to",
        _ => "Updated",
    };

    private IActionResult Back(string? returnUrl, string? success = null, string? error = null)
    {
        if (success != null) TempData["ToastSuccess"] = success;
        if (error != null) TempData["ToastError"] = error;
        return LocalRedirect(!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl) ? returnUrl : "/Admin/Users");
    }
}

/// <summary>Model of the account fields partial of the Create / Edit user forms.</summary>
public record UserFieldsView(
    string? Email, string? FirstName, string? LastName, bool EmailConfirmed, bool PasswordRequired,
    IReadOnlyList<string> AvailableRoles, IReadOnlyList<string> SelectedRoles, bool IsMe);

/// <summary>Model of the quick-actions partial (row menu and user page).</summary>
public record UserQuickActionsView(string UserId, AdminUserRow? Row, bool IsMe, string ReturnUrl);
