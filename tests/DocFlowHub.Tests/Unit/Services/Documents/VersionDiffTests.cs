using System.Text;
using DocFlowHub.Core.Models.Common;
using DocFlowHub.Core.Models.Documents.Browser;
using DocFlowHub.Core.Services.Interfaces;
using DocFlowHub.Infrastructure.Data;
using DocFlowHub.Infrastructure.Services.AI;
using DocFlowHub.Infrastructure.Services.Documents;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DocFlowHub.Tests.Unit.Services.Documents;

/// <summary>Sprint 21: the line diff and the version diff service.</summary>
public class LineDiffTests
{
    private static string[] L(params string[] lines) => lines;

    private static string[] Numbered(int count, string prefix = "line ") =>
        Enumerable.Range(1, count).Select(i => prefix + i).ToArray();

    /// <summary>One row per line: " 3|3 text", "-4 text", "+4 text", "~12" for a fold of 12 lines.</summary>
    private static string[] Show(VersionDiff diff) => diff.Sections.SelectMany(section => section.Kind switch
    {
        DiffSectionKind.Fold => new[] { $"~{section.FoldedLines}" },
        _ => section.Lines.Select(line => line.Kind switch
        {
            DiffLineKind.Context => $" {line.OldNumber}|{line.NewNumber} {line.NewText}",
            DiffLineKind.Removed => $"-{line.OldNumber} {line.OldText}",
            _ => $"+{line.NewNumber} {line.NewText}",
        }),
    }).ToArray();

    private static string Pieces(DiffLine line) =>
        string.Concat(line.Pieces!.Select(p => p.Changed ? $"[{p.Text}]" : p.Text));

    [Fact]
    public void Lines_SplitOnAnyNewline_WithoutTheEmptyLastLine()
    {
        LineDiff.Lines("a\r\nb\rc\n").Should().Equal("a", "b", "c");
        LineDiff.Lines("a\n\nb").Should().Equal("a", "", "b");
        LineDiff.Lines("a\n\n").Should().Equal("a", "");
        LineDiff.Lines("").Should().BeEmpty();
        LineDiff.Lines(null).Should().BeEmpty();
    }

    [Fact]
    public void SameText_IsIdentical()
    {
        var diff = LineDiff.Compare(L("a", "b"), L("a", "b"));
        diff.Status.Should().Be(VersionDiffStatus.Identical);
        diff.Sections.Should().BeEmpty();
        LineDiff.Compare(L(), L()).Status.Should().Be(VersionDiffStatus.Identical);
    }

    [Fact]
    public void AddedRemovedAndChangedLines_KeepBothLineNumbers()
    {
        var diff = LineDiff.Compare(
            L("title", "old intro", "same", "gone", "end"),
            L("title", "new intro", "same", "end", "added"));

        diff.Status.Should().Be(VersionDiffStatus.Changed);
        Show(diff).Should().Equal(
            " 1|1 title",
            "-2 old intro",
            "+2 new intro",
            " 3|3 same",
            "-4 gone",
            " 5|4 end",
            "+5 added");
        (diff.AddedLines, diff.RemovedLines, diff.Changes).Should().Be((2, 2, 3));
        diff.Sections.Where(s => s.Kind == DiffSectionKind.Change).Select(s => s.ChangeNumber).Should().Equal(1, 2, 3);
    }

    [Fact]
    public void AChange_ListsRemovedThenAddedLines_WithMatchingRows()
    {
        var diff = LineDiff.Compare(L("a", "one", "two", "z"), L("a", "uno", "dos", "tres", "z"));

        var change = diff.Sections.Single(s => s.Kind == DiffSectionKind.Change);
        change.Lines.Select(l => (l.Kind, l.Row, l.OldNumber, l.NewNumber)).Should().Equal(
            (DiffLineKind.Removed, 1, 2, null),
            (DiffLineKind.Removed, 2, 3, null),
            (DiffLineKind.Added, 1, null, 2),
            (DiffLineKind.Added, 2, null, 3),
            (DiffLineKind.Added, 3, null, 4));
    }

    [Fact]
    public void AnEmptySide_IsOneChange()
    {
        var added = LineDiff.Compare(L(), L("a", "b"));
        Show(added).Should().Equal("+1 a", "+2 b");
        (added.AddedLines, added.RemovedLines, added.Changes).Should().Be((2, 0, 1));

        var removed = LineDiff.Compare(L("a", "b"), L());
        Show(removed).Should().Equal("-1 a", "-2 b");
        (removed.AddedLines, removed.RemovedLines, removed.Changes).Should().Be((0, 2, 1));
    }

