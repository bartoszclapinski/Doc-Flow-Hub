using DocFlowHub.Core.Models.Common;
using DocFlowHub.Core.Models.Documents.Browser;
using DocFlowHub.Core.Services.Interfaces;
using DocFlowHub.Infrastructure.Data;
using DocFlowHub.Infrastructure.Services.AI;
using DocFlowHub.Infrastructure.Services.Documents;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using A = DocumentFormat.OpenXml.Drawing;
using DW = DocumentFormat.OpenXml.Drawing.Wordprocessing;
using PIC = DocumentFormat.OpenXml.Drawing.Pictures;

namespace DocFlowHub.Tests.Unit.Services.Documents;

/// <summary>Sprint 20: the DOCX → HTML writer and the Word preview.</summary>
public class DocxRenderingTests
{
    // A 1x1 transparent PNG.
    private static readonly byte[] Png = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNkYPhfDwAChwGA60e6kgAAAABJRU5ErkJggg==");

    internal static byte[] Build(Action<MainDocumentPart, Body> build)
    {
        using var ms = new MemoryStream();
        using (var doc = WordprocessingDocument.Create(ms, WordprocessingDocumentType.Document))
        {
            var main = doc.AddMainDocumentPart();
            var body = new Body();
            build(main, body);
            main.Document = new Document(body);
            main.Document.Save();
        }
        return ms.ToArray();
    }

    private static string Html(Action<MainDocumentPart, Body> build) => DocxRendering.Render(Build(build)).Html;

    private static Text T(string text) => new(text) { Space = SpaceProcessingModeValues.Preserve };

    private static Run R(string text, params OpenXmlElement[] properties) =>
        properties.Length == 0 ? new Run(T(text)) : new Run(new RunProperties(properties), T(text));

    private static Paragraph P(string text, string? styleId = null)
    {
        var paragraph = new Paragraph();
        if (styleId != null) paragraph.Append(new ParagraphProperties(new ParagraphStyleId { Val = styleId }));
        paragraph.Append(R(text));
        return paragraph;
    }

    private static Paragraph Item(string text, int numId, int level = 0) => new(
        new ParagraphProperties(new NumberingProperties(new NumberingLevelReference { Val = level }, new NumberingId { Val = numId })),
        R(text));

    private static void Styles(MainDocumentPart main, params Style[] styles) =>
        main.AddNewPart<StyleDefinitionsPart>().Styles = new Styles(styles);

    private static Style ParagraphStyle(string id, string name, int? outline = null, string? basedOn = null)
    {
        var style = new Style(new StyleName { Val = name }) { Type = StyleValues.Paragraph, StyleId = id };
        if (basedOn != null) style.Append(new BasedOn { Val = basedOn });
        if (outline != null) style.Append(new StyleParagraphProperties(new OutlineLevel { Val = outline }));
        return style;
    }

    /// <summary>numId 1 and 2: bullets; numId 3: lower letters starting at 3, decimal on level 1.</summary>
    private static void Numbering(MainDocumentPart main) =>
        main.AddNewPart<NumberingDefinitionsPart>().Numbering = new Numbering(
            new AbstractNum(
                new Level(new NumberingFormat { Val = NumberFormatValues.Bullet }) { LevelIndex = 0 },
                new Level(new NumberingFormat { Val = NumberFormatValues.Bullet }) { LevelIndex = 1 })
            { AbstractNumberId = 0 },
            new AbstractNum(
                new Level(new StartNumberingValue { Val = 3 }, new NumberingFormat { Val = NumberFormatValues.LowerLetter }) { LevelIndex = 0 },
                new Level(new StartNumberingValue { Val = 1 }, new NumberingFormat { Val = NumberFormatValues.Decimal }) { LevelIndex = 1 })
            { AbstractNumberId = 1 },
            new NumberingInstance(new AbstractNumId { Val = 0 }) { NumberID = 1 },
            new NumberingInstance(new AbstractNumId { Val = 0 }) { NumberID = 2 },
            new NumberingInstance(new AbstractNumId { Val = 1 }) { NumberID = 3 });

