using DocFlowHub.Core.Constants;
using DocFlowHub.Core.Identity;
using DocFlowHub.Core.Models;
using DocFlowHub.Core.Models.AI;
using DocFlowHub.Core.Models.Documents;
using DocFlowHub.Core.Models.Projects;
using DocFlowHub.Core.Services.Interfaces;
using DocFlowHub.Infrastructure.Data.Demo;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DocFlowHub.Infrastructure.Data;

/// <summary>
/// Seeds the public read-only demo (Sprint 24: from <see cref="DemoCatalog"/>): the two demo accounts and
/// ten colleagues, six teams, ten projects with their own folder trees, about 130 documents with real
/// structure and up to seven versions, AI summaries and comparisons that name the actual changes, and AI
/// usage logs spread over 14 months for the admin charts. Idempotent — only seeds when the demo user has
/// no documents yet, so it is safe to run on every startup.
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

        // ---- People: the two demo accounts and the colleagues (Sprint 24) ------
        var people = new Dictionary<string, ApplicationUser> { [DemoCatalog.User] = demoUser, [DemoCatalog.Admin] = demoAdmin };
        foreach (var colleague in DemoCatalog.Colleagues)
        {
            people[colleague.Key] = await EnsureColleagueAsync(userManager, colleague,
                now.AddDays(-rng.Next(430, 720)), now.AddDays(-rng.Next(0, 20)).AddHours(-rng.Next(0, 24)));
        }
        string NameOf(string key) => DemoContent.NameOf(key);

        // ---- Categories ------------------------------------------------------
        var categories = new Dictionary<string, DocumentCategory>();
        foreach (var name in DemoCatalog.Categories)
        {
            var existing = await context.DocumentCategories.FirstOrDefaultAsync(c => c.Name == name);
            if (existing == null)
            {
                existing = new DocumentCategory { Name = name, Description = $"{name} documents", CreatedAt = now.AddDays(-390), IsActive = true };
                context.DocumentCategories.Add(existing);
            }
            categories[name] = existing;
        }
        await context.SaveChangesAsync();

        // ---- Teams + members -------------------------------------------------
        var teams = new Dictionary<string, Team>();
        foreach (var spec in DemoCatalog.Teams)
        {
            var existing = await context.Teams.FirstOrDefaultAsync(t => t.Name == spec.Name);
            if (existing == null)
            {
                var created = now.AddDays(-rng.Next(330, 400));
                existing = new Team
                {
                    Name = spec.Name,
                    Description = spec.Description,
                    OwnerId = people[spec.Members.First(m => m.Role == TeamRole.Admin).Person].Id,
                    CreatedAt = created,
                    Members = spec.Members.Select(m => new TeamMember
                    {
                        UserId = people[m.Person].Id,
                        Role = m.Role,
                        JoinedAt = created.AddDays(rng.Next(0, 60)),
                    }).ToList(),
                };
                context.Teams.Add(existing);
            }
            teams[spec.Name] = existing;
        }
        await context.SaveChangesAsync();

        // ---- Projects + folders (each project its own tree) -------------------
        var projects = new Dictionary<string, Project>();
        var folders = new Dictionary<(string Project, string Path), Folder>();
        foreach (var spec in DemoCatalog.Projects)
        {
            var oldest = DemoCatalog.Documents.Where(d => d.Project == spec.Name).Select(d => d.AgeDays).DefaultIfEmpty(200).Max();
            var project = await context.Projects.FirstOrDefaultAsync(p => p.Name == spec.Name);
            if (project == null)
            {
                project = new Project
                {
                    Name = spec.Name,
                    Description = spec.Description,
                    Color = spec.Color,
                    Icon = spec.Icon,
                    IsActive = !spec.Archived,
                    OwnerId = people[spec.Owner].Id,
                    CreatedAt = now.AddDays(-(oldest + rng.Next(5, 25))),
                };
                if (spec.Archived) project.UpdatedAt = now.AddDays(-rng.Next(300, 330));
                context.Projects.Add(project);
                await context.SaveChangesAsync();
            }
            projects[spec.Name] = project;

            foreach (var path in spec.Folders)
            {
                var slash = path.LastIndexOf('/');
                var parent = slash < 0 ? null : folders[(spec.Name, path[..slash])];
                var name = slash < 0 ? path : path[(slash + 1)..];
                var folder = await context.Folders.FirstOrDefaultAsync(f => f.ProjectId == project.Id && f.Path == "/" + path);
                if (folder == null)
                {
                    folder = new Folder
                    {
                        Name = name,
                        Description = $"{name} for {spec.Name}",
                        ProjectId = project.Id,
                        ParentFolderId = parent?.Id,
                        Level = path.Count(c => c == '/'),
                        Path = "/" + path, // same format as FolderService: the project is not part of the path
                        CreatedByUserId = project.OwnerId,
                        CreatedAt = (parent?.CreatedAt ?? project.CreatedAt).AddDays(rng.Next(1, 10)),
                    };
                    context.Folders.Add(folder);
                    await context.SaveChangesAsync(); // a subfolder needs its parent's Id
                }
                folders[(spec.Name, path)] = folder;
            }
        }

        // ---- Documents + versions from the catalogue ---------------------------
        var documents = new List<Document>();
        var catalogue = new Dictionary<Document, DemoDoc>();
        foreach (var spec in DemoCatalog.Documents)
        {
            var createdAt = Min(now.AddDays(-spec.AgeDays).AddHours(-rng.Next(0, 9)).AddMinutes(-rng.Next(0, 60)), now.AddHours(-2));
            var team = spec.Team == null ? null : teams[spec.Team];
            var authors = spec.Team == null
                ? new[] { spec.Owner, spec.Owner, DemoCatalog.User }
                : DemoCatalog.Teams.First(t => t.Name == spec.Team).Members.Select(m => m.Person).Prepend(spec.Owner).ToArray();
            var extension = "." + spec.Type;

            var doc = new Document
            {
                Title = spec.Title,
                Description = DemoContent.Description(spec),
                FileType = extension,
                FileSize = 0, // the real size is stored with the file
                FilePath = string.Empty, // set after Id is known
                OwnerId = people[spec.Owner].Id,
                TeamId = team?.Id,
                ProjectId = spec.Project == null ? null : projects[spec.Project].Id,
                FolderId = spec.Project == null || spec.Folder == null ? null : folders[(spec.Project, spec.Folder)].Id,
                CreatedAt = createdAt,
                UpdatedAt = createdAt,
                IsDeleted = false,
            };

            // Versions spread from the first one to (almost) today; never in the future.
            var date = createdAt;
            var room = Math.Max(1.0, (now - createdAt).TotalDays - 1);
            for (var v = 1; v <= spec.Versions; v++)
            {
                if (v > 1)
                {
                    date = Min(date.AddDays(rng.NextDouble() * room / spec.Versions + 0.2).AddHours(rng.Next(0, 8)), now.AddHours(-1));
                }
                var author = v == 1 ? spec.Owner : authors[rng.Next(authors.Length)];
                doc.UpdatedAt = date;
                doc.Versions.Add(new DocumentVersion
                {
                    VersionNumber = v,
                    FileType = extension,
                    FileSize = 0,
                    FilePath = string.Empty,
                    StoragePath = string.Empty,
                    CreatedAt = date,
                    UserId = people[author].Id,
                    UserName = NameOf(author),
                    ChangeSummary = DemoContent.ChangeSummary(spec, v),
                    CreatedBy = people[author].Id,
                    FileHash = string.Empty,
                });
            }

            foreach (var category in spec.Categories.Distinct())
            {
                doc.Categories.Add(categories[category]);
            }

            context.Documents.Add(doc);
            documents.Add(doc);
            catalogue[doc] = spec;
        }
        await context.SaveChangesAsync();

        // FilePath + CurrentVersionId now that Ids exist; AI summaries for about three quarters.
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

            if (rng.NextDouble() < 0.75)
            {
                var (summary, keyPoints) = DemoContent.AiSummary(catalogue[doc]);
                context.DocumentSummaries.Add(new DocumentSummary
                {
                    DocumentId = doc.Id,
                    Summary = summary,
                    KeyPoints = keyPoints,
                    AIModel = rng.NextDouble() < 0.7 ? "claude-haiku-4-5" : "claude-sonnet-5",
                    ConfidenceScore = Math.Round(0.78 + rng.NextDouble() * 0.18, 2),
                    GeneratedAt = Min(latest.CreatedAt.AddMinutes(rng.Next(1, 180)), now),
                });
            }
        }
        await context.SaveChangesAsync();

        // ---- Saved AI comparisons of every pair of versions (S22-9) ---------------
        // The demo blocks POSTs, so the Compare page can only show comparisons that already exist.
        // Each one lists exactly the changes made between the two versions.
        foreach (var doc in documents)
        {
            var spec = catalogue[doc];
            var ordered = doc.Versions.OrderBy(v => v.VersionNumber).ToList();
            for (var i = 1; i < ordered.Count; i++)
            for (var j = 0; j < i; j++)
            {
                var changes = DemoContent.ChangesBetween(spec, ordered[j].VersionNumber, ordered[i].VersionNumber);
                var type = spec.IsPicture ? ChangeType.StructuralChange
                    : changes.Count == 1 ? ChangeType.ContentModification
                    : ChangeType.ContentModification | ChangeType.ContentAddition;
                context.VersionComparisons.Add(new VersionComparison
                {
                    DocumentId = doc.Id,
                    FromVersionId = ordered[j].Id,
                    ToVersionId = ordered[i].Id,
                    ChangeSummary = changes.Count == 1
                        ? $"One change since v{ordered[j].VersionNumber}: {LowerFirst(changes[0])}."
                        : $"{changes.Count} changes since v{ordered[j].VersionNumber}, from \"{LowerFirst(changes[0])}\" to \"{LowerFirst(changes[^1])}\".",
                    DetailedChanges = string.Join("\n", changes.Select(c => "• " + c)),
                    ChangeType = type,
                    Significance = changes.Count >= 3 ? ChangeSignificance.High : changes.Count == 2 ? ChangeSignificance.Medium : ChangeSignificance.Low,
                    AIModel = "claude-haiku-4-5",
                    ConfidenceScore = Math.Round(0.8 + rng.NextDouble() * 0.15, 2),
                    TokensUsed = rng.Next(900, 2400),
                    ProcessingTimeMs = rng.Next(1200, 4200),
                    GeneratedAt = Min(ordered[i].CreatedAt.AddMinutes(rng.Next(5, 600)), now),
                });
            }
        }
        await context.SaveChangesAsync();

        // ---- AI usage logs (14 months, more recent, split between people) --------
        // Saved before the slow file uploads, so an interrupted upload can't lose them
        // (the next startup takes the "already seeded" path and only backfills files).
        var operations = new[] { "Summarization", "Summarization", "VersionComparison" };
        var models = new[] { "claude-haiku-4-5", "claude-haiku-4-5", "claude-sonnet-5", "gpt-4o-mini" };
        var userIds = people.Values.Select(u => u.Id).ToArray();
        var usageLogs = new List<AIUsageLog>();
        const int usageLogCount = 640;
        for (var i = 0; i < usageLogCount; i++)
        {
            var createdAt = now.AddDays(-Math.Pow(rng.NextDouble(), 1.8) * 420).AddMinutes(-rng.Next(0, 1440));
            var inputTokens = rng.Next(400, 6000);
            var outputTokens = rng.Next(80, 900);
            usageLogs.Add(new AIUsageLog
            {
                UserId = userIds[rng.Next(userIds.Length)],
                OperationType = operations[rng.Next(operations.Length)],
                AIModel = models[rng.Next(models.Length)],
                TokensUsed = inputTokens + outputTokens,
                EstimatedCost = Math.Round((decimal)(inputTokens * 0.000001 + outputTokens * 0.000005), 6),
                ResponseTime = TimeSpan.FromMilliseconds(rng.Next(350, 4200)),
                IsSuccess = rng.NextDouble() > 0.05,
                CreatedAt = Min(createdAt, now),
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
    /// PDFs get two more pages (Sprint 19), so the demo shows the page thumbnails and navigation:
    /// "Details" with the other body texts, and an appendix with the real version history up to this version.
    /// </summary>
    internal static IReadOnlyList<string> BuildPdfLines(Document doc, DocumentVersion version)
    {
        var lines = new List<string>(BuildContentLines(doc, version))
        {
            DemoFileFactory.PageBreak,
            DemoFileFactory.HeadingPrefix + "Details",
            string.Empty,
        };
        var first = (version.VersionNumber + doc.Title.Length) % BodyTemplates.Length;
        for (var i = 1; i < BodyTemplates.Length; i++)
        {
            lines.Add(BodyTemplates[(first + i) % BodyTemplates.Length]);
            lines.Add(string.Empty);
        }
        lines.Add($"Prepared by {version.UserName} on {version.CreatedAt:yyyy-MM-dd}.");

        lines.Add(DemoFileFactory.PageBreak);
        lines.Add(DemoFileFactory.HeadingPrefix + "Appendix: version history");
        lines.Add(string.Empty);
        foreach (var v in doc.Versions.Where(v => v.VersionNumber <= version.VersionNumber).OrderBy(v => v.VersionNumber))
        {
            lines.Add($"v{v.VersionNumber} - {v.CreatedAt:yyyy-MM-dd} - {v.UserName}: {v.ChangeSummary}");
        }
        return lines;
    }

    /// <summary>
    /// DOCX files get real structure (Sprint 20), so the demo shows the formatted view: labelled facts,
    /// headings, a bullet list and a table with the version history up to this version.
    /// </summary>
    internal static IReadOnlyList<string> BuildDocxLines(Document doc, DocumentVersion version)
    {
        const string bold = DemoFileFactory.BoldMark;
        static string Cell(string? text) => (text ?? string.Empty).Replace('|', '/');

        var lines = new List<string>();
        if (!string.IsNullOrWhiteSpace(doc.Description)) lines.Add(doc.Description);
        lines.Add($"{bold}Version {version.VersionNumber}:{bold} {version.ChangeSummary}");
        lines.Add($"{bold}Author:{bold} {version.UserName}");
        lines.Add($"{bold}Date:{bold} {version.CreatedAt:yyyy-MM-dd}");

        lines.Add(DemoFileFactory.HeadingPrefix + "Summary");
        lines.Add(BodyTemplates[(version.VersionNumber + doc.Title.Length) % BodyTemplates.Length]);
        if (version.VersionNumber > 1)
        {
            lines.Add($"Changes since version {version.VersionNumber - 1}: updated figures, clarified scope and refreshed the action list.");
        }

        lines.Add(DemoFileFactory.HeadingPrefix + "Action points");
        lines.Add(DemoFileFactory.BulletPrefix + $"Review the figures with {version.UserName}");
        lines.Add(DemoFileFactory.BulletPrefix + "Confirm the scope with the document owner");
        lines.Add(DemoFileFactory.BulletPrefix + $"Share version {version.VersionNumber} with the team before the next review");

        lines.Add(DemoFileFactory.HeadingPrefix + "Version history");
        lines.Add(DemoFileFactory.RowPrefix + string.Join(DemoFileFactory.CellSeparator, "Version", "Date", "Author", "Summary"));
        foreach (var v in doc.Versions.Where(v => v.VersionNumber <= version.VersionNumber).OrderBy(v => v.VersionNumber))
        {
            lines.Add(DemoFileFactory.RowPrefix + string.Join(DemoFileFactory.CellSeparator,
                $"v{v.VersionNumber}", $"{v.CreatedAt:yyyy-MM-dd}", Cell(v.UserName), Cell(v.ChangeSummary)));
        }
        lines.Add(string.Empty);
        lines.Add("*Sample content generated for the DocFlowHub demo.*");
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

            var type = doc.FileType.TrimStart('.').ToLowerInvariant();
            foreach (var version in doc.Versions.OrderBy(v => v.VersionNumber))
            {
                var content = BuildFile(doc, version);
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

    /// <summary>
    /// The file of one version. A catalogue document (found by its title) is built from its template or
    /// drawn as a picture; anything else (rows from an older seed) gets the generic text. Only stored
    /// metadata is used, so the backfill can rebuild exactly the same file.
    /// </summary>
    internal static byte[] BuildFile(Document doc, DocumentVersion version)
    {
        var type = doc.FileType.TrimStart('.').ToLowerInvariant();
        var spec = DemoCatalog.Find(doc.Title);
        if (spec != null && spec.Type == type)
        {
            if (spec.IsPicture)
            {
                return DemoImageFactory.Create(Picture(spec.Kind), DemoContent.PictureSeed(spec), version.VersionNumber);
            }
            var history = doc.Versions
                .OrderBy(v => v.VersionNumber)
                .Select(v => new DemoVersionInfo(v.VersionNumber, v.UserName, v.CreatedAt, v.ChangeSummary))
                .ToList();
            return DemoFileFactory.Create(type, doc.Title, DemoContent.Lines(spec, version.VersionNumber, history));
        }

        var lines = type switch
        {
            "pdf" => BuildPdfLines(doc, version),
            "docx" => BuildDocxLines(doc, version),
            _ => BuildContentLines(doc, version),
        };
        return DemoFileFactory.Create(type, doc.Title, lines);
    }

    private static DemoPicture Picture(DemoKind kind) => kind switch
    {
        DemoKind.BarChart => DemoPicture.BarChart,
        DemoKind.LineChart => DemoPicture.LineChart,
        DemoKind.Wireframe => DemoPicture.Wireframe,
        DemoKind.Diagram => DemoPicture.Diagram,
        _ => DemoPicture.Donut,
    };

    private static string LowerFirst(string text) => text.Length == 0 ? text : char.ToLowerInvariant(text[0]) + text[1..];

    /// <summary>
    /// A colleague who appears in the demo (Sprint 24). They cannot sign in: the password is random and
    /// never stored anywhere.
    /// </summary>
    private static async Task<ApplicationUser> EnsureColleagueAsync(UserManager<ApplicationUser> userManager, DemoPerson person, DateTime createdAt, DateTime lastLogin)
    {
        var user = await userManager.FindByEmailAsync(person.Email);
        if (user != null) return user;

        user = new ApplicationUser
        {
            UserName = person.Email,
            Email = person.Email,
            FirstName = person.FirstName,
            LastName = person.LastName,
            Bio = person.Title,
            EmailConfirmed = true,
            CreatedAt = createdAt,
            LastLoginAt = lastLogin,
            IsActive = true,
        };
        var password = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(24)) + "aA1!";
        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                $"Failed to create demo colleague {person.Email}: {string.Join("; ", result.Errors.Select(e => e.Description))}");
        }
        await userManager.AddToRoleAsync(user, "User");
        return user;
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