    [Fact]
    public void ChangedLines_MarkTheWordsThatDiffer()
    {
        var diff = LineDiff.Compare(
            L("The budget for 2025 is 40,000 EUR and was approved."),
            L("The budget for 2026 is 55,000 EUR and was approved."));

        var change = diff.Sections.Single();
        Pieces(change.Lines[0]).Should().Be("The budget for [2025] is [40],000 EUR and was approved.");
        Pieces(change.Lines[1]).Should().Be("The budget for [2026] is [55],000 EUR and was approved.");
    }

    [Fact]
    public void ChangedWordsNextToEachOther_AreOneMark()
    {
        var diff = LineDiff.Compare(L("Owner: Anna Nowak, Finance team"), L("Owner: Piotr Zielony, Finance team"));

        Pieces(diff.Sections.Single().Lines[0]).Should().Be("Owner: [Anna Nowak], Finance team");
        Pieces(diff.Sections.Single().Lines[1]).Should().Be("Owner: [Piotr Zielony], Finance team");
    }

    [Fact]
    public void LinesWithLittleInCommon_AndUnpairedLines_HaveNoWordMarks()
    {
        var diff = LineDiff.Compare(L("Completely different sentence here", "x"), L("Nothing shared at all, really", "x", "extra"));

        diff.Sections.Where(s => s.Kind == DiffSectionKind.Change).SelectMany(s => s.Lines)
            .Should().OnlyContain(l => l.Pieces == null);
    }

    [Fact]
    public void IgnoreWhitespace_LeavesOutSpacingChanges_AndKeepsRealOnes()
    {
        var oldLines = L("alpha  beta", "  indented", "gamma", "tail ");
        var newLines = L("alpha beta", "indented", "delta", "tail");

        LineDiff.Compare(oldLines, newLines).Changes.Should().Be(1);
        (LineDiff.Compare(oldLines, newLines).AddedLines).Should().Be(4);

        var diff = LineDiff.Compare(oldLines, newLines, new VersionDiffOptions { IgnoreWhitespace = true });
        Show(diff).Should().Equal(" 1|1 alpha beta", " 2|2 indented", "-3 gamma", "+3 delta", " 4|4 tail");
        // The context keeps the old text for the left column.
        diff.Sections[0].Lines[0].OldText.Should().Be("alpha  beta");

        LineDiff.Compare(L("a  b"), L("a b"), new VersionDiffOptions { IgnoreWhitespace = true }).Status
            .Should().Be(VersionDiffStatus.Identical);
        LineDiff.Compare(L("ab"), L("a b"), new VersionDiffOptions { IgnoreWhitespace = true }).Status
            .Should().Be(VersionDiffStatus.Changed);
    }

    [Fact]
    public void UnchangedLines_AreFoldedAroundThreeLinesOfContext()
    {
        var oldLines = Numbered(30);
        var newLines = Numbered(30);
        newLines[14] = "line 15 changed";

        var diff = LineDiff.Compare(oldLines, newLines);

        Show(diff).Should().Equal(
            "~11",
            " 12|12 line 12", " 13|13 line 13", " 14|14 line 14",
            "-15 line 15", "+15 line 15 changed",
            " 16|16 line 16", " 17|17 line 17", " 18|18 line 18",
            "~12");
        diff.FoldedLines.Should().Be(23);
    }

    [Fact]
    public void BetweenTwoChanges_OnlyAGapOfMoreThanSevenLinesIsFolded()
    {
        // 7 unchanged lines between the changes: 3 + 3 context would leave one line to fold, so all stay.
        var near = Numbered(9);
        near[0] = "first"; near[8] = "last";
        Show(LineDiff.Compare(Numbered(9), near)).Should().NotContain(r => r.StartsWith('~'));

        // 8 unchanged lines: two are folded.
        var far = Numbered(10);
        far[0] = "first"; far[9] = "last";
        Show(LineDiff.Compare(Numbered(10), far)).Should().Equal(
            "-1 line 1", "+1 first",
            " 2|2 line 2", " 3|3 line 3", " 4|4 line 4",
            "~2",
            " 7|7 line 7", " 8|8 line 8", " 9|9 line 9",
            "-10 line 10", "+10 last");
    }

    [Fact]
    public void ShowAll_FoldsNothing()
    {
        var newLines = Numbered(30);
        newLines[14] = "changed";

        var diff = LineDiff.Compare(Numbered(30), newLines, new VersionDiffOptions { ShowAll = true });

        diff.FoldedLines.Should().Be(0);
        diff.Sections.Should().NotContain(s => s.Kind == DiffSectionKind.Fold);
        diff.Sections.Sum(s => s.Lines.Count(l => l.Kind == DiffLineKind.Context)).Should().Be(29);
    }

    [Fact]
    public void InsertedLines_ShiftTheNewNumbers()
    {
        var diff = LineDiff.Compare(L("a", "b", "c"), L("a", "x", "y", "b", "c"));

        Show(diff).Should().Equal(" 1|1 a", "+2 x", "+3 y", " 2|4 b", " 3|5 c");
    }