    private static Drawing Picture(string relationshipId, string? description, long widthEmu = 952500) => new(
        new DW.Inline(
            new DW.Extent { Cx = widthEmu, Cy = widthEmu },
            new DW.DocProperties { Id = 1U, Name = "Picture 1", Description = description },
            new A.Graphic(new A.GraphicData(
                new PIC.Picture(
                    new PIC.NonVisualPictureProperties(
                        new PIC.NonVisualDrawingProperties { Id = 0U, Name = "p.png" },
                        new PIC.NonVisualPictureDrawingProperties()),
                    new PIC.BlipFill(new A.Blip { Embed = relationshipId }, new A.Stretch(new A.FillRectangle())),
                    new PIC.ShapeProperties()))
            { Uri = "http://schemas.openxmlformats.org/drawingml/2006/picture" })));

    // ------------------------------------------------------------ headings

    [Fact]
    public void Headings_ComeFromTheStyleName_EvenWithALocalisedStyleId()
    {
        var html = Html((main, body) =>
        {
            Styles(main, ParagraphStyle("Tytu", "Title"), ParagraphStyle("Nagwek1", "heading 1"), ParagraphStyle("Nagwek2", "heading 2"));
            body.Append(P("Report", "Tytu"), P("Scope", "Nagwek1"), P("Details", "Nagwek2"), P("Body text."));
        });

        html.Should().Be("<h1 class=\"dfh-docx-title\">Report</h1><h1>Scope</h1><h2>Details</h2><p>Body text.</p>");
    }

    [Fact]
    public void Headings_ComeFromTheOutlineLevel_OfTheStyleChainOrTheParagraph()
    {
        var html = Html((main, body) =>
        {
            Styles(main, ParagraphStyle("Base", "Section base", outline: 1), ParagraphStyle("Child", "Section child", basedOn: "Base"));
            body.Append(P("From the chain", "Child"));
            body.Append(new Paragraph(new ParagraphProperties(new OutlineLevel { Val = 2 }), R("Own level")));
            body.Append(new Paragraph(new ParagraphProperties(new ParagraphStyleId { Val = "Base" }, new OutlineLevel { Val = 9 }), R("Body text level")));
            body.Append(new Paragraph(new ParagraphProperties(new OutlineLevel { Val = 8 }), R("Deep")));
        });

        html.Should().Be("<h2>From the chain</h2><h3>Own level</h3><p>Body text level</p><h6>Deep</h6>");
    }

    [Fact]
    public void Headings_WithoutAStylesPart_UseTheEnglishIds()
    {
        Html((_, body) => body.Append(P("A", "Heading2"), P("B", "Title"), P("C", "Quote")))
            .Should().Be("<h2>A</h2><h1 class=\"dfh-docx-title\">B</h1><p>C</p>");
    }

    // -------------------------------------------------------------- inline

    [Fact]
    public void Inline_FormattingBecomesTags_AndNeighbouringRunsAreJoined()
    {
        var html = Html((_, body) => body.Append(new Paragraph(
            R("Plain "),
            R("bo", new Bold()),
            R("ld", new Bold()),
            R(" both", new Bold(), new Italic()),
            R(" under", new Underline { Val = UnderlineValues.Single }),
            R(" gone", new Strike()),
            R("2", new VerticalTextAlignment { Val = VerticalPositionValues.Superscript }),
            R("n", new VerticalTextAlignment { Val = VerticalPositionValues.Subscript }),
            R(" off", new Bold { Val = false }, new Underline { Val = UnderlineValues.None }))));

        html.Should().Be("<p>Plain <strong>bold</strong><strong><em> both</em></strong><u> under</u><s> gone</s><sup>2</sup><sub>n</sub> off</p>");
    }

    [Fact]
    public void Inline_BoldFromACharacterStyle_IsKept()
    {
        var html = Html((main, body) =>
        {
            main.AddNewPart<StyleDefinitionsPart>().Styles = new Styles(
                new Style(new StyleName { Val = "Strong" }, new StyleRunProperties(new Bold())) { Type = StyleValues.Character, StyleId = "Pogrubienie" });
            body.Append(new Paragraph(R("Note: ", new RunStyle { Val = "Pogrubienie" }), R("text")));
        });

        html.Should().Be("<p><strong>Note: </strong>text</p>");
    }

    [Fact]
    public void Inline_LineBreaksTabsAndHiddenText()
    {
        var html = Html((_, body) => body.Append(new Paragraph(
            new Run(T("one"), new Break(), T("two"), new TabChar(), T("three")),
            R("secret", new Vanish()))));

        html.Should().Be("<p>one<br>two\u2003three</p>");
    }

