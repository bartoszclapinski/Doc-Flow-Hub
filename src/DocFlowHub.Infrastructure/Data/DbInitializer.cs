using DocFlowHub.Core.Identity;
using DocFlowHub.Core.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DocFlowHub.Infrastructure.Data;

public static class DbInitializer
{
    public static async Task InitializeAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        // Ensure database is created and migrated
        await context.Database.MigrateAsync();

        // Seed roles
        await SeedRolesAsync(roleManager);

        var demoMode = scope.ServiceProvider.GetRequiredService<IDemoModeService>();

        // Seed the default admin if none exists. Skipped in the public demo: its password is
        // in this repository, and the demo seeds its own read-only admin account instead.
        if (!demoMode.IsEnabled)
        {
            await SeedAdminUserAsync(userManager);
        }

        // In the public read-only demo, seed a large, time-distributed dataset.
        if (demoMode.IsEnabled)
        {
            var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DemoDataSeeder");

            // The storage service throws on construction when storage isn't configured. The demo
            // needs storage (most pages fail without it), but seeding the database still works,
            // so log the problem instead of stopping here.
            IDocumentStorageService? storage = null;
            try
            {
                storage = scope.ServiceProvider.GetRequiredService<IDocumentStorageService>();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Document storage is not configured — set DocumentStorage:ConnectionString. Demo documents get no files and most pages will fail.");
            }

            await DemoDataSeeder.SeedAsync(context, userManager, logger, storage);
        }
    }

    private static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
    {
        // Add default roles
        var roles = new[] { "Administrator", "Manager", "User" };
        foreach (var roleName in roles)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole(roleName));
            }
        }
    }

    private static async Task SeedAdminUserAsync(UserManager<ApplicationUser> userManager)
    {
        // Check if admin user exists
        var adminEmail = "admin@docflowhub.com";
        var adminUser = await userManager.FindByEmailAsync(adminEmail);

        if (adminUser == null)
        {
            // Create admin user
            adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                FirstName = "System",
                LastName = "Administrator",
                EmailConfirmed = true,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            var result = await userManager.CreateAsync(adminUser, "Admin123!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "Administrator");
            }
        }
    }
} 