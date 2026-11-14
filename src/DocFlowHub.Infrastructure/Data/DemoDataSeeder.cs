using DocFlowHub.Core.Constants;
using DocFlowHub.Core.Identity;
using DocFlowHub.Core.Models;
using DocFlowHub.Core.Models.AI;
using DocFlowHub.Core.Models.Documents;
using DocFlowHub.Core.Models.Projects;
using DocFlowHub.Core.Services.Interfaces;
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

    // Seeded documents are recognised by this storage-path prefix ("demo/{id}/v{n}.{ext}").
    private const string DemoPathPrefix = "demo/";

    private static readonly string[] BodyTemplates =
    {
        "The goal of this document is to give the team one clear reference point. It describes the current situation, the agreed approach and the next milestones.",
        "This revision collects the latest figures and feedback from stakeholders. Open questions are listed at the end together with their owners.",
        "Scope, assumptions and constraints are described first, followed by the detailed plan. Risks are tracked separately and reviewed every two weeks.",
    };

    /// <param name="storage">
    /// Blob storage for the demo files. When null (storage not configured) the seed still
    /// creates all metadata, but seeded documents have no downloadable file.
    /// </param>
    public static async Task SeedAsync(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        ILogger logger,
        IDocumentStorageService? storage = null)
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
            await NormalizeLegacyFileTypesAsync(context);
            if (storage != null)
            {
                await RunFileStepSafelyAsync(logger, () => BackfillMissingFilesAsync(context, storage, logger));
            }
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
                    Path = $"/{rootName}", // same format as FolderService: the project is not part of the path
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
        // Only types the upload whitelist accepts, so every demo file is a valid upload.
        var fileTypes = DemoFileFactory.SupportedTypes;
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
            // Skewed towards recent dates so the dashboard's "last 7 days" has activity.
            var ageDays = 1 + Math.Pow(rng.NextDouble(), 2) * 359;
            var createdAt = now.AddDays(-ageDays).AddHours(-rng.Next(0, 24));
            var ownerId = owners[rng.Next(owners.Length)];
            var fileType = fileTypes[rng.Next(fileTypes.Length)];
            var title = $"{titleNouns[rng.Next(titleNouns.Length)]} {2024 + rng.Next(0, 2)}-{rng.Next(1, 13):D2}";

            // ~60% land in a project, ~half of those in a folder; ~35% shared with a team.
            Project? project = rng.NextDouble() < 0.6 ? projects[rng.Next(projects.Count)] : null;
            Folder? folder = project != null && rng.NextDouble() < 0.5
                ? folders.Where(f => f.ProjectId == project.Id).OrderBy(_ => rng.Next()).FirstOrDefault()
                : null;
            Team? team = rng.NextDouble() < 0.35 ? teams[rng.Next(teams.Count)] : null;

            // The app stores FileType as the dotted extension (Path.GetExtension), which the
            // download handlers append to the file name and map to a content type.
            var extension = "." + fileType;

            var doc = new Document
            {
                Title = title,
                Description = $"Demo {fileType.ToUpper()} document — {title}.",
                FileType = extension,
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

            // 1–3 versions, dates increasing from the document's creation; none in the future.
            var versionCount = rng.Next(1, 4);
            var versionDate = createdAt;
            for (var v = 1; v <= versionCount; v++)
            {
                if (versionDate > now) break;
                doc.UpdatedAt = versionDate;
                doc.Versions.Add(new DocumentVersion
                {
                    VersionNumber = v,
                    FileType = extension,
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
                versionDate = versionDate.AddDays(rng.Next(1, 40)).AddHours(rng.Next(0, 24));
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
                v.FilePath = $"{DemoPathPrefix}{doc.Id}/v{v.VersionNumber}{doc.FileType}";
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
                    GeneratedAt = Min(doc.CreatedAt.AddHours(rng.Next(1, 48)), now),
                });
            }
        }
        await context.SaveChangesAsync();

        // ---- Saved AI comparisons of consecutive versions (decision D4) -------
        // The demo blocks POSTs, so the Compare page can only show comparisons that already exist.
        var comparisonTemplates = new[]
        {
            ("Figures updated with the latest actuals; the conclusions are unchanged.",
             "• Revenue table updated with final numbers\n• Totals in the appendix corrected\n• Wording of the summary tightened",
             ChangeType.ContentModification, ChangeSignificance.Medium),
            ("A new section was added and an outdated note removed.",
             "• Added a section on regional results\n• Removed the note about pending estimates\n• Updated the table of contents",
             ChangeType.ContentAddition | ChangeType.ContentDeletion, ChangeSignificance.High),
            ("Formatting and structure changes only; the content is the same.",
             "• Tables reformatted\n• Headings renumbered\n• Typos fixed",
             ChangeType.StructuralChange, ChangeSignificance.Low),
        };
        foreach (var doc in documents)
        {
            var ordered = doc.Versions.OrderBy(v => v.VersionNumber).ToList();
            for (var i = 1; i < ordered.Count; i++)
            {
                var (summary, changes, type, significance) = comparisonTemplates[rng.Next(comparisonTemplates.Length)];
                context.VersionComparisons.Add(new VersionComparison
                {
                    DocumentId = doc.Id,
                    FromVersionId = ordered[i - 1].Id,
                    ToVersionId = ordered[i].Id,
                    ChangeSummary = summary,
                    DetailedChanges = changes,
                    ChangeType = type,
                    Significance = significance,
                    AIModel = "claude-haiku-4-5",
                    ConfidenceScore = Math.Round(0.8 + rng.NextDouble() * 0.15, 2),
                    TokensUsed = rng.Next(900, 2400),
                    ProcessingTimeMs = rng.Next(1200, 4200),
                    GeneratedAt = Min(ordered[i].CreatedAt.AddMinutes(rng.Next(5, 600)), now),
                });
            }
        }
        await context.SaveChangesAsync();

        // ---- AI usage logs (time-distributed, for admin charts) --------------
        // Saved before the slow file uploads, so an interrupted upload can't lose them
        // (the next startup takes the "already seeded" path and only backfills files).
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

        // ---- Real files in blob storage (so downloads work) -----------------
        if (storage != null)
        {
            await RunFileStepSafelyAsync(logger, () => UploadFilesAsync(context, storage, logger, documents));
        }
    }

    /// <summary>
    /// Demo files are optional: a storage or database hiccup while handling them must never
    /// stop the app from starting. Missing files are retried on the next startup.
    /// </summary>
    private static async Task RunFileStepSafelyAsync(ILogger logger, Func<Task> step)
    {
        try
        {
            await step();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Demo file step failed; it will be retried on the next startup.");
        }
    }

    /// <summary>
    /// Demo rows written by the earlier seeder stored FileType without the leading dot
    /// ("pdf"), which broke download file names and content types. Runs on every startup;
    /// a no-op once the rows are fixed.
    /// </summary>
    internal static async Task NormalizeLegacyFileTypesAsync(ApplicationDbContext context)
    {
        var docs = await context.Documents
            .Include(d => d.Versions)
            .Where(d => d.FilePath.StartsWith(DemoPathPrefix) && !d.FileType.StartsWith("."))
            .ToListAsync();
        if (docs.Count == 0) return;

        foreach (var doc in docs)
        {
            doc.FileType = "." + doc.FileType;
            foreach (var v in doc.Versions.Where(v => !v.FileType.StartsWith('.')))
            {
                v.FileType = "." + v.FileType;
            }
        }
        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Text of a demo file, built only from stored metadata so the same file can be
    /// rebuilt later (backfill) without the seed's RNG.
    /// </summary>
    internal static IReadOnlyList<string> BuildContentLines(Document doc, DocumentVersion version)
    {
        var lines = new List<string>
        {
            doc.Description ?? string.Empty,
            string.Empty,
            $"Version {version.VersionNumber}: {version.ChangeSummary}",
            $"Author: {version.UserName}",
            $"Date: {version.CreatedAt:yyyy-MM-dd}",
            string.Empty,
            BodyTemplates[(version.VersionNumber + doc.Title.Length) % BodyTemplates.Length],
        };
        if (version.VersionNumber > 1)
        {
            lines.Add($"Changes since version {version.VersionNumber - 1}: updated figures, clarified scope and refreshed the action list.");
        }
        lines.Add(string.Empty);
        lines.Add("Sample content generated for the DocFlowHub demo.");
        return lines;
    }

    /// <summary>
    /// Generates and uploads one file per version (overwriting any existing blob), then
    /// stores the real size and hash. Saves after each document, so an interruption loses
    /// at most one document's metadata. Stops at the first failed upload (e.g. storage
    /// unreachable); the next startup's backfill re-uploads everything.
    /// </summary>
    internal static async Task<int> UploadFilesAsync(
        ApplicationDbContext context,
        IDocumentStorageService storage,
        ILogger logger,
        IEnumerable<Document> documents)
    {
        var uploaded = 0;
        foreach (var doc in documents)
        {
            if (!DemoFileFactory.IsSupported(doc.FileType))
            {
                logger.LogWarning("Skipping demo document {Id}: no demo file for type '{Type}'.", doc.Id, doc.FileType);
                continue;
            }

            var type = doc.FileType.TrimStart('.');
            foreach (var version in doc.Versions.OrderBy(v => v.VersionNumber))
            {
                var content = DemoFileFactory.Create(type, doc.Title, BuildContentLines(doc, version));
                var file = DemoFileFactory.ToFormFile(content, $"{doc.Title}.{type}", type);
                var result = await storage.UploadDocumentAsync(file, doc.Id, version.VersionNumber);
                if (!result.Succeeded)
                {
                    logger.LogWarning(
                        "Demo file upload failed after {Uploaded} files ({Error}); it will be retried on the next startup.",
                        uploaded, result.Error);
                    await context.SaveChangesAsync();
                    return uploaded;
                }

                version.FileSize = content.Length;
                version.FileHash = DemoFileFactory.ComputeHash(content);
                if (version.Id == doc.CurrentVersionId)
                {
                    doc.FileSize = content.Length;
                }
                uploaded++;
            }
            await context.SaveChangesAsync();
        }

        logger.LogInformation("Uploaded {Count} demo files to storage.", uploaded);
        return uploaded;
    }

    /// <summary>
    /// For an already-seeded demo: files are uploaded in Id/version order, so if the
    /// newest demo document's last file exists, all do (one storage call per startup).
    /// Otherwise (first upload failed or was cut off, or storage was added later) every
    /// file is uploaded again, which also fixes sizes/hashes of the ones already there.
    /// </summary>
    internal static async Task BackfillMissingFilesAsync(
        ApplicationDbContext context,
        IDocumentStorageService storage,
        ILogger logger)
    {
        var demoDocs = (await context.Documents
                .Include(d => d.Versions)
                .Where(d => d.FilePath.StartsWith(DemoPathPrefix))
                .OrderBy(d => d.Id)
                .ToListAsync())
            .Where(d => DemoFileFactory.IsSupported(d.FileType) && d.Versions.Count > 0)
            .ToList();
        if (demoDocs.Count == 0) return;

        var last = demoDocs[^1];
        var probe = await storage.DocumentExistsAsync(last.Id, last.Versions.Max(v => v.VersionNumber));
        if (probe.Succeeded && probe.Data) return;

        logger.LogInformation("Demo files missing from storage — uploading them.");
        await UploadFilesAsync(context, storage, logger, demoDocs);
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

    private static DateTime Min(DateTime a, DateTime b) => a < b ? a : b;
}