    [Fact]
    public void Text_IsAlwaysEncoded()
    {
        var html = Html((_, body) => body.Append(P("<script>alert(1)</script> & <img src=x onerror=alert(2)> \"q\"")));

        html.Should().NotContain("<script").And.NotContain("<img");
        html.Should().Contain("&lt;script&gt;alert(1)&lt;/script&gt; &amp; &lt;img src=x onerror=alert(2)&gt;");
    }

    [Fact]
    public void TrackedChanges_ShowTheCurrentText()
    {
        var html = Html((_, body) => body.Append(new Paragraph(
            R("Kept "),
            new DeletedRun(new Run(new DeletedText("removed "))) { Id = "1", Author = "a" },
            new InsertedRun(R("added")) { Id = "2", Author = "a" })));

        html.Should().Be("<p>Kept added</p>");
    }

    [Fact]
    public void Alignment_BecomesAClass_AndEmptyParagraphsAreLeftOut()
    {
        var html = Html((_, body) => body.Append(
            new Paragraph(new ParagraphProperties(new Justification { Val = JustificationValues.Center }), R("Middle")),
            new Paragraph(),
            new Paragraph(new ParagraphProperties(new Justification { Val = JustificationValues.Right }), R("Edge")),
            new Paragraph(new ParagraphProperties(new Justification { Val = JustificationValues.Both }), R("Justified"))));

        html.Should().Be("<p class=\"is-center\">Middle</p><p class=\"is-right\">Edge</p><p>Justified</p>");
    }

    [Fact]
    public void PageBreaks_BecomeARule()
    {
        var html = Html((_, body) => body.Append(
            P("Page one"),
            new Paragraph(new Run(new Break { Type = BreakValues.Page })),
            new Paragraph(new ParagraphProperties(new PageBreakBefore()), R("Page three"))));

        html.Should().Be("<p>Page one</p><hr class=\"dfh-docx-break\"><hr class=\"dfh-docx-break\"><p>Page three</p>");
    }

    // --------------------------------------------------------------- lists

    [Fact]
    public void Lists_BulletsWithNesting()
    {
        var html = Html((main, body) =>
        {
            Numbering(main);
            body.Append(P("Before"), Item("A", 1), Item("A1", 1, 1), Item("A2", 1, 1), Item("B", 1), P("After"));
        });

        html.Should().Be("<p>Before</p><ul><li>A<ul><li>A1</li><li>A2</li></ul></li><li>B</li></ul><p>After</p>");
    }

    [Fact]
    public void Lists_NumberedWithTypeAndStart_AndANestedDecimalList()
    {
        var html = Html((main, body) =>
        {
            Numbering(main);
            body.Append(Item("Third", 3), Item("Sub", 3, 1), Item("Fourth", 3));
        });

        html.Should().Be("<ol type=\"a\" start=\"3\"><li>Third<ol><li>Sub</li></ol></li><li>Fourth</li></ol>");
    }

    [Fact]
    public void Lists_TwoListsInARow_StaySeparate_AndAListMayStartNested()
    {
        var html = Html((main, body) =>
        {
            Numbering(main);
            body.Append(Item("One", 1), Item("Two", 2), Item("Letter", 3), Item("Deep", 1, 1));
        });

        html.Should().Be("<ul><li>One</li></ul><ul><li>Two</li></ul><ol type=\"a\" start=\"3\"><li>Letter<ul><li>Deep</li></ul></li></ol>");
    }

    [Fact]
    public void Lists_UnknownNumbering_IsABulletList_AndNumIdZeroIsNoList()
    {
        Html((_, body) => body.Append(Item("Loose", 42), Item("Off", 0)))
            .Should().Be("<ul><li>Loose</li></ul><p>Off</p>");
    }

    // -------------------------------------------------------------- tables

    private static TableCell Cell(string text, int span = 1, MergedCellValues? merge = null, bool continues = false)
    {
        var properties = new TableCellProperties();
        if (span > 1) properties.Append(new GridSpan { Val = span });
        if (merge != null) properties.Append(new VerticalMerge { Val = merge.Value });
        else if (continues) properties.Append(new VerticalMerge());
        var cell = new TableCell();
        if (properties.HasChildren) cell.Append(properties);
        cell.Append(text.Length == 0 ? new Paragraph() : P(text));
        return cell;
    }