    [Fact]
    public void RepeatedAndEmptyLines_AreHandled()
    {
        var diff = LineDiff.Compare(L("", "x", "", "x", ""), L("", "x", "", "y", "", ""));

        diff.Status.Should().Be(VersionDiffStatus.Changed);
        // Applying the diff to the old text gives the new text.
        var rebuilt = diff.Sections.SelectMany(s => s.Lines).Where(l => l.Kind != DiffLineKind.Removed).Select(l => l.NewText);
        rebuilt.Should().Equal("", "x", "", "y", "", "");
        var original = diff.Sections.SelectMany(s => s.Lines).Where(l => l.Kind != DiffLineKind.Added).Select(l => l.OldText);
        original.Should().Equal("", "x", "", "x", "");
    }

    [Fact]
    public void LimitSizedTexts_AreComparedQuickly()
    {
        var oldLines = Numbered(VersionDiff.LineLimit, "old ");
        var newLines = Numbered(VersionDiff.LineLimit, "new ");

        var watch = System.Diagnostics.Stopwatch.StartNew();
        var diff = LineDiff.Compare(oldLines, newLines);
        watch.Stop();

        (diff.AddedLines, diff.RemovedLines, diff.Changes).Should().Be((VersionDiff.LineLimit, VersionDiff.LineLimit, 1));
        watch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(10));
    }
}

public class VersionDiffServiceTests
{
    private readonly Mock<IDocumentBrowserService> _browser = new();
    private readonly Mock<IDocumentStorageService> _storage = new();
    private readonly VersionDiffService _service;

    public VersionDiffServiceTests()
    {
        var context = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var extraction = new TextExtractionService(_storage.Object, context, Mock.Of<ILogger<TextExtractionService>>());
        _service = new VersionDiffService(_browser.Object, _storage.Object, extraction, Mock.Of<ILogger<VersionDiffService>>());
    }

    private static DocumentComparisonView View(string oldType, string newType) => new()
    {
        DocumentId = 7,
        Title = "Doc",
        FileType = newType,
        From = new DocumentDetailsVersion { Id = 101, Number = 1, FileType = oldType },
        To = new DocumentDetailsVersion { Id = 102, Number = 2, FileType = newType },
    };

    private void File(int version, byte[] bytes) =>
        _storage.Setup(s => s.DownloadDocumentAsync(7, version)).ReturnsAsync(ServiceResult<byte[]>.Success(bytes));

    private void File(int version, string text) => File(version, Encoding.UTF8.GetBytes(text));

    [Fact]
    public async Task TextFiles_AreComparedLineByLine()
    {
        File(1, "Title\r\nfirst draft\r\nend\r\n");
        File(2, "Title\nsecond draft\nend\nmore\n");

        var diff = (await _service.GetDiffAsync(View(".txt", ".txt"))).Data!;

        diff.Status.Should().Be(VersionDiffStatus.Changed);
        (diff.AddedLines, diff.RemovedLines, diff.Changes).Should().Be((2, 1, 2));
        diff.Note.Should().BeNull();
    }

    [Fact]
    public async Task SameText_SaysSo_AlsoWhenOnlyWhitespaceDiffers()
    {
        File(1, "a\nb  c\n");
        File(2, "a\nb c");

        var exact = (await _service.GetDiffAsync(View(".md", ".md"))).Data!;
        exact.Status.Should().Be(VersionDiffStatus.Changed);

        var loose = (await _service.GetDiffAsync(View(".md", ".md"), new VersionDiffOptions { IgnoreWhitespace = true })).Data!;
        loose.Status.Should().Be(VersionDiffStatus.Identical);
        loose.Note.Should().Contain("v1 and v2").And.Contain("whitespace is ignored");

        File(2, "a\r\nb  c");
        var same = (await _service.GetDiffAsync(View(".md", ".md"))).Data!;
        same.Status.Should().Be(VersionDiffStatus.Identical);
        same.Note.Should().Contain("same text").And.Contain("layout");
    }

    [Fact]
    public async Task PdfAndDocx_AreComparedByTheirExtractedText_EvenAcrossTypes()
    {
        File(1, DemoFileFactory.Create("docx", "Handbook", new[] { "Welcome to the team.", "Old rule." }));
        File(2, DemoFileFactory.Create("pdf", "Handbook", new[] { "Welcome to the team.", "New rule." }));

        var diff = (await _service.GetDiffAsync(View(".docx", ".pdf"))).Data!;

        diff.Status.Should().Be(VersionDiffStatus.Changed);
        var lines = diff.Sections.SelectMany(s => s.Lines).ToList();
        lines.Should().Contain(l => l.Kind == DiffLineKind.Removed && l.OldText == "Old rule.");
        lines.Should().Contain(l => l.Kind == DiffLineKind.Added && l.NewText!.Contains("New rule."));
    }

