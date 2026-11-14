#nullable disable

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using DocFlowHub.Core.Constants;
using DocFlowHub.Core.Identity;
using DocFlowHub.Core.Services.Interfaces;

namespace DocFlowHub.Web.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<LoginModel> _logger;
        private readonly IDemoModeService _demoMode;

        public LoginModel(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager, ILogger<LoginModel> logger, IDemoModeService demoMode)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _logger = logger;
            _demoMode = demoMode;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string ReturnUrl { get; set; }

        [TempData]
        public string ErrorMessage { get; set; }

        public class InputModel
        {
            [Required]
            [EmailAddress]
            public string Email { get; set; }

            [Required]
            [DataType(DataType.Password)]
            public string Password { get; set; }

            [Display(Name = "Remember me?")]
            public bool RememberMe { get; set; }
        }

        public async Task OnGetAsync(string returnUrl = null)
        {
            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                ModelState.AddModelError(string.Empty, ErrorMessage);
            }

            returnUrl ??= Url.Content("~/");

            // Clear the existing external cookie to ensure a clean login process
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            ReturnUrl = returnUrl;
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");

            if (ModelState.IsValid)
            {
                // This doesn't count login failures towards account lockout
                // To enable password failures to trigger account lockout, set lockoutOnFailure: true
                var result = await _signInManager.PasswordSignInAsync(Input.Email, Input.Password, Input.RememberMe, lockoutOnFailure: false);
                if (result.Succeeded)
                {
                    // Update LastLoginAt for the user
                    var user = await _userManager.FindByEmailAsync(Input.Email);
                    if (user != null)
                    {
                        user.LastLoginAt = DateTime.UtcNow;
                        await _userManager.UpdateAsync(user);
                    }
                    
                    _logger.LogInformation("User logged in.");
                    return LocalRedirect(returnUrl);
                }
                if (result.RequiresTwoFactor)
                {
                    return RedirectToPage("./LoginWith2fa", new { ReturnUrl = returnUrl, RememberMe = Input.RememberMe });
                }
                if (result.IsLockedOut)
                {
                    _logger.LogWarning("User account locked out.");
                    return RedirectToPage("./Lockout");
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                    return Page();
                }
            }

            // If we got this far, something failed, redisplay form
            return Page();
        }

        /// <summary>
        /// One-click sign-in to a public demo account (Sprint 17, decision S17-3). Only exists in demo mode;
        /// the password comes from <see cref="DemoAccounts"/>, not from the form.
        /// </summary>
        public async Task<IActionResult> OnPostDemoAsync(string account, string returnUrl = null)
        {
            if (!_demoMode.IsEnabled)
            {
                return NotFound();
            }

            var email = account switch
            {
                "user" => DemoAccounts.UserEmail,
                "admin" => DemoAccounts.AdminEmail,
                _ => null,
            };
            if (email == null)
            {
                return BadRequest();
            }

            returnUrl = !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl) ? returnUrl : Url.Content("~/");
            ReturnUrl = returnUrl;
            // The e-mail form is not part of this post; its empty fields must not show errors.
            ModelState.Clear();

            var result = await _signInManager.PasswordSignInAsync(email, DemoAccounts.Password, isPersistent: false, lockoutOnFailure: false);
            if (!result.Succeeded)
            {
                _logger.LogWarning("Demo sign-in as {Account} failed", account);
                ModelState.AddModelError(string.Empty, "The demo account isn't available right now. Try again in a minute.");
                return Page();
            }

            var user = await _userManager.FindByEmailAsync(email);
            if (user != null)
            {
                user.LastLoginAt = DateTime.UtcNow;
                await _userManager.UpdateAsync(user);
            }

            _logger.LogInformation("Demo sign-in as {Account}", account);
            return LocalRedirect(returnUrl);
        }
    }
} 