    [Fact]
    public void Tables_HeaderRowAndMergedCells()
    {
        var html = Html((_, body) => body.Append(new Table(
            new TableRow(new TableRowProperties(new TableHeader()), Cell("Name"), Cell("Q1"), Cell("Q2")),
            new TableRow(Cell("North", merge: MergedCellValues.Restart), Cell("Both quarters", span: 2)),
            new TableRow(Cell("", continues: true), Cell("1"), Cell("2")),
            new TableRow(Cell("South"), Cell("3"), Cell("4")))));

        html.Should().Be(
            "<div class=\"dfh-docx-table\"><table>" +
            "<tr><th scope=\"col\"><p>Name</p></th><th scope=\"col\"><p>Q1</p></th><th scope=\"col\"><p>Q2</p></th></tr>" +
            "<tr><td rowspan=\"2\"><p>North</p></td><td colspan=\"2\"><p>Both quarters</p></td></tr>" +
            "<tr><td><p>1</p></td><td><p>2</p></td></tr>" +
            "<tr><td><p>South</p></td><td><p>3</p></td><td><p>4</p></td></tr>" +
            "</table></div>");
    }

    [Fact]
    public void Tables_MayHoldListsAndOtherTables()
    {
        var html = Html((main, body) =>
        {
            Numbering(main);
            var inner = new Table(new TableRow(Cell("Inner")));
            body.Append(new Table(new TableRow(new TableCell(Item("Point", 1), inner, new Paragraph()))));
        });

        html.Should().Be(
            "<div class=\"dfh-docx-table\"><table><tr><td><ul><li>Point</li></ul>" +
            "<div class=\"dfh-docx-table\"><table><tr><td><p>Inner</p></td></tr></table></div>" +
            "</td></tr></table></div>");
    }

    // --------------------------------------------------------------- links

    [Fact]
    public void Links_OnlyWebAndMailAreKept()
    {
        var html = Html((main, body) =>
        {
            var web = main.AddHyperlinkRelationship(new Uri("https://example.com/a?b=1&c=2"), true).Id;
            var mail = main.AddHyperlinkRelationship(new Uri("mailto:team@example.com"), true).Id;
            var script = main.AddHyperlinkRelationship(new Uri("javascript:alert(1)"), true).Id;
            var file = main.AddHyperlinkRelationship(new Uri("file:///C:/secret.txt"), true).Id;
            body.Append(new Paragraph(
                new Hyperlink(R("site", new Bold())) { Id = web },
                R(" "),
                new Hyperlink(R("mail")) { Id = mail },
                R(" "),
                new Hyperlink(R("script")) { Id = script },
                R(" "),
                new Hyperlink(R("file")) { Id = file },
                R(" "),
                new Hyperlink(R("bookmark")) { Anchor = "top" }));
        });

        html.Should().Be(
            "<p><a href=\"https://example.com/a?b=1&amp;c=2\" rel=\"noopener noreferrer nofollow\" target=\"_blank\"><strong>site</strong></a> " +
            "<a href=\"mailto:team@example.com\">mail</a> script file bookmark</p>");
        html.Should().NotContain("javascript:").And.NotContain("file:");
    }

    // ------------------------------------------------------------ pictures

    [Fact]
    public void Pictures_PngIsEmbeddedWithItsAltTextAndWidth()
    {
        var html = Html((main, body) =>
        {
            var part = main.AddImagePart(ImagePartType.Png);
            part.FeedData(new MemoryStream(Png));
            body.Append(new Paragraph(new Run(Picture(main.GetIdOfPart(part), "Logo \"new\" <b>"))));
        });

        html.Should().Be(
            "<p><img class=\"dfh-docx-img\" src=\"data:image/png;base64," + Convert.ToBase64String(Png) +
            "\" alt=\"Logo &quot;new&quot; &lt;b&gt;\" width=\"100\"></p>");
    }

