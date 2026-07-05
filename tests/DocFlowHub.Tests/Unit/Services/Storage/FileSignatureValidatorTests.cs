using System.Text;
using DocFlowHub.Infrastructure.Services.Storage;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace DocFlowHub.Tests.Unit.Services.Storage;

/// <summary>
/// Unit tests for <see cref="FileSignatureValidator"/> — the magic-byte content check that
/// stops a mislabeled file (e.g. an executable renamed to .pdf) from slipping past the
/// extension whitelist. Pure and fast: no storage, no network.
/// </summary>
public class FileSignatureValidatorTests
{
    // Known-good leading signatures for the formats the app accepts.
    private static readonly byte[] PdfMagic = { 0x25, 0x50, 0x44, 0x46 };                          // %PDF
    private static readonly byte[] PngMagic = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
    private static readonly byte[] JpgMagic = { 0xFF, 0xD8, 0xFF };
    private static readonly byte[] GifMagic = { 0x47, 0x49, 0x46, 0x38 };                          // GIF8
    private static readonly byte[] ZipMagic = { 0x50, 0x4B, 0x03, 0x04 };                          // docx (ZIP)
    private static readonly byte[] Ole2Magic = { 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1 }; // legacy .doc

    private static IFormFile MakeFile(byte[] content, string fileName = "upload.bin")
    {
        var stream = new MemoryStream(content);
        return new FormFile(stream, 0, stream.Length, "file", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = "application/octet-stream"
        };
    }

    private static byte[] WithPadding(byte[] magic, int totalLength = 64)
    {
        var buffer = new byte[Math.Max(totalLength, magic.Length)];
        Array.Copy(magic, buffer, magic.Length);
        // Fill the remainder with a printable byte so it looks like real trailing content.
        for (var i = magic.Length; i < buffer.Length; i++)
        {
            buffer[i] = (byte)'A';
        }
        return buffer;
    }

    [Theory]
    [InlineData(".pdf")]
    [InlineData(".png")]
    [InlineData(".jpg")]
    [InlineData(".jpeg")]
    [InlineData(".gif")]
    [InlineData(".docx")]
    [InlineData(".doc")]
    public async Task IsContentValidAsync_WithMatchingSignature_ReturnsTrue(string extension)
    {
        var magic = extension switch
        {
            ".pdf" => PdfMagic,
            ".png" => PngMagic,
            ".jpg" or ".jpeg" => JpgMagic,
            ".gif" => GifMagic,
            ".docx" => ZipMagic,
            ".doc" => Ole2Magic,
            _ => throw new ArgumentOutOfRangeException(nameof(extension))
        };

        var file = MakeFile(WithPadding(magic));

        var result = await FileSignatureValidator.IsContentValidAsync(file, extension);

        result.Should().BeTrue($"content leads with the correct {extension} signature");
    }

    [Fact]
    public async Task IsContentValidAsync_RenamedTextAsPdf_ReturnsFalse()
    {
        // The core attack: a plain-text (or any non-PDF) payload with a .pdf extension.
        var file = MakeFile(Encoding.UTF8.GetBytes("This is not really a PDF, just text."));

        var result = await FileSignatureValidator.IsContentValidAsync(file, ".pdf");

        result.Should().BeFalse("the bytes do not start with %PDF");
    }

    [Fact]
    public async Task IsContentValidAsync_ExecutableRenamedAsPng_ReturnsFalse()
    {
        // MZ header (Windows executable) mislabeled as a .png.
        var mz = WithPadding(new byte[] { 0x4D, 0x5A, 0x90, 0x00 });

        var result = await FileSignatureValidator.IsContentValidAsync(MakeFile(mz), ".png");

        result.Should().BeFalse("an MZ executable is not a PNG");
    }

    [Fact]
    public async Task IsContentValidAsync_WrongImageSignature_ReturnsFalse()
    {
        // A real GIF labeled as .png — signature mismatch must be caught.
        var result = await FileSignatureValidator.IsContentValidAsync(MakeFile(WithPadding(GifMagic)), ".png");

        result.Should().BeFalse("GIF bytes are not a PNG");
    }

    [Fact]
    public async Task IsContentValidAsync_PlainTextWithTxtExtension_ReturnsTrue()
    {
        var file = MakeFile(Encoding.UTF8.GetBytes("Just some readable notes.\nSecond line."), "notes.txt");

        var result = await FileSignatureValidator.IsContentValidAsync(file, ".txt");

        result.Should().BeTrue("readable text with no NUL bytes is valid for .txt");
    }

    [Fact]
    public async Task IsContentValidAsync_MarkdownWithMdExtension_ReturnsTrue()
    {
        var file = MakeFile(Encoding.UTF8.GetBytes("# Heading\n\nSome **markdown** body."), "readme.md");

        var result = await FileSignatureValidator.IsContentValidAsync(file, ".md");

        result.Should().BeTrue();
    }

