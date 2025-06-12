using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using DocFlowHub.Core.Models.Common;
using DocFlowHub.Core.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Azure.Storage;

namespace DocFlowHub.Infrastructure.Services.Storage;

public class DocumentStorageService : IDocumentStorageService
{
    private readonly BlobContainerClient _containerClient;
    private readonly DocumentStorageOptions _options;
    private readonly ILogger<DocumentStorageService> _logger;
    private readonly SemaphoreSlim _initializationLock = new(1, 1);
    private bool _isInitialized;
    private readonly StorageSharedKeyCredential _credential;

    public DocumentStorageService(
        IOptions<DocumentStorageOptions> options,
        ILogger<DocumentStorageService> logger)
    {
        _options = options.Value;
        _logger = logger;

        if (string.IsNullOrEmpty(_options.ConnectionString))
            throw new ArgumentException("Azure Storage connection string is not configured");

        if (string.IsNullOrEmpty(_options.AccountName) || string.IsNullOrEmpty(_options.AccountKey))
            throw new ArgumentException("Azure Storage account name or key is not configured");

        // Create credential for SAS token generation using options
        _credential = new StorageSharedKeyCredential(_options.AccountName, _options.AccountKey);
        
        // Create blob service client using connection string from options
        var blobServiceClient = new BlobServiceClient(_options.ConnectionString);
        _containerClient = blobServiceClient.GetBlobContainerClient(_options.ContainerName);
    }

    private async Task EnsureInitializedAsync()
    {
        if (_isInitialized) return;

        try
        {
            await _initializationLock.WaitAsync();
            if (!_isInitialized)
            {
                await _containerClient.CreateIfNotExistsAsync();
                _isInitialized = true;
            }
        }
        finally
        {
            _initializationLock.Release();
        }
    }

    public async Task<ServiceResult<string>> UploadDocumentAsync(IFormFile file, int documentId, int versionNumber)
    {
        try
        {
            await EnsureInitializedAsync();

            if (file == null || file.Length == 0)
            {
                return ServiceResult<string>.Failure("No file was provided");
            }

            if (file.Length > _options.MaxFileSizeBytes)
            {
                return ServiceResult<string>.Failure($"File size exceeds maximum allowed size of {_options.MaxFileSizeBytes / 1024 / 1024}MB");
            }

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!_options.AllowedFileTypes.Contains(extension))
            {
                return ServiceResult<string>.Failure($"File type {extension} is not allowed");
            }

            var blobName = GetBlobName(documentId, versionNumber);
            var blobClient = _containerClient.GetBlobClient(blobName);

            await using var stream = file.OpenReadStream();
            await blobClient.UploadAsync(stream, new BlobHttpHeaders { ContentType = file.ContentType });

            return ServiceResult<string>.Success(blobName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading document {FileName}", GetBlobName(documentId, versionNumber));
            return ServiceResult<string>.Failure($"Error uploading document: {ex.Message}");
        }
    }

    /// <summary>
    /// Downloads a document from storage.
    /// </summary>
    /// <param name="documentId">The ID of the document.</param>
    /// <param name="versionNumber">The version number of the document.</param>
    /// <returns>A ServiceResult containing the document stream. Note: The caller is responsible for disposing the returned Stream.</returns>
    public async Task<ServiceResult<byte[]>> DownloadDocumentAsync(int documentId, int versionNumber)
    {
        try
        {
            await EnsureInitializedAsync();
            
            var blobName = GetBlobName(documentId, versionNumber);
            var blobClient = _containerClient.GetBlobClient(blobName);
            
            if (!await blobClient.ExistsAsync())
            {
                return ServiceResult<byte[]>.Failure("Document not found");
            }

            using var memoryStream = new MemoryStream();
            await blobClient.DownloadToAsync(memoryStream);
            return ServiceResult<byte[]>.Success(memoryStream.ToArray());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading document {FileName}", GetBlobName(documentId, versionNumber));
            return ServiceResult<byte[]>.Failure($"Error downloading document: {ex.Message}");
        }
    }

    public async Task<ServiceResult> DeleteDocumentAsync(int documentId, int versionNumber)
    {
        try
        {
            await EnsureInitializedAsync();
            
            var blobName = GetBlobName(documentId, versionNumber);
            var blobClient = _containerClient.GetBlobClient(blobName);
            
            if (!await blobClient.ExistsAsync())
            {
                return ServiceResult.Failure("Document not found");
            }

            await blobClient.DeleteAsync();
            return ServiceResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting document {FileName}", GetBlobName(documentId, versionNumber));
            return ServiceResult.Failure($"Error deleting document: {ex.Message}");
        }
    }

