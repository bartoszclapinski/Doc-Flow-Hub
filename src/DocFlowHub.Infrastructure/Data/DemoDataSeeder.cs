using DocFlowHub.Core.Constants;
using DocFlowHub.Core.Identity;
using DocFlowHub.Core.Models;
using DocFlowHub.Core.Models.AI;
using DocFlowHub.Core.Models.Documents;
using DocFlowHub.Core.Models.Projects;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DocFlowHub.Infrastructure.Data;

/// <summary>
/// Seeds a large, realistic, time-distributed dataset for the public read-only demo:
/// two demo accounts, categories, teams, projects/folders, ~50 documents (with
/// versions + static AI summaries), and a few hundred AI-usage log rows spread over
/// time so the admin dashboards/charts look populated. Idempotent — only seeds when
/// the demo user has no documents yet, so it is safe to run on every startup (F1 has
/// no Always-On, so the seed runs on the first request).
/// </summary>
public static class DemoDataSeeder
{
    // Deterministic RNG so repeated fresh seeds produce the same demo.
    private const int Seed = 20260710;

    public static async Task SeedAsync(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        ILogger logger)
    {
        var now = DateTime.UtcNow;
        var rng = new Random(Seed);

        var demoUser = await EnsureUserAsync(userManager, DemoAccounts.UserEmail,
            DemoAccounts.UserFirstName, DemoAccounts.UserLastName, "User", now.AddDays(-400));
        var demoAdmin = await EnsureUserAsync(userManager, DemoAccounts.AdminEmail,
            DemoAccounts.AdminFirstName, DemoAccounts.AdminLastName, "Administrator", now.AddDays(-420));

        // Idempotency guard: if the demo user already owns documents, assume seeded.
        if (await context.Documents.AnyAsync(d => d.OwnerId == demoUser.Id))
        {
            logger.LogInformation("Demo data already present — skipping demo seed.");
            return;
        }

        logger.LogInformation("Seeding demo data...");

        var owners = new[] { demoUser.Id, demoAdmin.Id };

        // ---- Categories ------------------------------------------------------
        var categoryNames = new[] { "Reports", "Contracts", "Invoices", "Specifications", "Manuals", "Presentations", "Policies" };
        var categories = new List<DocumentCategory>();
        foreach (var name in categoryNames)
        {
            var existing = await context.DocumentCategories.FirstOrDefaultAsync(c => c.Name == name);
            if (existing == null)
            {
                existing = new DocumentCategory { Name = name, Description = $"{name} documents", CreatedAt = now.AddDays(-390), IsActive = true };
                context.DocumentCategories.Add(existing);
            }
            categories.Add(existing);
        }
        await context.SaveChangesAsync();

        // ---- Teams + members -------------------------------------------------
        var teamNames = new[] { "Engineering", "Marketing", "Operations", "Finance" };
        var teams = new List<Team>();
        foreach (var name in teamNames)
        {
            var existing = await context.Teams.FirstOrDefaultAsync(t => t.Name == name);
            if (existing == null)
            {
                existing = new Team
                {
                    Name = name,
                    Description = $"{name} team workspace",
                    OwnerId = demoAdmin.Id,
                    CreatedAt = now.AddDays(-rng.Next(300, 380)),
                    Members = new List<TeamMember>
                    {
                        new() { UserId = demoAdmin.Id, Role = TeamRole.Admin, JoinedAt = now.AddDays(-360) },
                        new() { UserId = demoUser.Id, Role = TeamRole.Member, JoinedAt = now.AddDays(-350) },
                    }
                };
                context.Teams.Add(existing);
            }
            teams.Add(existing);
        }
        await context.SaveChangesAsync();

        // ---- Projects + folders ---------------------------------------------
        var projectSpecs = new (string Name, string Color, string Icon)[]
        {
            ("Website Redesign", "#0284c7", "bi-globe"),
            ("Q3 Financials", "#059669", "bi-graph-up"),
            ("Product Launch", "#7c3aed", "bi-rocket"),
            ("Internal Tools", "#d97706", "bi-tools"),
        };
        var projects = new List<Project>();
        var folders = new List<Folder>();
        foreach (var (name, color, icon) in projectSpecs)
        {
            var existing = await context.Projects.FirstOrDefaultAsync(p => p.Name == name);
            if (existing == null)
            {
                existing = new Project
                {
                    Name = name,
                    Description = $"{name} project workspace",
                    Color = color,
                    Icon = icon,
                    IsActive = true,
                    OwnerId = owners[rng.Next(owners.Length)],
                    CreatedAt = now.AddDays(-rng.Next(200, 340)),
                };
                context.Projects.Add(existing);
            }
            projects.Add(existing);
        }
        await context.SaveChangesAsync();

        foreach (var project in projects)
        {
            if (await context.Folders.AnyAsync(f => f.ProjectId == project.Id))
            {
                folders.AddRange(await context.Folders.Where(f => f.ProjectId == project.Id).ToListAsync());
                continue;
            }

            var rootNames = new[] { "Drafts", "Final", "Archive" };
            foreach (var rootName in rootNames)
            {
                var root = new Folder
                {
                    Name = rootName,
                    Description = $"{rootName} in {project.Name}",
                    ProjectId = project.Id,
                    Level = 0,
                    Path = $"/{project.Name}/{rootName}",
                    CreatedByUserId = project.OwnerId,
                    CreatedAt = project.CreatedAt.AddDays(rng.Next(1, 20)),
                };
                context.Folders.Add(root);
                await context.SaveChangesAsync(); // need root.Id for child path/parent

                // one nested subfolder under "Final"
                if (rootName == "Final")
                {
                    var child = new Folder
                    {
                        Name = "Approved",
                        Description = "Approved documents",
                        ProjectId = project.Id,
                        ParentFolderId = root.Id,
                        Level = 1,
                        Path = $"{root.Path}/Approved",
                        CreatedByUserId = project.OwnerId,
                        CreatedAt = root.CreatedAt.AddDays(rng.Next(1, 15)),
                    };
                    context.Folders.Add(child);
                    folders.Add(child);
                }
                folders.Add(root);
            }
        }
        await context.SaveChangesAsync();

        // ---- Documents + versions + summaries -------------------------------
        var titleNouns = new[] { "Annual Report", "Service Agreement", "Budget Plan", "API Specification", "User Manual", "Roadmap", "Meeting Notes", "Release Notes", "Design Brief", "Security Review", "Onboarding Guide", "Invoice", "Proposal", "Retrospective", "Style Guide" };
        var fileTypes = new[] { "pdf", "docx", "txt", "xlsx", "md" };
        var summaryTemplates = new[]
        {
            "This document outlines the key objectives, scope, and deliverables. It summarizes the main findings and provides actionable recommendations for the team.",
            "A concise overview covering background, current status, and next steps. Highlights risks, dependencies, and the proposed timeline for completion.",
            "Details the requirements and acceptance criteria, along with supporting data. Concludes with a prioritized list of follow-up actions.",
            "Captures the decisions made, rationale behind them, and assigned owners. Includes open questions to be resolved in the next review.",
        };
        var keyPointsTemplates = new[]
        {
            "• Clear objectives defined\n• Timeline on track\n• Two open risks identified",
            "• Budget within target\n• Stakeholders aligned\n• Next review scheduled",
            "• Requirements finalized\n• Dependencies mapped\n• Owners assigned",
        };

        var documents = new List<Document>();
        const int documentCount = 52;
        for (var i = 0; i < documentCount; i++)
        {
            var createdAt = now.AddDays(-rng.Next(1, 360)).AddHours(-rng.Next(0, 24));
            var ownerId = owners[rng.Next(owners.Length)];
            var fileType = fileTypes[rng.Next(fileTypes.Length)];
            var title = $"{titleNouns[rng.Next(titleNouns.Length)]} {2024 + rng.Next(0, 2)}-{rng.Next(1, 13):D2}";

            // ~60% land in a project, ~half of those in a folder; ~35% shared with a team.
            Project? project = rng.NextDouble() < 0.6 ? projects[rng.Next(projects.Count)] : null;
            Folder? folder = project != null && rng.NextDouble() < 0.5
                ? folders.Where(f => f.ProjectId == project.Id).OrderBy(_ => rng.Next()).FirstOrDefault()
                : null;
            Team? team = rng.NextDouble() < 0.35 ? teams[rng.Next(teams.Count)] : null;

            var doc = new Document
            {
                Title = title,
                Description = $"Demo {fileType.ToUpper()} document — {title}.",
                FileType = fileType,
                FileSize = rng.Next(24_000, 5_000_000),
                FilePath = string.Empty, // set after Id is known
                OwnerId = ownerId,
                TeamId = team?.Id,
                ProjectId = project?.Id,
                FolderId = folder?.Id,
                CreatedAt = createdAt,
                UpdatedAt = createdAt,
                IsDeleted = false,
            };

            // 1–3 versions, dates increasing from the document's creation.
            var versionCount = rng.Next(1, 4);
            var versionDate = createdAt;
            for (var v = 1; v <= versionCount; v++)
            {
                doc.Versions.Add(new DocumentVersion
                {
                    VersionNumber = v,
                    FileType = fileType,
                    FileSize = doc.FileSize + rng.Next(-5000, 20000),
                    FilePath = string.Empty,
                    StoragePath = string.Empty,
                    CreatedAt = versionDate,
                    UserId = ownerId,
                    UserName = ownerId == demoUser.Id ? "Demo User" : "Demo Admin",
                    ChangeSummary = v == 1 ? "Initial version" : $"Revision {v}",
                    CreatedBy = ownerId,
                    FileHash = Guid.NewGuid().ToString("N"),
                });
                versionDate = versionDate.AddDays(rng.Next(3, 40));
                if (versionDate > now) versionDate = now;
            }

            // 2 distinct categories each (avoid a duplicate join row).
            var c1 = rng.Next(categories.Count);
            var c2 = (c1 + 1 + rng.Next(categories.Count - 1)) % categories.Count;
            doc.Categories.Add(categories[c1]);
            doc.Categories.Add(categories[c2]);

            context.Documents.Add(doc);
            documents.Add(doc);
        }
        await context.SaveChangesAsync();

        // Set FilePath + CurrentVersionId now that Ids exist, and seed ~70% summaries.
        foreach (var doc in documents)
        {
            var latest = doc.Versions.OrderByDescending(v => v.VersionNumber).First();
            foreach (var v in doc.Versions)
            {
                v.FilePath = $"demo/{doc.Id}/v{v.VersionNumber}.{doc.FileType}";
                v.StoragePath = v.FilePath;
            }
            doc.FilePath = latest.FilePath;
            doc.CurrentVersionId = latest.Id;

            if (rng.NextDouble() < 0.7)
            {
                context.DocumentSummaries.Add(new DocumentSummary
                {
                    DocumentId = doc.Id,
                    Summary = summaryTemplates[rng.Next(summaryTemplates.Length)],
                    KeyPoints = keyPointsTemplates[rng.Next(keyPointsTemplates.Length)],
                    AIModel = "claude-haiku-4-5",
                    ConfidenceScore = Math.Round(0.75 + rng.NextDouble() * 0.2, 2),
                    GeneratedAt = doc.CreatedAt.AddHours(rng.Next(1, 48)),
                });
            }
        }
        await context.SaveChangesAsync();

        // ---- AI usage logs (time-distributed, for admin charts) --------------
        var operations = new[] { "Summarization", "VersionComparison" };
        var models = new[] { "claude-haiku-4-5", "claude-sonnet-5", "gpt-4o-mini" };
        var usageLogs = new List<AIUsageLog>();
        const int usageLogCount = 320;
        for (var i = 0; i < usageLogCount; i++)
        {
            var createdAt = now.AddDays(-rng.Next(0, 90)).AddMinutes(-rng.Next(0, 1440));
            var inputTokens = rng.Next(400, 6000);
            var outputTokens = rng.Next(80, 900);
            var tokens = inputTokens + outputTokens;
            usageLogs.Add(new AIUsageLog
            {
                UserId = owners[rng.Next(owners.Length)],
                OperationType = operations[rng.Next(operations.Length)],
                AIModel = models[rng.Next(models.Length)],
                TokensUsed = tokens,
                EstimatedCost = Math.Round((decimal)(inputTokens * 0.000001 + outputTokens * 0.000005), 6),
                ResponseTime = TimeSpan.FromMilliseconds(rng.Next(350, 4200)),
                IsSuccess = rng.NextDouble() > 0.05,
                CreatedAt = createdAt,
                DocumentId = documents[rng.Next(documents.Count)].Id,
                InputSize = inputTokens * 4,
                OutputSize = outputTokens * 4,
                ServedFromCache = rng.NextDouble() < 0.2,
                QualitySetting = 0.7,
            });
        }
        context.AIUsageLogs.AddRange(usageLogs);
        await context.SaveChangesAsync();

        logger.LogInformation("Demo seed complete: {Docs} documents, {Logs} usage logs.", documents.Count, usageLogs.Count);
    }

    private static async Task<ApplicationUser> EnsureUserAsync(
        UserManager<ApplicationUser> userManager,
        string email, string firstName, string lastName, string role, DateTime createdAt)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user == null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FirstName = firstName,
                LastName = lastName,
                EmailConfirmed = true,
                CreatedAt = createdAt,
                IsActive = true,
            };
            var result = await userManager.CreateAsync(user, DemoAccounts.Password);
            if (!result.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Failed to create demo user {email}: {string.Join("; ", result.Errors.Select(e => e.Description))}");
            }
        }

        if (!await userManager.IsInRoleAsync(user, role))
        {
            await userManager.AddToRoleAsync(user, role);
        }

        return user;
    }
}
