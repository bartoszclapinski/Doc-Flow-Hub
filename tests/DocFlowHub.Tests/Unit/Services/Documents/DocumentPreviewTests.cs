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

/// <summary>Sprint 18: text decoding, Markdown sanitising and the preview service.</summary>
public class PreviewRenderingTests
{
    [Fact]
    public void DecodeText_Utf8WithoutBom_IsUtf8()
    {
        var (text, encoding, truncated) = PreviewRendering.DecodeText(Encoding.UTF8.GetBytes("Zażółć gęślą jaźń\r\nline 2"), 1024);
        text.Should().Be("Zażółć gęślą jaźń\nline 2");
        encoding.Should().Be("UTF-8");
        truncated.Should().BeFalse();
    }

    [Fact]
    public void DecodeText_Utf8Bom_IsSkipped()
    {
        var bytes = new byte[] { 0xEF, 0xBB, 0xBF }.Concat(Encoding.UTF8.GetBytes("abc")).ToArray();
        PreviewRendering.DecodeText(bytes, 1024).Text.Should().Be("abc");
    }

    [Fact]
    public void DecodeText_Utf16LittleEndianBom_IsUtf16()
    {
        var bytes = new byte[] { 0xFF, 0xFE }.Concat(Encoding.Unicode.GetBytes("łódź")).ToArray();
        var (text, encoding, _) = PreviewRendering.DecodeText(bytes, 1024);
        text.Should().Be("łódź");
        encoding.Should().Be("UTF-16");
    }

    [Fact]
    public void DecodeText_InvalidUtf8_FallsBackToWindows1250()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        var bytes = Encoding.GetEncoding(1250).GetBytes("Zażółć gęślą jaźń");
        var (text, encoding, _) = PreviewRendering.DecodeText(bytes, 1024);
        text.Should().Be("Zażółć gęślą jaźń");
        encoding.Should().Be("Windows-1250");
    }

    [Fact]
    public void DecodeText_OverLimit_IsCutWithoutSplittingACharacter()
    {
        // "ż" is two bytes in UTF-8; a limit of 3 falls in the middle of the second one.
        var (text, _, truncated) = PreviewRendering.DecodeText(Encoding.UTF8.GetBytes("żżżż"), 3);
        truncated.Should().BeTrue();
        text.Should().Be("ż");
    }

    [Fact]
    public void RenderMarkdown_RendersHeadingsTablesAndTaskLists()
    {
        var html = PreviewRendering.RenderMarkdown("# Title\n\n| a | b |\n|---|---|\n| 1 | 2 |\n\n- [x] done\n");
        html.Should().Contain("<h1>Title</h1>").And.Contain("<table>").And.Contain("type=\"checkbox\"");
    }

    [Fact]
    public void RenderMarkdown_RawHtmlIsShownAsText()
    {
        var html = PreviewRendering.RenderMarkdown("Hello <script>alert(1)</script> <img src=x onerror=alert(1)>");
        html.Should().NotContain("<script").And.NotContain("<img");
        html.Should().Contain("&lt;script&gt;");
    }

    [Theory]
    [InlineData("[click](javascript:alert(1))")]
    [InlineData("[click](JaVaScRiPt:alert(1))")]
    [InlineData("[click](java\tscript:alert(1))")]
    [InlineData("[click](&#106;avascript:alert(1))")]
    [InlineData("[click](data:text/html;base64,PHNjcmlwdD4=)")]
    [InlineData("[click][r]\n\n[r]: javascript:alert(1)")]
    [InlineData("<javascript:alert(1)>")]
    public void RenderMarkdown_UnsafeLinksBecomeText(string markdown)
    {
        var html = PreviewRendering.RenderMarkdown(markdown);
        html.Should().NotContain("href=\"javascript", "the link must be dropped")
            .And.NotContain("href=\"data:")
            .And.NotContain("href=\"JaVaScRiPt");
        html.ToLowerInvariant().Should().NotContain("<a href=\"java");
    }

    [Fact]
    public void RenderMarkdown_WebLinksOpenInANewTabWithoutReferrer()
    {
        var html = PreviewRendering.RenderMarkdown("[site](https://example.com) and [mail](mailto:a@b.c) and [top](#top)");
        html.Should().Contain("href=\"https://example.com\"").And.Contain("rel=\"noopener noreferrer nofollow\"").And.Contain("target=\"_blank\"");
        html.Should().Contain("href=\"mailto:a@b.c\"").And.Contain("href=\"#top\"");
    }

    [Fact]
    public void RenderMarkdown_ImagesAreNeverLoaded()
    {
        var html = PreviewRendering.RenderMarkdown("![Chart](https://tracker.example/pixel.png) ![Local](diagram.png)");
        html.Should().NotContain("<img");
        html.Should().Contain("href=\"https://tracker.example/pixel.png\"").And.Contain("image: Chart");
        html.Should().Contain("[image: Local]");
    }
}

public class DocumentPreviewServiceTests
{
    private readonly Mock<IDocumentBrowserService> _browser = new();
    private readonly Mock<IDocumentStorageService> _storage = new();
    private readonly DocumentPreviewService _service;