    public async Task<ServiceResult<bool>> DocumentExistsAsync(int documentId, int versionNumber)
    {
        try
        {
            await EnsureInitializedAsync();
            
            var blobName = GetBlobName(documentId, versionNumber);
            var blobClient = _containerClient.GetBlobClient(blobName);
            var exists = await blobClient.ExistsAsync();
            return ServiceResult<bool>.Success(exists.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking document existence {FileName}", GetBlobName(documentId, versionNumber));
            return ServiceResult<bool>.Failure($"Error checking document existence: {ex.Message}");
        }
    }

    public async Task<ServiceResult<string>> GetDocumentUrlAsync(int documentId, int versionNumber, int expiryMinutes = 60)
    {
        try
        {
            await EnsureInitializedAsync();
            
            var blobName = GetBlobName(documentId, versionNumber);
            var blobClient = _containerClient.GetBlobClient(blobName);
            
            if (!await blobClient.ExistsAsync())
            {
                return ServiceResult<string>.Failure("Document not found");
            }

            var sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = _containerClient.Name,
                BlobName = blobName,
                Resource = "b",
                StartsOn = DateTimeOffset.UtcNow,
                ExpiresOn = DateTimeOffset.UtcNow.AddMinutes(expiryMinutes)
            };

            sasBuilder.SetPermissions(BlobSasPermissions.Read);

            // Use the credential created in constructor from options
            var sasToken = sasBuilder.ToSasQueryParameters(_credential).ToString();
            return ServiceResult<string>.Success($"{blobClient.Uri}?{sasToken}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating document URL for {FileName}", GetBlobName(documentId, versionNumber));
            return ServiceResult<string>.Failure($"Error generating document URL: {ex.Message}");
        }
    }

    public async Task<ServiceResult<string>> GetFileHashAsync(IFormFile file)
    {
        try
        {
            await EnsureInitializedAsync();
            
            using var md5 = MD5.Create();
            await using var stream = file.OpenReadStream();
            var hash = await md5.ComputeHashAsync(stream);
            return ServiceResult<string>.Success(Convert.ToHexString(hash));
        }
        catch (Exception ex)
        {
            return ServiceResult<string>.Failure($"Failed to compute file hash: {ex.Message}");
        }
    }

    public async Task<ServiceResult<string>> CopyDocumentAsync(string sourceFilePath, string userId)
    {
        try
        {
            await EnsureInitializedAsync();
            
            var sourceBlobClient = _containerClient.GetBlobClient(sourceFilePath);
            
            if (!await sourceBlobClient.ExistsAsync())
                return ServiceResult<string>.Failure("Source document not found.");

            var fileExtension = Path.GetExtension(sourceFilePath);
            var newBlobName = $"{userId}/{DateTime.UtcNow:yyyy/MM/dd}/{Guid.NewGuid()}{fileExtension}";
            var destinationBlobClient = _containerClient.GetBlobClient(newBlobName);

            // Start the copy operation
            var copyOperation = await destinationBlobClient.StartCopyFromUriAsync(sourceBlobClient.Uri);

            // Wait for the copy operation to complete
            while (!copyOperation.HasCompleted)
            {
                var properties = await destinationBlobClient.GetPropertiesAsync();
                if (properties.Value.CopyStatus == CopyStatus.Failed || 
                    properties.Value.CopyStatus == CopyStatus.Aborted)
                {
                    await destinationBlobClient.DeleteIfExistsAsync();
                    return ServiceResult<string>.Failure($"Copy failed with status: {properties.Value.CopyStatus}");
                }
                await Task.Delay(500); // Wait for 500ms before checking again
            }

            // Verify the copy was successful
            var finalProperties = await destinationBlobClient.GetPropertiesAsync();
            if (finalProperties.Value.CopyStatus != CopyStatus.Success)
            {
                await destinationBlobClient.DeleteIfExistsAsync();
                return ServiceResult<string>.Failure($"Copy operation failed with status: {finalProperties.Value.CopyStatus}");
            }

            return ServiceResult<string>.Success(newBlobName);
        }
        catch (Exception ex)
        {
            return ServiceResult<string>.Failure($"Failed to copy document: {ex.Message}");
        }
    }

    private static string GetBlobName(int documentId, int versionNumber)
    {
        return $"{documentId}/{versionNumber}";
    }

    public void Dispose()
    {
        _initializationLock.Dispose();
    }
}