    [Fact]
    public void Pictures_OverTheBudgetOrOfAnotherType_BecomePlaceholders()
    {
        var bytes = Build((main, body) =>
        {
            var first = main.AddImagePart(ImagePartType.Png);
            first.FeedData(new MemoryStream(Png));
            var second = main.AddImagePart(ImagePartType.Png);
            second.FeedData(new MemoryStream(Png.Concat(new byte[] { 0 }).ToArray()));
            var emf = main.AddImagePart(ImagePartType.Emf);
            emf.FeedData(new MemoryStream(new byte[] { 1, 2, 3 }));
            body.Append(
                new Paragraph(new Run(Picture(main.GetIdOfPart(first), "First"))),
                new Paragraph(new Run(Picture(main.GetIdOfPart(second), "Second"))),
                new Paragraph(new Run(Picture(main.GetIdOfPart(emf), null))),
                new Paragraph(new Run(Picture("rIdMissing", "Lost"))));
        });

        var html = DocxRendering.Render(bytes, pictureBudget: Png.Length).Html;

        html.Should().Contain("alt=\"First\"");
        html.Should().Contain("<span class=\"dfh-docx-picture\">[picture: Second]</span>");
        html.Should().Contain("<span class=\"dfh-docx-picture\">[picture]</span>");
        html.Should().Contain("<span class=\"dfh-docx-picture\">[picture: Lost]</span>");
        html.Split("<img").Length.Should().Be(2);
    }

    // -------------------------------------------------------------- limits

    [Fact]
    public void LongDocuments_AreCutAtABlock_AndSaySo()
    {
        var bytes = Build((main, body) =>
        {
            Numbering(main);
            for (var i = 0; i < 200; i++) body.Append(Item($"Item number {i}", 1));
            body.Append(P("The end"));
        });

        var result = DocxRendering.Render(bytes, htmlLimit: 500);

        result.Truncated.Should().BeTrue();
        result.Html.Should().EndWith("</li></ul>").And.NotContain("The end");
        result.Html.Length.Should().BeLessThan(700);
        DocxRendering.Render(bytes).Truncated.Should().BeFalse();
    }

    [Fact]
    public void PicturesDoNotCountTowardsTheHtmlLimit()
    {
        var bytes = Build((main, body) =>
        {
            var part = main.AddImagePart(ImagePartType.Png);
            part.FeedData(new MemoryStream(Png));
            body.Append(new Paragraph(new Run(Picture(main.GetIdOfPart(part), null))), P("After the picture"));
        });

        var result = DocxRendering.Render(bytes, htmlLimit: 120);

        result.Truncated.Should().BeFalse();
        result.Html.Should().Contain("After the picture");
    }

    [Fact]
    public void ContentControls_ShowTheirContent()
    {
        var html = Html((_, body) => body.Append(
            new SdtBlock(new SdtContentBlock(P("Inside a block control"))),
            new Paragraph(R("Name: "), new SdtRun(new SdtContentRun(R("Anna"))))));

        html.Should().Be("<p>Inside a block control</p><p>Name: Anna</p>");
    }

    [Fact]
    public void EmptyDocument_HasNoHtml_AndADamagedFileThrows()
    {
        DocxRendering.Render(Build((_, _) => { })).Html.Should().BeEmpty();
        var damaged = () => DocxRendering.Render(new byte[] { 0x50, 0x4B, 0x03, 0x04, 1, 2, 3, 4, 5 });
        damaged.Should().Throw<Exception>();
    }

    // ---------------------------------------------------------- demo files

    [Fact]
    public void DemoDocx_HasATitleHeadingsAListATableAndBoldText()
    {
        var lines = new[]
        {
            "A description.",
            $"{DemoFileFactory.BoldMark}Author:{DemoFileFactory.BoldMark} Anna Nowak",
            DemoFileFactory.PageBreak,
            DemoFileFactory.HeadingPrefix + "Action points",
            DemoFileFactory.BulletPrefix + "First point",
            DemoFileFactory.BulletPrefix + "Second point",
            DemoFileFactory.HeadingPrefix + "Version history",
            DemoFileFactory.RowPrefix + string.Join(DemoFileFactory.CellSeparator, "Version", "Summary"),
            DemoFileFactory.RowPrefix + string.Join(DemoFileFactory.CellSeparator, "v1", "Initial version"),
            string.Empty,
            "*Sample content.*",
        };

        var html = DocxRendering.Render(DemoFileFactory.Create("docx", "Team handbook", lines)).Html;

        html.Should().Be(
            "<h1 class=\"dfh-docx-title\">Team handbook</h1><p>A description.</p><p><strong>Author:</strong> Anna Nowak</p>" +
            "<h1>Action points</h1><ul><li>First point</li><li>Second point</li></ul>" +
            "<h1>Version history</h1><div class=\"dfh-docx-table\"><table>" +
            "<tr><th scope=\"col\"><p><strong>Version</strong></p></th><th scope=\"col\"><p><strong>Summary</strong></p></th></tr>" +
            "<tr><td><p>v1</p></td><td><p>Initial version</p></td></tr></table></div>" +
            "<p><em>Sample content.</em></p>");
    }
}