    [Theory]
    [InlineData(".png", "an image")]
    [InlineData(".doc", "a DOC file")]
    [InlineData(".xyz", "a XYZ file")]
    public async Task TypesWithoutText_ExplainThemselves_WithoutReadingTheFile(string type, string label)
    {
        File(1, "text");
        var diff = (await _service.GetDiffAsync(View(".txt", type))).Data!;

        diff.Status.Should().Be(VersionDiffStatus.NoText);
        diff.Note.Should().Contain("v2 is " + label);
        _storage.Verify(s => s.DownloadDocumentAsync(7, 2), Times.Never);
    }

    [Fact]
    public async Task APdfWithoutText_HasNoDiff()
    {
        File(1, "text");
        File(2, Encoding.ASCII.GetBytes("%PDF-1.4 not really a pdf"));

        var diff = (await _service.GetDiffAsync(View(".txt", ".pdf"))).Data!;

        diff.Status.Should().Be(VersionDiffStatus.NoText);
        diff.Note.Should().Contain("No text could be found in v2");
    }

    [Fact]
    public async Task AMissingFile_IsUnreadable()
    {
        _storage.Setup(s => s.DownloadDocumentAsync(7, 1)).ReturnsAsync(ServiceResult<byte[]>.Failure("missing blob"));

        var diff = (await _service.GetDiffAsync(View(".txt", ".txt"))).Data!;

        diff.Status.Should().Be(VersionDiffStatus.Unreadable);
        diff.Note.Should().Contain("v1 could not be read");
    }

    [Fact]
    public async Task TooMuchTextOrTooManyLines_IsNotCompared()
    {
        File(1, "short");
        File(2, new string('x', VersionDiff.TextLimitBytes + 10));
        var big = (await _service.GetDiffAsync(View(".txt", ".txt"))).Data!;
        big.Status.Should().Be(VersionDiffStatus.TooLarge);
        big.Note.Should().Contain("v2 is too large").And.Contain("256 KB");

        File(2, string.Join('\n', Enumerable.Repeat("x", VersionDiff.LineLimit + 1)));
        var many = (await _service.GetDiffAsync(View(".txt", ".txt"))).Data!;
        many.Status.Should().Be(VersionDiffStatus.TooLarge);

        File(2, string.Join('\n', Enumerable.Repeat("x", VersionDiff.LineLimit)));
        (await _service.GetDiffAsync(View(".txt", ".txt"))).Data!.Status.Should().Be(VersionDiffStatus.Changed);
    }

    [Fact]
    public async Task Access_GoesThroughTheComparePageCheck()
    {
        _browser.Setup(b => b.GetComparisonAsync("stranger", 7, 1, 2))
            .ReturnsAsync(ServiceResult<DocumentComparisonView>.Failure("Document not found"));
        (await _service.GetDiffAsync("stranger", 7, 1, 2)).Succeeded.Should().BeFalse();
        _storage.Verify(s => s.DownloadDocumentAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);

        _browser.Setup(b => b.GetComparisonAsync("owner", 7, 1, 2))
            .ReturnsAsync(ServiceResult<DocumentComparisonView>.Success(View(".txt", ".txt")));
        File(1, "a");
        File(2, "b");
        (await _service.GetDiffAsync("owner", 7, 1, 2)).Data!.Status.Should().Be(VersionDiffStatus.Changed);
    }

    [Fact]
    public async Task DemoVersions_OfOneDocument_Differ()
    {
        // The demo needs no new seed data: consecutive versions already differ in a few lines.
        var doc = new DocFlowHub.Core.Models.Documents.Document { Title = "Roadmap", Description = "Demo document." };
        for (var n = 1; n <= 2; n++)
        {
            doc.Versions.Add(new DocFlowHub.Core.Models.Documents.DocumentVersion
            {
                VersionNumber = n, UserName = "Demo User", ChangeSummary = n == 1 ? "Initial version" : "Revision 2",
                CreatedAt = new DateTime(2026, 1, n, 0, 0, 0, DateTimeKind.Utc),
            });
        }
        var versions = doc.Versions.OrderBy(x => x.VersionNumber).ToList();
        File(1, DemoFileFactory.Create("md", doc.Title, DemoDataSeeder.BuildContentLines(doc, versions[0])));
        File(2, DemoFileFactory.Create("md", doc.Title, DemoDataSeeder.BuildContentLines(doc, versions[1])));

        var diff = (await _service.GetDiffAsync(View(".md", ".md"))).Data!;

        diff.Status.Should().Be(VersionDiffStatus.Changed);
        diff.Changes.Should().BeGreaterThan(0);
        diff.Sections.SelectMany(s => s.Lines).Should().Contain(l => l.Kind == DiffLineKind.Added && l.NewText!.StartsWith("Changes since version 1"));
    }
}
