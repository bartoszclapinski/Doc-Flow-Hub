using DocFlowHub.Core.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using DocFlowHub.Web.Services;
using Microsoft.AspNetCore.Identity.UI.Services;
using DocFlowHub.Infrastructure.Data;
using DocFlowHub.Core.Services.Interfaces;
using DocFlowHub.Infrastructure.Services.Profile;
using DocFlowHub.Infrastructure.Services.Role;
using DocFlowHub.Infrastructure;
using DocFlowHub.Web.Filters;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// Hosts such as Render tell the app which port to listen on through PORT.
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddInfrastructure(builder.Configuration);

// Demo mode: enforce read-only via a global page filter (no-op unless DemoMode:Enabled).
builder.Services.AddScoped<DemoModePageFilter>();
builder.Services.Configure<MvcOptions>(options => options.Filters.AddService<DemoModePageFilter>());

// Add Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Password settings
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 8;

    // Lockout settings
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;

    // User settings
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Add Email Sender
builder.Services.AddTransient<IEmailSender, EmailSender>();

// Configure cookie policy
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.SlidingExpiration = true;
    options.ExpireTimeSpan = TimeSpan.FromDays(7);
});

var app = builder.Build();

// Initialize database (schema + seed). On SQL Server, transient errors are already retried
// for about two minutes. If the database is still unreachable, keep the site running so
// visitors get the error page instead of a crash loop.
try
{
    await DbInitializer.InitializeAsync(app.Services);
}
catch (Exception ex)
{
    app.Logger.LogCritical(ex, "Database initialization failed; the app will start without it. Restart the app once the database is reachable.");
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

// Error status codes without a body (404, 403 …) get a page in the app's style.
app.UseStatusCodePagesWithReExecute("/StatusCode/{0}");

app.UseHttpsRedirection();
app.UseStaticFiles();

// The UI is English and posts numbers with a dot ("0.7"). Without a fixed culture the form binder
// used the server's culture, so on a Polish (or German, …) host every decimal field failed validation.
var english = new System.Globalization.CultureInfo("en-US");
app.UseRequestLocalization(new Microsoft.AspNetCore.Builder.RequestLocalizationOptions
{
    DefaultRequestCulture = new Microsoft.AspNetCore.Localization.RequestCulture(english),
    SupportedCultures = new[] { english },
    SupportedUICultures = new[] { english },
});

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

// Liveness check for the host (Render's health check); doesn't touch the database.
app.MapGet("/healthz", () => Results.Text("ok"));

// Keep crawlers out of the public demo: every visit wakes the sleeping free instance.
if (app.Services.GetRequiredService<IDemoModeService>().IsEnabled)
{
    app.MapGet("/robots.txt", () => Results.Text("User-agent: *\nDisallow: /\n"));
}

app.Run();
