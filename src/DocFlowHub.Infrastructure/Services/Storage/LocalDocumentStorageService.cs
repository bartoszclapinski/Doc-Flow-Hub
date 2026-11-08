using System.Security.Cryptography;
using DocFlowHub.Core.Models.Common;
using DocFlowHub.Core.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DocFlowHub.Infrastructure.Services.Storage;

/// <summary>
/// Stores document files in a folder on the server's disk. Used by the self-contained public
/// demo, which keeps everything inside one container; the regular app uses Azure Blob Storage.
/// Files are laid out like the blobs: <c>{documentId}/{versionNumber}</c>.
/// </summary>
public class LocalDocumentStorageService : IDocumentStorageService
{
    private readonly DocumentStorageOptions _options;
    private readonly ILogger<LocalDocumentStorageService> _logger;
    private readonly string _root;

    public LocalDocumentStorageService(
        IOptions<DocumentStorageOptions> options,
        ILogger<LocalDocumentStorageService> logger)
    {
        _options = options.Value;
        _logger = logger;

        if (string.IsNullOrWhiteSpace(_options.LocalPath))
            throw new ArgumentException("DocumentStorage:LocalPath is not configured");

        _root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(_options.LocalPath));
        Directory.CreateDirectory(_root);
    }

    public async Task<ServiceResult<string>> UploadDocumentAsync(IFormFile file, int documentId, int versionNumber)
    {
        var relativePath = GetRelativePath(documentId, versionNumber);
        try
        {
            var error = await DocumentUploadRules.ValidateAsync(file, _options);
            if (error != null)
            {
                return ServiceResult<string>.Failure(error);
            }

            var fullPath = GetFullPath(relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

            await using var source = file.OpenReadStream();
            await using var target = File.Create(fullPath);
            await source.CopyToAsync(target);

            return ServiceResult<string>.Success(relativePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading document {FileName}", relativePath);
            return ServiceResult<string>.Failure($"Error uploading document: {ex.Message}");
        }
    }

    public async Task<ServiceResult<byte[]>> DownloadDocumentAsync(int documentId, int versionNumber)
    {
        var relativePath = GetRelativePath(documentId, versionNumber);
        try
        {
            var fullPath = GetFullPath(relativePath);
            if (!File.Exists(fullPath))
            {
                return ServiceResult<byte[]>.Failure("Document not found");
            }

            return ServiceResult<byte[]>.Success(await File.ReadAllBytesAsync(fullPath));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading document {FileName}", relativePath);
            return ServiceResult<byte[]>.Failure($"Error downloading document: {ex.Message}");
        }
    }

    public Task<ServiceResult> DeleteDocumentAsync(int documentId, int versionNumber)
    {
        var relativePath = GetRelativePath(documentId, versionNumber);
        try
        {
            var fullPath = GetFullPath(relativePath);
            if (!File.Exists(fullPath))
            {
                return Task.FromResult(ServiceResult.Failure("Document not found"));
            }

            File.Delete(fullPath);
            return Task.FromResult(ServiceResult.Success());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting document {FileName}", relativePath);
            return Task.FromResult(ServiceResult.Failure($"Error deleting document: {ex.Message}"));
        }
    }

    public Task<ServiceResult<bool>> DocumentExistsAsync(int documentId, int versionNumber) =>
        Task.FromResult(ServiceResult<bool>.Success(File.Exists(GetFullPath(GetRelativePath(documentId, versionNumber)))));

    public Task<ServiceResult<string>> GetDocumentUrlAsync(int documentId, int versionNumber, int expiryMinutes = 60) =>
        Task.FromResult(ServiceResult<string>.Failure("Direct document links are not available with local storage"));

    public async Task<ServiceResult<string>> GetFileHashAsync(IFormFile file)
    {
        try
        {
            await using var stream = file.OpenReadStream();
            var hash = await MD5.HashDataAsync(stream);
            return ServiceResult<string>.Success(Convert.ToHexString(hash));
        }
        catch (Exception ex)
        {
            return ServiceResult<string>.Failure($"Failed to compute file hash: {ex.Message}");
        }
    }

    public Task<ServiceResult<string>> CopyDocumentAsync(string sourceFilePath, string userId)
    {
        try
        {
            var source = GetFullPath(sourceFilePath);
            if (!File.Exists(source))
            {
                return Task.FromResult(ServiceResult<string>.Failure("Source document not found."));
            }

            var newRelativePath =
                $"{userId}/{DateTime.UtcNow:yyyy/MM/dd}/{Guid.NewGuid()}{Path.GetExtension(sourceFilePath)}";
            var destination = GetFullPath(newRelativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            File.Copy(source, destination);

            return Task.FromResult(ServiceResult<string>.Success(newRelativePath));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ServiceResult<string>.Failure($"Failed to copy document: {ex.Message}"));
        }
    }

    public void Dispose()
    {
        // Nothing to release: files are opened and closed per call.
    }

    private static string GetRelativePath(int documentId, int versionNumber) => $"{documentId}/{versionNumber}";

    /// <summary>Resolves a stored path under the root folder; anything that escapes it is rejected.</summary>
    private string GetFullPath(string relativePath)
    {
        var fullPath = Path.GetFullPath(Path.Combine(_root, relativePath));
        if (!fullPath.StartsWith(_root + Path.DirectorySeparatorChar, StringComparison.Ordinal))
        {
            throw new ArgumentException("Invalid document path", nameof(relativePath));
        }

        return fullPath;
    }
}
