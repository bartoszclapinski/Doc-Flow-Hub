using DocFlowHub.Core.Models.Documents;
using DocFlowHub.Infrastructure.Data;
using DocFlowHub.Infrastructure.Data.Demo;
using DocFlowHub.Infrastructure.Services.Documents;
using DocFlowHub.Infrastructure.Services.Storage;
using FluentAssertions;
using Xunit;

namespace DocFlowHub.Tests.Unit.Services.Storage;

/// <summary>Sprint 24: the demo catalogue is consistent, and every demo file is valid and rebuildable.</summary>
public class DemoCatalogTests
{
    private static readonly string[] AccountKeys = { DemoCatalog.User, DemoCatalog.Admin };

    private static bool PersonExists(string key) => AccountKeys.Contains(key) || DemoCatalog.Person(key) != null;

    private static bool InTeam(string? team, string person) =>
        team != null && DemoCatalog.Teams.Single(t => t.Name == team).Members.Any(m => m.Person == person);

    public static TheoryData<string> Titles()
    {
        var data = new TheoryData<string>();
        foreach (var d in DemoCatalog.Documents) data.Add(d.Title);
        return data;
    }

    [Fact]
    public void TheCatalogue_HasTheAgreedSize()
    {
        DemoCatalog.Documents.Count.Should().BeInRange(120, 140);
        DemoCatalog.Projects.Should().HaveCount(10);
        DemoCatalog.Teams.Should().HaveCount(6);
        DemoCatalog.Colleagues.Should().HaveCount(10);
        DemoCatalog.Documents.Count(d => d.IsPicture).Should().BeGreaterOrEqualTo(10);
        DemoCatalog.Documents.Count(d => d.Versions >= 5).Should().BeGreaterOrEqualTo(15);
        DemoCatalog.Documents.Select(d => d.Type).Distinct().Should().BeEquivalentTo("pdf", "docx", "txt", "md", "png");
    }

    [Fact]
    public void Everything_ItRefersTo_Exists()
    {
        DemoCatalog.Documents.Select(d => d.Title).Should().OnlyHaveUniqueItems();
        DemoCatalog.Colleagues.Select(p => p.Email).Should().OnlyHaveUniqueItems();
        DemoCatalog.Teams.SelectMany(t => t.Members).Should().OnlyContain(m => PersonExists(m.Person));
        DemoCatalog.Projects.Should().OnlyContain(p => PersonExists(p.Owner));

        foreach (var project in DemoCatalog.Projects)
        {
            foreach (var path in project.Folders.Where(f => f.Contains('/')))
            {
                project.Folders.Should().Contain(path[..path.LastIndexOf('/')], $"{path} needs its parent in {project.Name}");
            }
        }

        foreach (var d in DemoCatalog.Documents)
        {
            PersonExists(d.Owner).Should().BeTrue(d.Title);
            if (d.Team != null) DemoCatalog.Teams.Should().Contain(t => t.Name == d.Team, d.Title);
            d.Categories.Should().NotBeEmpty().And.OnlyContain(c => DemoCatalog.Categories.Contains(c), d.Title);
            d.Versions.Should().BeInRange(1, 7, d.Title);
            d.AgeDays.Should().BeInRange(1, 420, d.Title);
            if (d.Project != null)
            {
                var project = DemoCatalog.Projects.SingleOrDefault(p => p.Name == d.Project);
                project.Should().NotBeNull(d.Title);
                if (d.Folder != null) project!.Folders.Should().Contain(d.Folder, d.Title);
            }
            else
            {
                d.Folder.Should().BeNull(d.Title);
            }
        }
    }

    [Fact]
    public void ColleaguesDocuments_ReachADemoAccountThroughATeam()
    {
        // The demo accounts only see their own documents and those of their teams.
        foreach (var d in DemoCatalog.Documents.Where(d => !AccountKeys.Contains(d.Owner)))
        {
            (InTeam(d.Team, DemoCatalog.User) || InTeam(d.Team, DemoCatalog.Admin)).Should().BeTrue(d.Title);
        }
        // And everything in the demo user's projects is visible to the demo user.
        var userProjects = DemoCatalog.Projects.Where(p => p.Owner == DemoCatalog.User).Select(p => p.Name).ToHashSet();
        foreach (var d in DemoCatalog.Documents.Where(d => d.Project != null && userProjects.Contains(d.Project) && d.Owner != DemoCatalog.User))
        {
            InTeam(d.Team, DemoCatalog.User).Should().BeTrue(d.Title);
        }
    }

