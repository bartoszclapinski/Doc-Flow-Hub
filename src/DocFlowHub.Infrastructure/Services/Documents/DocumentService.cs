using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DocFlowHub.Core.Models;
using DocFlowHub.Core.Models.Common;
using DocFlowHub.Core.Models.Documents;
using DocFlowHub.Core.Models.Documents.Dto;
using DocFlowHub.Core.Models.Documents.Extensions;
using DocFlowHub.Core.Services.Interfaces;
using DocFlowHub.Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DocFlowHub.Infrastructure.Services.Documents;

public class DocumentService : IDocumentService
{
    private readonly ApplicationDbContext _context;
    private readonly IDocumentStorageService _storageService;
    private readonly ILogger<DocumentService> _logger;

    public DocumentService(
        ApplicationDbContext context,
        IDocumentStorageService storageService,
        ILogger<DocumentService> logger)
    {
        _context = context;
        _storageService = storageService;
        _logger = logger;
    }

    public async Task<ServiceResult<DocumentDto>> GetDocumentByIdAsync(int id)
    {
        var document = await _context.Documents
            .Include(d => d.Versions)
            .Include(d => d.Categories)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (document == null)
            return ServiceResult<DocumentDto>.Failure("Document not found");

        return ServiceResult<DocumentDto>.Success(document.ToDto());
    }

    public async Task<ServiceResult<PagedResult<DocumentDto>>> GetDocumentsAsync(DocumentFilter filter)
    {
        var query = _context.Documents
            .Include(d => d.Versions)
            .Include(d => d.Categories)
            .AsQueryable();

        if (!string.IsNullOrEmpty(filter.SearchTerm))
        {
            query = query.Where(d => 
                d.Title.Contains(filter.SearchTerm) || 
                d.Description.Contains(filter.SearchTerm));
        }

        if (!string.IsNullOrEmpty(filter.FileType))
        {
            query = query.Where(d => d.FileType == filter.FileType);
        }

        if (filter.CategoryId.HasValue)
        {
            query = query.Where(d => 
                d.Categories.Any(c => c.Id == filter.CategoryId));
        }

        if (!string.IsNullOrEmpty(filter.OwnerId))
        {
            query = query.Where(d => d.OwnerId == filter.OwnerId);
        }

        if (filter.TeamId.HasValue)
        {
            query = query.Where(d => d.TeamId == filter.TeamId);
        }

        if (!filter.IncludeDeleted)
        {
            query = query.Where(d => !d.IsDeleted);
        }

        var totalItems = await query.CountAsync();
        var items = await query
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        return ServiceResult<PagedResult<DocumentDto>>.Success(new PagedResult<DocumentDto>
        {
            Items = items.Select(d => d.ToDto()),
            TotalItems = totalItems,
            PageNumber = filter.PageNumber,
            PageSize = filter.PageSize
        });
    }

    public async Task<ServiceResult<IEnumerable<DocumentDto>>> GetUserDocumentsAsync(string userId, DocumentFilter filter)
    {
        filter.OwnerId = userId;
        var result = await GetDocumentsAsync(filter);
        if (!result.Succeeded)
            return ServiceResult<IEnumerable<DocumentDto>>.Failure(result.Error!);

        return ServiceResult<IEnumerable<DocumentDto>>.Success(result.Data.Items);
    }

    public async Task<ServiceResult<DocumentDto>> CreateDocumentAsync(CreateDocumentRequest request, IFormFile file)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var document = new Document
            {
                Title = request.Title,
                Description = request.Description,
                OwnerId = request.OwnerId,
                TeamId = request.TeamId,
                FileType = Path.GetExtension(file.FileName),
                FileSize = file.Length,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsDeleted = false
            };

            _context.Documents.Add(document);
            await _context.SaveChangesAsync();

            var version = new DocumentVersion
            {
                DocumentId = document.Id,
                VersionNumber = 1,
                CreatedAt = DateTime.UtcNow,
                UserId = request.OwnerId,
                FileType = document.FileType,
                FileSize = file.Length
            };

            _context.DocumentVersions.Add(version);
            await _context.SaveChangesAsync();

            var storageResult = await _storageService.UploadDocumentAsync(file, document.Id, version.VersionNumber);
            if (!storageResult.Succeeded)
            {
                await transaction.RollbackAsync();
                return ServiceResult<DocumentDto>.Failure(storageResult.Error!);
            }

