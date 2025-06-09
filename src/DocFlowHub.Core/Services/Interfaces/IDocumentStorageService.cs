using System.Threading.Tasks;
using DocFlowHub.Core.Models.Common;
using Microsoft.AspNetCore.Http;

namespace DocFlowHub.Core.Services.Interfaces;

public interface IDocumentStorageService
{
    Task<ServiceResult<string>> UploadDocumentAsync(IFormFile file, string userId);
    
    Task<ServiceResult<byte[]>> DownloadDocumentAsync(string filePath);
    
    Task<ServiceResult> DeleteDocumentAsync(string filePath);
    
    Task<ServiceResult<string>> GetFileHashAsync(IFormFile file);
    
    Task<bool> FileExistsAsync(string filePath);
    
    Task<ServiceResult<string>> CopyDocumentAsync(string sourceFilePath, string userId);
} 