using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using DocFlowHub.Core.Models.Common;
using DocFlowHub.Core.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace DocFlowHub.Infrastructure.Services.Storage;

public class DocumentStorageService : IDocumentStorageService
{
    private readonly BlobContainerClient _containerClient;
    private readonly DocumentStorageOptions _options;

    public DocumentStorageService(IOptions<DocumentStorageOptions> options)
    {
        _options = options.Value;
        _containerClient = new BlobContainerClient(_options.ConnectionString, _options.ContainerName);
        _containerClient.CreateIfNotExists();
    }

    public async Task<ServiceResult<string>> UploadDocumentAsync(IFormFile file, string userId)
    {
        try
        {
            if (file == null || file.Length == 0)
                return ServiceResult<string>.Failure("No file was provided.");

            if (file.Length > _options.MaxFileSizeBytes)
                return ServiceResult<string>.Failure($"File size exceeds maximum limit of {_options.MaxFileSizeBytes / 1024 / 1024}MB.");

            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!Array.Exists(_options.AllowedFileTypes, x => x == fileExtension))
                return ServiceResult<string>.Failure("File type is not allowed.");

            // Create a unique blob name using userId and timestamp
            var blobName = $"{userId}/{DateTime.UtcNow:yyyy/MM/dd}/{Guid.NewGuid()}{fileExtension}";
            var blobClient = _containerClient.GetBlobClient(blobName);

            await using var stream = file.OpenReadStream();
            await blobClient.UploadAsync(stream, true);

            return ServiceResult<string>.Success(blobName);
        }
        catch (Exception ex)
        {
            return ServiceResult<string>.Failure($"Failed to upload document: {ex.Message}");
        }
    }

    public async Task<ServiceResult<byte[]>> DownloadDocumentAsync(string filePath)
    {
        try
        {
            var blobClient = _containerClient.GetBlobClient(filePath);
            
            if (!await blobClient.ExistsAsync())
                return ServiceResult<byte[]>.Failure("Document not found.");

            using var memoryStream = new MemoryStream();
            await blobClient.DownloadToAsync(memoryStream);
            return ServiceResult<byte[]>.Success(memoryStream.ToArray());
        }
        catch (Exception ex)
        {
            return ServiceResult<byte[]>.Failure($"Failed to download document: {ex.Message}");
        }
    }

    public async Task<ServiceResult> DeleteDocumentAsync(string filePath)
    {
        try
        {
            var blobClient = _containerClient.GetBlobClient(filePath);
            
            if (!await blobClient.ExistsAsync())
                return ServiceResult.Failure("Document not found.");

            await blobClient.DeleteAsync();
            return ServiceResult.Success();
        }
        catch (Exception ex)
        {
            return ServiceResult.Failure($"Failed to delete document: {ex.Message}");
        }
    }

    public async Task<ServiceResult<string>> GetFileHashAsync(IFormFile file)
    {
        try
        {
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

    public async Task<bool> FileExistsAsync(string filePath)
    {
        try
        {
            var blobClient = _containerClient.GetBlobClient(filePath);
            return await blobClient.ExistsAsync();
        }
        catch
        {
            return false;
        }
    }

    public async Task<ServiceResult<string>> CopyDocumentAsync(string sourceFilePath, string userId)
    {
        try
        {
            var sourceBlobClient = _containerClient.GetBlobClient(sourceFilePath);
            
            if (!await sourceBlobClient.ExistsAsync())
                return ServiceResult<string>.Failure("Source document not found.");

            var fileExtension = Path.GetExtension(sourceFilePath);
            var newBlobName = $"{userId}/{DateTime.UtcNow:yyyy/MM/dd}/{Guid.NewGuid()}{fileExtension}";
            var destinationBlobClient = _containerClient.GetBlobClient(newBlobName);

            await destinationBlobClient.StartCopyFromUriAsync(sourceBlobClient.Uri);
            return ServiceResult<string>.Success(newBlobName);
        }
        catch (Exception ex)
        {
            return ServiceResult<string>.Failure($"Failed to copy document: {ex.Message}");
        }
    }
} 