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

    public DocumentStorageService(
        IOptions<DocumentStorageOptions> options,
        ILogger<DocumentStorageService> logger)
    {
        _options = options.Value;
        _logger = logger;

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

    public async Task<ServiceResult<string>> UploadDocumentAsync(IFormFile file, string fileName)
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

            var blobClient = _containerClient.GetBlobClient(fileName);

            await using var stream = file.OpenReadStream();
            await blobClient.UploadAsync(stream, new BlobHttpHeaders { ContentType = file.ContentType });

            return ServiceResult<string>.Success(blobClient.Uri.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading document {FileName}", fileName);
            return ServiceResult<string>.Failure($"Error uploading document: {ex.Message}");
        }
    }

    /// <summary>
    /// Downloads a document from storage.
    /// </summary>
    /// <param name="fileName">The name of the file to download.</param>
    /// <returns>A ServiceResult containing the document stream. Note: The caller is responsible for disposing the returned Stream.</returns>
    public async Task<ServiceResult<Stream>> DownloadDocumentAsync(string fileName)
    {
        try
        {
            await EnsureInitializedAsync();
            
            var blobClient = _containerClient.GetBlobClient(fileName);
            
            if (!await blobClient.ExistsAsync())
            {
                return ServiceResult<Stream>.Failure("Document not found");
            }

            // Create a new memory stream that we'll copy the blob content into
            var memoryStream = new MemoryStream();
            
            // Download and copy the blob content
            var download = await blobClient.DownloadStreamingAsync();
            using (download.Value)
            using (var blobStream = download.Value.Content)
            {
                await blobStream.CopyToAsync(memoryStream);
            }
            
            // Reset the position to the beginning for reading
            memoryStream.Position = 0;
            return ServiceResult<Stream>.Success(memoryStream);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading document {FileName}", fileName);
            return ServiceResult<Stream>.Failure($"Error downloading document: {ex.Message}");
        }
    }

    public async Task<ServiceResult> DeleteDocumentAsync(string fileName)
    {
        try
        {
            await EnsureInitializedAsync();
            
            var blobClient = _containerClient.GetBlobClient(fileName);
            
            if (!await blobClient.ExistsAsync())
            {
                return ServiceResult.Failure("Document not found");
            }

            await blobClient.DeleteAsync();
            return ServiceResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting document {FileName}", fileName);
            return ServiceResult.Failure($"Error deleting document: {ex.Message}");
        }
    }

    public async Task<ServiceResult<bool>> DocumentExistsAsync(string fileName)
    {
        try
        {
            await EnsureInitializedAsync();
            
            var blobClient = _containerClient.GetBlobClient(fileName);
            var exists = await blobClient.ExistsAsync();
            return ServiceResult<bool>.Success(exists.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking document existence {FileName}", fileName);
            return ServiceResult<bool>.Failure($"Error checking document existence: {ex.Message}");
        }
    }

    public async Task<ServiceResult<string>> GetDocumentUrlAsync(string fileName, int expiryMinutes = 60)
    {
        try
        {
            await EnsureInitializedAsync();
            
            var blobClient = _containerClient.GetBlobClient(fileName);
            
            if (!await blobClient.ExistsAsync())
            {
                return ServiceResult<string>.Failure("Document not found");
            }

            var sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = _containerClient.Name,
                BlobName = fileName,
                Resource = "b",
                StartsOn = DateTimeOffset.UtcNow,
                ExpiresOn = DateTimeOffset.UtcNow.AddMinutes(expiryMinutes)
            };

            sasBuilder.SetPermissions(BlobSasPermissions.Read);

            try
            {
                // Get the account name from the container client
                var accountName = _containerClient.AccountName;
                
                // Extract the account key safely using Azure's built-in parser
                var credential = new StorageSharedKeyCredential(accountName, GetAccountKeyFromConnectionString(_options.ConnectionString));
                
                var sasToken = sasBuilder.ToSasQueryParameters(credential).ToString();
                return ServiceResult<string>.Success($"{blobClient.Uri}?{sasToken}");
            }
            catch (FormatException fex)
            {
                _logger.LogError(fex, "Invalid connection string format");
                return ServiceResult<string>.Failure("Invalid storage connection string format");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating SAS URL for document {FileName}", fileName);
            return ServiceResult<string>.Failure($"Error generating document URL: {ex.Message}");
        }
    }

    private static string GetAccountKeyFromConnectionString(string connectionString)
    {
        try
        {
            var parts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            
            foreach (var part in connectionString.Split(';')
                .Select(p => p.Trim())
                .Where(p => !string.IsNullOrEmpty(p)))
            {
                var keyValue = part.Split('=', 2);
                if (keyValue.Length != 2)
                {
                    throw new FormatException($"Invalid connection string part: {part}");
                }

                var key = keyValue[0].Trim();
                var value = keyValue[1].Trim();

                if (parts.ContainsKey(key))
                {
                    throw new ArgumentException($"Duplicate key in connection string: {key}", nameof(connectionString));
                }

                parts.Add(key, value);
            }

            if (!parts.TryGetValue("AccountKey", out var accountKey))
            {
                throw new ArgumentException("AccountKey not found in connection string", nameof(connectionString));
            }

            return accountKey;
        }
        catch (ArgumentException)
        {
            throw; // Re-throw ArgumentException to preserve the original error
        }
        catch (Exception ex)
        {
            throw new FormatException("Invalid connection string format", ex);
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

    public void Dispose()
    {
        _initializationLock.Dispose();
    }
} 