public class DocxPreviewServiceTests
{
    private readonly Mock<IDocumentBrowserService> _browser = new();
    private readonly Mock<IDocumentStorageService> _storage = new();
    private readonly DocumentPreviewService _service;

    public DocxPreviewServiceTests()
    {
        var context = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var extraction = new TextExtractionService(_storage.Object, context, Mock.Of<ILogger<TextExtractionService>>());
        _service = new DocumentPreviewService(_browser.Object, _storage.Object, extraction, Mock.Of<ILogger<DocumentPreviewService>>());
    }

    private static DocumentDetails Doc() => new()
    {
        Id = 7,
        Title = "Doc",
        FileType = ".docx",
        Versions = new List<DocumentDetailsVersion> { new() { Id = 101, Number = 1, FileType = ".docx", FileSize = 10, Hash = "abc" } },
    };

    private void File(byte[] bytes) =>
        _storage.Setup(s => s.DownloadDocumentAsync(7, 1)).ReturnsAsync(ServiceResult<byte[]>.Success(bytes));

    [Fact]
    public async Task Docx_IsFormattedHtml_WithoutTheTextView()
    {
        File(DemoFileFactory.Create("docx", "Handbook", new[] { "Welcome to the team." }));
        var preview = (await _service.GetPreviewAsync(Doc())).Data!;
        preview.Kind.Should().Be(DocumentPreviewKind.Docx);
        preview.Html.Should().Contain("<h1 class=\"dfh-docx-title\">Handbook</h1>").And.Contain("<p>Welcome to the team.</p>");
        preview.Text.Should().BeNull();
        preview.Note.Should().BeNull();
    }

    [Fact]
    public async Task Docx_TextView_HasThePlainText()
    {
        File(DemoFileFactory.Create("docx", "Handbook", new[] { "Welcome to the team." }));
        var preview = (await _service.GetPreviewAsync(Doc(), textView: true)).Data!;
        preview.Kind.Should().Be(DocumentPreviewKind.Docx);
        preview.Html.Should().BeNull();
        preview.Text.Should().Contain("Handbook").And.Contain("Welcome to the team.");
    }

    [Fact]
    public async Task Docx_WithoutContent_HasNoPreview_AndItsTextViewExplains()
    {
        File(DocxRenderingTests.Build((_, _) => { }));
        var document = (await _service.GetPreviewAsync(Doc())).Data!;
        document.Kind.Should().Be(DocumentPreviewKind.None);
        document.Note.Should().Contain("nothing to show");

        var text = (await _service.GetPreviewAsync(Doc(), textView: true)).Data!;
        text.Kind.Should().Be(DocumentPreviewKind.Docx);
        text.Text.Should().BeNull();
        text.Note.Should().Contain("Document view");
    }

    [Fact]
    public async Task DamagedDocx_HasNoPreviewInsteadOfFailing()
    {
        File(new byte[] { 0x50, 0x4B, 0x03, 0x04, 1, 2, 3, 4, 5 });
        var preview = (await _service.GetPreviewAsync(Doc())).Data!;
        preview.Kind.Should().Be(DocumentPreviewKind.None);
        preview.Note.Should().Contain("damaged or password-protected");
    }

    [Fact]
    public async Task HugeDocx_IsNotRendered_ButItsTextIsShown()
    {
        // One part over the size cap: the formatted view refuses it, the plain text view still works.
        var chunk = new string('x', 9_999) + " ";
        File(DocxRenderingTests.Build((_, body) =>
        {
            for (var i = 0; i < 2_050; i++) body.Append(new Paragraph(new Run(new Text(chunk))));
        }));

        var preview = (await _service.GetPreviewAsync(Doc())).Data!;

        preview.Kind.Should().Be(DocumentPreviewKind.Docx);
        preview.Html.Should().BeNull();
        preview.Text.Should().StartWith("xxxx");
        preview.Truncated.Should().BeTrue();
        preview.Note.Should().Contain("could not be built");
    }
}
