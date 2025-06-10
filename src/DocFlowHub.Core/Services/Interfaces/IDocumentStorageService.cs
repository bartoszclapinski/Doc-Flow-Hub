using System.Threading.Tasks;
using DocFlowHub.Core.Models.Common;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Diagnostics;

namespace DocFlowHub.Core.Services.Interfaces;

public interface IDocumentStorageService : IDisposable
{
    /// <summary>
    /// Uploads a document file to storage
    /// </summary>
    /// <param name="file">The file to upload</param>
    /// <param name="fileName">Unique file name in storage</param>
    /// <returns>URL to the uploaded file</returns>
    Task<ServiceResult<string>> UploadDocumentAsync(IFormFile file, string fileName);

    /// <summary>
    /// Downloads a document file from storage
    /// </summary>
    /// <param name="fileName">File name in storage</param>
    /// <returns>File stream if found</returns>
    Task<ServiceResult<Stream>> DownloadDocumentAsync(string fileName);

    /// <summary>
    /// Deletes a document file from storage
    /// </summary>
    /// <param name="fileName">File name in storage</param>
    Task<ServiceResult> DeleteDocumentAsync(string fileName);

    /// <summary>
    /// Checks if a document exists in storage
    /// </summary>
    /// <param name="fileName">File name to check</param>
    /// <returns>True if file exists</returns>
    Task<ServiceResult<bool>> DocumentExistsAsync(string fileName);

    /// <summary>
    /// Gets the URL for a document that can be used to download it
    /// </summary>
    /// <param name="fileName">File name in storage</param>
    /// <param name="expiryMinutes">Number of minutes the URL will be valid</param>
    /// <returns>Temporary URL to download the file</returns>
    Task<ServiceResult<string>> GetDocumentUrlAsync(string fileName, int expiryMinutes = 60);

    Task<ServiceResult<string>> GetFileHashAsync(IFormFile file);

    Task<ServiceResult<string>> CopyDocumentAsync(string sourceFilePath, string userId);
} 