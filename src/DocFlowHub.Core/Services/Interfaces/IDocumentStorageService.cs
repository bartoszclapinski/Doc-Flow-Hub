using System.Threading.Tasks;
using DocFlowHub.Core.Models.Common;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Diagnostics;
using DocFlowHub.Core.Models;

namespace DocFlowHub.Core.Services.Interfaces;

public interface IDocumentStorageService : IDisposable
{
    /// <summary>
    /// Uploads a document file to storage
    /// </summary>
    /// <param name="file">The file to upload</param>
    /// <param name="documentId">The ID of the document</param>
    /// <param name="versionNumber">The version number of the document</param>
    /// <returns>URL to the uploaded file</returns>
    Task<ServiceResult<string>> UploadDocumentAsync(IFormFile file, int documentId, int versionNumber);

    /// <summary>
    /// Downloads a document file from storage
    /// </summary>
    /// <param name="documentId">The ID of the document</param>
    /// <param name="versionNumber">The version number of the document</param>
    /// <returns>File stream if found</returns>
    Task<ServiceResult<byte[]>> DownloadDocumentAsync(int documentId, int versionNumber);

    /// <summary>
    /// Deletes a document file from storage
    /// </summary>
    /// <param name="documentId">The ID of the document</param>
    /// <param name="versionNumber">The version number of the document</param>
    Task<ServiceResult> DeleteDocumentAsync(int documentId, int versionNumber);

    /// <summary>
    /// Checks if a document exists in storage
    /// </summary>
    /// <param name="documentId">The ID of the document</param>
    /// <param name="versionNumber">The version number of the document</param>
    /// <returns>True if file exists</returns>
    Task<ServiceResult<bool>> DocumentExistsAsync(int documentId, int versionNumber);

    /// <summary>
    /// Gets the URL for a document that can be used to download it
    /// </summary>
    /// <param name="documentId">The ID of the document</param>
    /// <param name="versionNumber">The version number of the document</param>
    /// <param name="expiryMinutes">Number of minutes the URL will be valid</param>
    /// <returns>Temporary URL to download the file</returns>
    Task<ServiceResult<string>> GetDocumentUrlAsync(int documentId, int versionNumber, int expiryMinutes = 60);

    Task<ServiceResult<string>> GetFileHashAsync(IFormFile file);

    Task<ServiceResult<string>> CopyDocumentAsync(string sourceFilePath, string userId);
} 