    public DocumentPreviewServiceTests()
    {
        var context = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var extraction = new TextExtractionService(_storage.Object, context, Mock.Of<ILogger<TextExtractionService>>());
        _service = new DocumentPreviewService(_browser.Object, _storage.Object, extraction, Mock.Of<ILogger<DocumentPreviewService>>());
    }

    private static DocumentDetails Doc(string type, params int[] versions) => new()
    {
        Id = 7,
        Title = "Doc",
        FileType = type,
        Versions = versions.OrderByDescending(n => n)
            .Select(n => new DocumentDetailsVersion { Id = 100 + n, Number = n, FileType = type, FileSize = 10, Hash = "abc123" + n })
            .ToList(),
    };

    private void File(int version, byte[] bytes) =>
        _storage.Setup(s => s.DownloadDocumentAsync(7, version)).ReturnsAsync(ServiceResult<byte[]>.Success(bytes));

    [Fact]
    public async Task NoAccess_Fails()
    {
        _browser.Setup(b => b.GetDetailsAsync("u", 7)).ReturnsAsync(ServiceResult<DocumentDetails>.Failure("Document not found"));
        (await _service.GetPreviewAsync("u", 7)).Succeeded.Should().BeFalse();
        (await _service.GetInlineFileAsync("u", 7)).Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task Text_LatestVersionByDefault()
    {
        File(2, Encoding.UTF8.GetBytes("second"));
        var result = await _service.GetPreviewAsync(Doc(".txt", 1, 2));
        result.Data!.Kind.Should().Be(DocumentPreviewKind.Text);
        result.Data.Text.Should().Be("second");
        result.Data.IsLatest.Should().BeTrue();
    }

    [Fact]
    public async Task OlderVersion_IsShownAndMarked()
    {
        File(1, Encoding.UTF8.GetBytes("first"));
        var result = await _service.GetPreviewAsync(Doc(".txt", 1, 2), 1);
        result.Data!.Text.Should().Be("first");
        result.Data.IsLatest.Should().BeFalse();
        result.Data.LatestVersionNumber.Should().Be(2);
    }

    [Fact]
    public async Task UnknownVersion_Fails()
    {
        (await _service.GetPreviewAsync(Doc(".txt", 1), 5)).Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task Markdown_HasSanitisedHtmlAndSource()
    {
        File(1, Encoding.UTF8.GetBytes("# Hi\n\n<script>x</script>"));
        var preview = (await _service.GetPreviewAsync(Doc(".md", 1))).Data!;
        preview.Kind.Should().Be(DocumentPreviewKind.Markdown);
        preview.Html.Should().Contain("<h1>Hi</h1>").And.NotContain("<script>");
        preview.Text.Should().Contain("<script>x</script>");
    }

    [Fact]
    public async Task Pdf_ShowsExtractedText()
    {
        File(1, DemoFileFactory.Create("pdf", "Quarterly report", new[] { "Revenue grew in the third quarter." }));
        var preview = (await _service.GetPreviewAsync(Doc(".pdf", 1))).Data!;
        preview.Kind.Should().Be(DocumentPreviewKind.ExtractedText);
        preview.Text.Should().Contain("Revenue grew");
    }

    [Fact]
    public async Task Docx_ShowsExtractedText()
    {
        File(1, DemoFileFactory.Create("docx", "Handbook", new[] { "Welcome to the team." }));
        var preview = (await _service.GetPreviewAsync(Doc(".docx", 1))).Data!;
        preview.Kind.Should().Be(DocumentPreviewKind.ExtractedText);
        preview.Text.Should().Contain("Welcome to the team.");
    }

    [Fact]
    public async Task LegacyDoc_HasNoPreview()
    {
        var preview = (await _service.GetPreviewAsync(Doc(".doc", 1))).Data!;
        preview.Kind.Should().Be(DocumentPreviewKind.None);
        preview.Note.Should().Contain("DOC");
    }

    [Fact]
    public async Task UnreadableFile_HasNoPreviewInsteadOfFailing()
    {
        _storage.Setup(s => s.DownloadDocumentAsync(7, 1)).ReturnsAsync(ServiceResult<byte[]>.Failure("missing blob"));
        var preview = (await _service.GetPreviewAsync(Doc(".txt", 1))).Data!;
        preview.Kind.Should().Be(DocumentPreviewKind.None);
        preview.Note.Should().Contain("could not be read");
    }

    [Fact]
    public async Task Image_IsServedInlineWithItsType()
    {
        _browser.Setup(b => b.GetDetailsAsync("u", 7)).ReturnsAsync(ServiceResult<DocumentDetails>.Success(Doc(".png", 1)));
        File(1, new byte[] { 0x89, 0x50, 0x4E, 0x47 });
        (await _service.GetPreviewAsync("u", 7)).Data!.Kind.Should().Be(DocumentPreviewKind.Image);
        var file = await _service.GetInlineFileAsync("u", 7);
        file.Data!.ContentType.Should().Be("image/png");
        file.Data.ETag.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task InlineFile_RefusesNonImages()
    {
        _browser.Setup(b => b.GetDetailsAsync("u", 7)).ReturnsAsync(ServiceResult<DocumentDetails>.Success(Doc(".txt", 1)));
        (await _service.GetInlineFileAsync("u", 7)).Succeeded.Should().BeFalse();
    }
}