            version.FilePath = storageResult.Data!;
            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            return ServiceResult<DocumentDto>.Success(document.ToDto());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating document");
            await transaction.RollbackAsync();
            return ServiceResult<DocumentDto>.Failure($"Error creating document: {ex.Message}");
        }
    }

    public async Task<ServiceResult<DocumentDto>> UpdateDocumentAsync(int id, UpdateDocumentRequest request)
    {
        var document = await _context.Documents
            .Include(d => d.Versions)
            .Include(d => d.Categories)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (document == null)
            return ServiceResult<DocumentDto>.Failure("Document not found");

        document.Title = request.Title;
        document.Description = request.Description;
        document.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return ServiceResult<DocumentDto>.Success(document.ToDto());
    }

    public async Task<ServiceResult<DocumentDto>> UpdateDocumentContentAsync(int id, IFormFile file)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var document = await _context.Documents
                .Include(d => d.Versions)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (document == null)
                return ServiceResult<DocumentDto>.Failure("Document not found");

            var versionNumber = document.Versions.Max(v => v.VersionNumber) + 1;
            var version = new DocumentVersion
            {
                DocumentId = document.Id,
                VersionNumber = versionNumber,
                CreatedAt = DateTime.UtcNow,
                FileType = Path.GetExtension(file.FileName),
                FileSize = file.Length
            };

            _context.DocumentVersions.Add(version);
            await _context.SaveChangesAsync();

            var storageResult = await _storageService.UploadDocumentAsync(file, document.Id, version.VersionNumber);
            if (!storageResult.Succeeded)
            {
                await transaction.RollbackAsync();
                return ServiceResult<DocumentDto>.Failure(storageResult.Error!);
            }

            version.FilePath = storageResult.Data!;
            document.UpdatedAt = DateTime.UtcNow;
            document.FileType = version.FileType;
            document.FileSize = version.FileSize;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return ServiceResult<DocumentDto>.Success(document.ToDto());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating document content");
            await transaction.RollbackAsync();
            return ServiceResult<DocumentDto>.Failure($"Error updating document content: {ex.Message}");
        }
    }

    public async Task<ServiceResult<DocumentVersionDto>> GetDocumentVersionAsync(int documentId, int versionId)
    {
        var version = await _context.DocumentVersions
            .Include(v => v.Document)
            .FirstOrDefaultAsync(v => v.DocumentId == documentId && v.Id == versionId);

        if (version == null)
            return ServiceResult<DocumentVersionDto>.Failure("Version not found");

        return ServiceResult<DocumentVersionDto>.Success(version.ToDto());
    }

    public async Task<ServiceResult<PagedResult<DocumentVersionDto>>> GetDocumentVersionsAsync(int documentId, DocumentFilter filter)
    {
        var query = _context.DocumentVersions
            .Include(v => v.Document)
            .Where(v => v.DocumentId == documentId)
            .OrderByDescending(v => v.VersionNumber)
            .AsQueryable();

        var totalItems = await query.CountAsync();
        var items = await query
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        return ServiceResult<PagedResult<DocumentVersionDto>>.Success(new PagedResult<DocumentVersionDto>
        {
            Items = items.Select(v => v.ToDto()),
            TotalItems = totalItems,
            PageNumber = filter.PageNumber,
            PageSize = filter.PageSize
        });
    }

    public async Task<ServiceResult<byte[]>> DownloadDocumentVersionAsync(int documentId, int versionId)
    {
        var version = await _context.DocumentVersions
            .FirstOrDefaultAsync(v => v.DocumentId == documentId && v.Id == versionId);

        if (version == null)
            return ServiceResult<byte[]>.Failure("Version not found");

        return await _storageService.DownloadDocumentAsync(documentId, version.VersionNumber);
    }

    public async Task<ServiceResult> DeleteDocumentAsync(int id)
    {
        var document = await _context.Documents.FindAsync(id);
        if (document == null)
            return ServiceResult.Failure("Document not found");

        document.IsDeleted = true;
        document.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return ServiceResult.Success();
    }

    public async Task<ServiceResult> RestoreDocumentAsync(int id)
    {
        var document = await _context.Documents.FindAsync(id);
        if (document == null)
            return ServiceResult.Failure("Document not found");

        document.IsDeleted = false;
        document.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return ServiceResult.Success();
    }

    public async Task<ServiceResult<DocumentDto>> ShareDocumentWithTeamAsync(int documentId, int teamId)
    {
        var document = await _context.Documents
            .Include(d => d.Versions)
            .Include(d => d.Categories)
            .FirstOrDefaultAsync(d => d.Id == documentId);

        if (document == null)
            return ServiceResult<DocumentDto>.Failure("Document not found");

        document.TeamId = teamId;
        document.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return ServiceResult<DocumentDto>.Success(document.ToDto());
    }

    public async Task<ServiceResult> UnshareDocumentFromTeamAsync(int documentId)
    {
        var document = await _context.Documents.FindAsync(documentId);
        if (document == null)
            return ServiceResult.Failure("Document not found");

        document.TeamId = null;
        document.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return ServiceResult.Success();
    }

    public async Task<ServiceResult<DocumentDto>> UploadNewVersionAsync(UploadVersionRequest request)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var document = await _context.Documents
                .Include(d => d.Versions)
                .FirstOrDefaultAsync(d => d.Id == request.DocumentId);

            if (document == null)
                return ServiceResult<DocumentDto>.Failure("Document not found");

            var versionNumber = document.Versions.Max(v => v.VersionNumber) + 1;
            var version = new DocumentVersion
            {
                DocumentId = document.Id,
                VersionNumber = versionNumber,
                CreatedAt = DateTime.UtcNow,
                UserId = request.UserId,
                ChangeSummary = request.ChangeSummary,
                FileType = Path.GetExtension(request.File.FileName),
                FileSize = request.File.Length
            };

            _context.DocumentVersions.Add(version);
            await _context.SaveChangesAsync();

            var storageResult = await _storageService.UploadDocumentAsync(request.File, document.Id, version.VersionNumber);
            if (!storageResult.Succeeded)
            {
                await transaction.RollbackAsync();
                return ServiceResult<DocumentDto>.Failure(storageResult.Error!);
            }

            version.FilePath = storageResult.Data!;
            document.UpdatedAt = DateTime.UtcNow;
            document.FileType = version.FileType;
            document.FileSize = version.FileSize;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return ServiceResult<DocumentDto>.Success(document.ToDto());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading new version");
            await transaction.RollbackAsync();
            return ServiceResult<DocumentDto>.Failure($"Error uploading new version: {ex.Message}");
        }
    }
} 