    private static Document Seeded(DemoDoc spec)
    {
        var doc = new Document { Title = spec.Title, FileType = "." + spec.Type };
        var date = new DateTime(2026, 1, 5, 9, 0, 0, DateTimeKind.Utc);
        for (var v = 1; v <= spec.Versions; v++)
        {
            doc.Versions.Add(new DocumentVersion
            {
                VersionNumber = v,
                FileType = doc.FileType,
                CreatedAt = date.AddDays(v * 9),
                UserName = DemoContent.NameOf(spec.Owner),
                ChangeSummary = DemoContent.ChangeSummary(spec, v),
            });
        }
        return doc;
    }

    [Theory]
    [MemberData(nameof(Titles))]
    public async Task EveryVersion_IsAValidUpload_IsRebuiltTheSame_AndDiffersFromThePreviousOne(string title)
    {
        var spec = DemoCatalog.Find(title)!;
        var doc = Seeded(spec);
        // A DOCX is a zip with timestamps, so it is compared by what it shows; the other types byte by byte.
        string Content(byte[] bytes) => spec.Type == "docx" ? DocxRendering.Render(bytes).Html : Convert.ToBase64String(bytes);

        string? previous = null;
        foreach (var version in doc.Versions.OrderBy(v => v.VersionNumber))
        {
            var bytes = DemoDataSeeder.BuildFile(doc, version);
            Content(DemoDataSeeder.BuildFile(doc, version)).Should().Be(Content(bytes), "a missing file is rebuilt from metadata");

            var file = DemoFileFactory.ToFormFile(bytes, $"{title}.{spec.Type}", spec.Type);
            (await DocumentUploadRules.ValidateAsync(file, new DocumentStorageOptions())).Should().BeNull(title);

            if (previous != null) Content(bytes).Should().NotBe(previous, $"v{version.VersionNumber} of {title} changes something");
            previous = Content(bytes);
        }
    }

    [Fact]
    public void TextVersions_ChangeSpecificLines_ThatTheDiffShows()
    {
        foreach (var spec in DemoCatalog.Documents.Where(d => !d.IsPicture && d.Versions > 1))
        {
            var doc = Seeded(spec);
            var history = doc.Versions.Select(v => new DemoVersionInfo(v.VersionNumber, v.UserName, v.CreatedAt, v.ChangeSummary)).ToList();
            for (var v = 2; v <= spec.Versions; v++)
            {
                var diff = LineDiff.Compare(
                    DemoContent.Lines(spec, v - 1, history).ToArray(),
                    DemoContent.Lines(spec, v, history).ToArray());
                diff.Changes.Should().BeGreaterThan(0, $"v{v} of {spec.Title} ({spec.Kind}) changes the text");
            }
        }
    }

    [Fact]
    public void TheAiTexts_NameTheDocumentsOwnFacts()
    {
        var contract = DemoCatalog.Find("Service agreement — Northwind Hosting")!;
        DemoContent.AiSummary(contract).Summary.Should().Contain("Northwind Hosting");
        DemoContent.ChangesBetween(contract, 1, 3).Should().Equal(
            "Updated the monthly fee after the vendor call",
            "Added clause 7: data retention");
        DemoContent.AiSummary(contract).KeyPoints.Split('\n').Should().OnlyContain(l => l.StartsWith("• "));
    }

    [Fact]
    public void Pictures_ArePngs_ThatChangeBetweenVersions()
    {
        foreach (DemoPicture picture in Enum.GetValues<DemoPicture>())
        {
            var v1 = DemoImageFactory.Create(picture, 7, 1);
            var v2 = DemoImageFactory.Create(picture, 7, 2);
            v1.Take(8).Should().Equal(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A });
            v1.Should().NotEqual(v2, picture.ToString());
            DemoImageFactory.Create(picture, 7, 1).Should().Equal(v1);
        }
    }

    [Fact]
    public void ADocumentFromAnOlderSeed_StillGetsAFile()
    {
        var doc = new Document { Title = "Roadmap 2025-04", Description = "Demo PDF document.", FileType = ".pdf" };
        doc.Versions.Add(new DocumentVersion { VersionNumber = 1, FileType = ".pdf", UserName = "Demo User", ChangeSummary = "Initial version" });

        DemoDataSeeder.BuildFile(doc, doc.Versions.First()).Take(5).Should().Equal("%PDF-"u8.ToArray());
    }
}
