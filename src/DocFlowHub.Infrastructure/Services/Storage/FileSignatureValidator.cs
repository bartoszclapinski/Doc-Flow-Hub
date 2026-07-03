using Microsoft.AspNetCore.Http;

namespace DocFlowHub.Infrastructure.Services.Storage;

/// <summary>
/// Validates that an uploaded file's binary content matches its declared extension
/// (magic-byte / signature sniffing). This closes the gap where the extension whitelist
/// alone trusts the filename, so a mislabeled file (e.g. an executable renamed to .pdf)
/// would otherwise reach storage. Extension and size limits are enforced separately by
/// <see cref="DocumentStorageService"/>.
/// </summary>
public static class FileSignatureValidator
{
    private const int HeaderSampleSize = 16;

    // Extensions with no reliable binary signature (plain text): accepted if the sampled
    // header contains no NUL bytes (a cheap "this is really text, not a binary" heuristic).
    private static readonly HashSet<string> TextExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".txt", ".md"
    };

    // Extension -> acceptable leading byte signatures. An extension matches if the header
    // starts with any one of its candidate signatures.
    private static readonly Dictionary<string, byte[][]> Signatures = new(StringComparer.OrdinalIgnoreCase)
    {
        [".pdf"]  = new[] { new byte[] { 0x25, 0x50, 0x44, 0x46 } },                          // "%PDF"
        [".jpg"]  = new[] { new byte[] { 0xFF, 0xD8, 0xFF } },
        [".jpeg"] = new[] { new byte[] { 0xFF, 0xD8, 0xFF } },
        [".png"]  = new[] { new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A } },
        [".gif"]  = new[] { new byte[] { 0x47, 0x49, 0x46, 0x38 } },                          // "GIF8"
        // .docx (and modern Office) are ZIP containers; three standard ZIP record markers.
        [".docx"] = new[]
        {
            new byte[] { 0x50, 0x4B, 0x03, 0x04 },
            new byte[] { 0x50, 0x4B, 0x05, 0x06 },
            new byte[] { 0x50, 0x4B, 0x07, 0x08 }
        },
        // legacy .doc is an OLE2 compound document.
        [".doc"]  = new[] { new byte[] { 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1 } }
    };

    /// <summary>
    /// Returns true if the file's content is consistent with <paramref name="extension"/>.
    /// Unknown (but whitelisted) extensions are not blocked here — the extension whitelist
    /// remains the authority for which types are allowed at all.
    /// </summary>
    public static async Task<bool> IsContentValidAsync(IFormFile file, string extension)
    {
        var header = new byte[HeaderSampleSize];
        int read;
        await using (var stream = file.OpenReadStream())
        {
            read = await ReadAtLeastAsync(stream, header);
        }

        if (read == 0)
        {
            return false; // empty content
        }

        if (TextExtensions.Contains(extension))
        {
            // A byte-order mark marks a valid Unicode text encoding. UTF-16/UTF-32 text
            // legitimately contains NUL bytes, so a recognized BOM is accepted outright —
            // this matches TextExtractionService, which reads those encodings back.
            if (HasTextBom(header, read))
            {
                return true;
            }

            for (var i = 0; i < read; i++)
            {
                if (header[i] == 0x00)
                {
                    return false; // NUL byte with no BOM => not plain text
                }
            }
            return true;
        }

        if (!Signatures.TryGetValue(extension, out var candidates))
        {
            return true; // no signature on record for this whitelisted extension
        }

        foreach (var signature in candidates)
        {
            if (read >= signature.Length && header.AsSpan(0, signature.Length).SequenceEqual(signature))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// True if the header starts with a UTF-8, UTF-16, or UTF-32 byte-order mark.
    /// (UTF-32 is checked before UTF-16 because the UTF-16 LE BOM is a prefix of it.)
    /// </summary>
    private static bool HasTextBom(byte[] header, int read)
    {
        bool StartsWith(params byte[] bom)
        {
            if (read < bom.Length) return false;
            for (var i = 0; i < bom.Length; i++)
            {
                if (header[i] != bom[i]) return false;
            }
            return true;
        }

        return StartsWith(0x00, 0x00, 0xFE, 0xFF) // UTF-32 BE
            || StartsWith(0xFF, 0xFE, 0x00, 0x00) // UTF-32 LE
            || StartsWith(0xEF, 0xBB, 0xBF)        // UTF-8
            || StartsWith(0xFE, 0xFF)              // UTF-16 BE
            || StartsWith(0xFF, 0xFE);             // UTF-16 LE
    }

    /// <summary>
    /// Reads up to <paramref name="buffer"/>.Length bytes, tolerating short reads from the
    /// underlying stream (a single ReadAsync is not guaranteed to fill the buffer).
    /// </summary>
    private static async Task<int> ReadAtLeastAsync(Stream stream, byte[] buffer)
    {
        var total = 0;
        while (total < buffer.Length)
        {
            var n = await stream.ReadAsync(buffer.AsMemory(total, buffer.Length - total));
            if (n == 0)
            {
                break;
            }
            total += n;
        }
        return total;
    }
}