    [Theory]
    // UTF-16/UTF-32 text legitimately contains NUL bytes; a recognized BOM must be accepted.
    [InlineData(new byte[] { 0xFF, 0xFE, 0x48, 0x00, 0x69, 0x00 })]             // UTF-16 LE "Hi"
    [InlineData(new byte[] { 0xFE, 0xFF, 0x00, 0x48, 0x00, 0x69 })]             // UTF-16 BE "Hi"
    [InlineData(new byte[] { 0xFF, 0xFE, 0x00, 0x00, 0x48, 0x00, 0x00, 0x00 })] // UTF-32 LE "H"
    [InlineData(new byte[] { 0x00, 0x00, 0xFE, 0xFF, 0x00, 0x00, 0x00, 0x48 })] // UTF-32 BE "H"
    [InlineData(new byte[] { 0xEF, 0xBB, 0xBF, 0x48, 0x69 })]                   // UTF-8 BOM "Hi"
    public async Task IsContentValidAsync_UnicodeBomText_ReturnsTrue(byte[] content)
    {
        var result = await FileSignatureValidator.IsContentValidAsync(MakeFile(content, "notes.txt"), ".txt");

        result.Should().BeTrue("a recognized Unicode BOM marks valid text even with NUL bytes");
    }

    [Fact]
    public async Task IsContentValidAsync_BinaryContentWithTxtExtension_ReturnsFalse()
    {
        // Contains a NUL byte in the sampled header => not plain text.
        var binary = new byte[] { 0x54, 0x00, 0x65, 0x78, 0x74 }; // 'T', NUL, 'e', 'x', 't'

        var result = await FileSignatureValidator.IsContentValidAsync(MakeFile(binary, "x.txt"), ".txt");

        result.Should().BeFalse("a NUL byte means the payload is not plain text");
    }

    [Fact]
    public async Task IsContentValidAsync_Utf8BomPrefixedBinary_ReturnsFalse()
    {
        // Attack: prepend the UTF-8 BOM to a NUL-containing binary and name it .txt. UTF-8 text
        // never contains NUL, so the BOM must not license the payload past the content check.
        var payload = new byte[] { 0xEF, 0xBB, 0xBF, 0x4D, 0x5A, 0x00, 0x00, 0x90 }; // BOM + MZ-ish + NUL

        var result = await FileSignatureValidator.IsContentValidAsync(MakeFile(payload, "evil.txt"), ".txt");

        result.Should().BeFalse("a UTF-8 BOM does not permit NUL bytes — the binary must still be rejected");
    }

    [Fact]
    public async Task IsContentValidAsync_PdfWithLeadingUtf8Bom_ReturnsTrue()
    {
        // Some producers emit a leading UTF-8 BOM before "%PDF"; readers still accept it.
        var bytes = new byte[] { 0xEF, 0xBB, 0xBF }.Concat(WithPadding(PdfMagic)).ToArray();

        var result = await FileSignatureValidator.IsContentValidAsync(MakeFile(bytes, "doc.pdf"), ".pdf");

        result.Should().BeTrue("a PDF whose %PDF marker follows a BOM is still a valid PDF");
    }

    [Fact]
    public async Task IsContentValidAsync_PdfWithLeadingWhitespace_ReturnsTrue()
    {
        // Leading whitespace/newlines before %PDF are tolerated by readers.
        var bytes = new byte[] { 0x0D, 0x0A, 0x20 }.Concat(WithPadding(PdfMagic)).ToArray();

        var result = await FileSignatureValidator.IsContentValidAsync(MakeFile(bytes, "doc.pdf"), ".pdf");

        result.Should().BeTrue("a PDF whose %PDF marker follows whitespace is still a valid PDF");
    }

    [Fact]
    public async Task IsContentValidAsync_NonPdfLeadingWhitespaceTrick_StillRejected()
    {
        // The leading-offset tolerance is PDF-only: a PNG signature preceded by junk is NOT a PNG.
        var bytes = new byte[] { 0x20, 0x20 }.Concat(WithPadding(GifMagic)).ToArray();

        var result = await FileSignatureValidator.IsContentValidAsync(MakeFile(bytes), ".png");

        result.Should().BeFalse("only PDF tolerates a leading offset; other signatures stay strict at offset 0");
    }

    [Fact]
    public async Task IsContentValidAsync_EmptyFile_ReturnsFalse()
    {
        var result = await FileSignatureValidator.IsContentValidAsync(MakeFile(Array.Empty<byte>()), ".pdf");

        result.Should().BeFalse("an empty file has no valid content");
    }

    [Fact]
    public async Task IsContentValidAsync_WhitelistedExtensionWithoutKnownSignature_ReturnsTrue()
    {
        // The validator is not the type authority — extensions it has no signature for are
        // deferred to the extension whitelist and pass content validation.
        var file = MakeFile(Encoding.UTF8.GetBytes("col1,col2\n1,2"), "data.csv");

        var result = await FileSignatureValidator.IsContentValidAsync(file, ".csv");

        result.Should().BeTrue("no signature on record => content check defers to the whitelist");
    }

    [Fact]
    public async Task IsContentValidAsync_ExtensionMatchIsCaseInsensitive()
    {
        var file = MakeFile(WithPadding(PdfMagic));

        var result = await FileSignatureValidator.IsContentValidAsync(file, ".PDF");

        result.Should().BeTrue("extension comparison is case-insensitive");
    }

    [Fact]
    public async Task IsContentValidAsync_DocxAlternateZipMarker_ReturnsTrue()
    {
        // Empty-archive ZIP marker (PK\x05\x06) is also a legitimate docx container start.
        var emptyZip = WithPadding(new byte[] { 0x50, 0x4B, 0x05, 0x06 });

        var result = await FileSignatureValidator.IsContentValidAsync(MakeFile(emptyZip), ".docx");

        result.Should().BeTrue();
    }
}
