using Microsoft.AspNetCore.Http;

namespace DocFlowHub.Infrastructure.Services.Storage;

/// <summary>Upload checks shared by every storage provider.</summary>
internal static class DocumentUploadRules
{
    /// <returns>An error message, or <c>null</c> when the file may be stored.</returns>
    public static async Task<string?> ValidateAsync(IFormFile? file, DocumentStorageOptions options)
    {
        if (file == null || file.Length == 0)
        {
            return "No file was provided";
        }

        if (file.Length > options.MaxFileSizeBytes)
        {
            return $"File size exceeds maximum allowed size of {options.MaxFileSizeBytes / 1024 / 1024}MB";
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!options.AllowedFileTypes.Contains(extension))
        {
            return $"File type {extension} is not allowed";
        }

        // Content sniffing: reject files whose bytes don't match the declared extension
        // (e.g. an executable renamed to .pdf), so the whitelist can't be bypassed by rename.
        if (!await FileSignatureValidator.IsContentValidAsync(file, extension))
        {
            return $"File content does not match its {extension} extension";
        }

        return null;
    }
}
