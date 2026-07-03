using System.Text;
using DocFlowHub.Core.Services.Interfaces;
using DocFlowHub.Infrastructure.Data;
using DocFlowHub.Infrastructure.Services.AI;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DocFlowHub.Tests.Unit.Services.AI;

/// <summary>
/// Exercises the real text-extraction paths (Workstream C): TXT, DOCX (OpenXML) and PDF
/// (PdfPig). Content is generated at runtime so the tests prove genuine round-trip
/// extraction rather than asserting against a stub.
/// </summary>
public class TextExtractionServiceTests
{
    private readonly TextExtractionService _service;

    public TextExtractionServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        var context = new ApplicationDbContext(options);
        var storageMock = new Mock<IDocumentStorageService>();
        var loggerMock = new Mock<ILogger<TextExtractionService>>();

        _service = new TextExtractionService(storageMock.Object, context, loggerMock.Object);
    }

    [Fact]
    public async Task ExtractTextAsync_PlainText_ReturnsContent()
    {
        var bytes = Encoding.UTF8.GetBytes("Hello plain text extraction.");

        var result = await _service.ExtractTextAsync(bytes, "notes.txt", "text/plain");

        result.Succeeded.Should().BeTrue(result.Error);
        result.Data.Should().Contain("Hello plain text extraction.");
    }

    [Fact]
    public async Task ExtractTextAsync_Docx_ReturnsParagraphText()
    {
        const string expected = "The quarterly report shows strong growth.";
        var bytes = CreateDocx(expected);

        var result = await _service.ExtractTextAsync(
            bytes, "report.docx",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document");

        result.Succeeded.Should().BeTrue(result.Error);
        result.Data.Should().Contain(expected);
    }

    [Fact]
    public async Task ExtractTextAsync_Pdf_ReturnsText()
    {
        const string expected = "Hello PDF extraction";
        var bytes = CreateMinimalPdf(expected);

        var result = await _service.ExtractTextAsync(bytes, "doc.pdf", "application/pdf");

        result.Succeeded.Should().BeTrue(result.Error);
        result.Data.Should().Contain("Hello");
    }

    [Fact]
    public async Task ExtractTextAsync_LegacyDoc_DegradesGracefully()
    {
        // Legacy binary .doc has no OSS extractor: expect a clean "no text" failure,
        // never a crash and never garbage bytes passed through.
        var bytes = new byte[] { 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1, 0x00, 0x00 };

        var result = await _service.ExtractTextAsync(bytes, "old.doc", "application/msword");

        result.Succeeded.Should().BeFalse();
    }

    private static byte[] CreateDocx(string text)
    {
        using var ms = new MemoryStream();
        using (var doc = WordprocessingDocument.Create(ms, WordprocessingDocumentType.Document))
        {
            var main = doc.AddMainDocumentPart();
            main.Document = new Document(new Body(new Paragraph(new Run(new Text(text)))));
            main.Document.Save();
        }
        return ms.ToArray();
    }

    /// <summary>
    /// Writes a minimal, valid single-page PDF showing <paramref name="text"/>, with a
    /// correctly computed xref table. Text must not contain '(', ')' or '\'.
    /// </summary>
    private static byte[] CreateMinimalPdf(string text)
    {
        var streamContent = $"BT /F1 24 Tf 72 700 Td ({text}) Tj ET";
        var objects = new[]
        {
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Contents 4 0 R /Resources << /Font << /F1 5 0 R >> >> >>",
            $"<< /Length {streamContent.Length} >>\nstream\n{streamContent}\nendstream",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>"
        };

        var sb = new StringBuilder();
        sb.Append("%PDF-1.4\n");
        var offsets = new int[objects.Length];
        for (var i = 0; i < objects.Length; i++)
        {
            offsets[i] = sb.Length;
            sb.Append($"{i + 1} 0 obj\n{objects[i]}\nendobj\n");
        }

        var xrefStart = sb.Length;
        sb.Append("xref\n");
        sb.Append($"0 {objects.Length + 1}\n");
        sb.Append("0000000000 65535 f \n");
        foreach (var offset in offsets)
        {
            sb.Append(offset.ToString("D10")).Append(" 00000 n \n");
        }
        sb.Append("trailer\n");
        sb.Append($"<< /Size {objects.Length + 1} /Root 1 0 R >>\n");
        sb.Append("startxref\n");
        sb.Append($"{xrefStart}\n");
        sb.Append("%%EOF");

        return Encoding.ASCII.GetBytes(sb.ToString());
    }
}
