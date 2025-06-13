# Sprint 3 Quick Start Guide

## 🎯 Sprint 3 Goal: Complete Document Details & Download Functionality

### Current Status: Sprint 2 ✅ COMPLETED
- Document upload working end-to-end with Azure Storage
- Document listing with search and filtering
- All backend services implemented and tested
- Database schema complete and working

### Sprint 3 Priority Tasks

## 1. Document Details Page (HIGH PRIORITY - Day 1-2)

### Files to Create:
- `src/DocFlowHub.Web/Pages/Documents/Details.cshtml`
- `src/DocFlowHub.Web/Pages/Documents/Details.cshtml.cs`

### Backend Services Ready ✅
All required services are already implemented:
- `IDocumentService.GetDocumentByIdAsync(int id)` - Get document details
- `IDocumentService.GetDocumentVersionsAsync(int documentId, DocumentFilter filter)` - Get version history
- `IDocumentCategoryService.GetDocumentCategoriesAsync(int documentId)` - Get document categories
- `IDocumentService.DownloadDocumentVersionAsync(int documentId, int versionId)` - Download specific version

### Implementation Steps:
1. Create the page model with proper dependency injection
2. Implement OnGetAsync to load document, versions, and categories
3. Implement OnPostDownloadAsync for file downloads
4. Create responsive UI with Bootstrap showing:
   - Document metadata (title, description, owner, dates)
   - File information (type, size)
   - Categories as badges
   - Version history list
   - Download and edit action buttons

## 2. Document Download from Index Page (HIGH PRIORITY - Day 2-3)

### Files to Modify:
- `src/DocFlowHub.Web/Pages/Documents/Index.cshtml`
- `src/DocFlowHub.Web/Pages/Documents/Index.cshtml.cs`

### Implementation Steps:
1. Add download button to each document card
2. Add OnPostDownloadAsync handler to IndexModel
3. Use existing `IDocumentService.DownloadDocumentVersionAsync` method
4. Implement proper content-type detection and file naming

## 3. Document Edit Page (MEDIUM PRIORITY - Day 3-5)

### Files to Create:
- `src/DocFlowHub.Web/Pages/Documents/Edit.cshtml`
- `src/DocFlowHub.Web/Pages/Documents/Edit.cshtml.cs`

### Backend Services Ready ✅
- `IDocumentService.UpdateDocumentAsync(int id, UpdateDocumentRequest request)` - Update metadata
- `IDocumentService.UpdateDocumentContentAsync(int id, IFormFile file, string? changeSummary)` - Upload new version
- `IDocumentCategoryService.AssignCategoriesToDocumentAsync(int documentId, IEnumerable<int> categoryIds)` - Update categories

### Implementation Steps:
1. Create edit form for document metadata
2. Add category selection (similar to upload page)
3. Add file replacement functionality for new versions
4. Implement form validation and error handling

## 🛠️ Technical Implementation Notes

### Page Model Pattern
```csharp
[Authorize]
public class DetailsModel : PageModel
{
    private readonly IDocumentService _documentService;
    // ... other dependencies
    
    public DocumentDto Document { get; set; } = null!;
    public PagedResult<DocumentVersionDto> Versions { get; set; } = new();
    // ... other properties
    
    [BindProperty(SupportsGet = true)]
    public int Id { get; set; }
    
    public async Task<IActionResult> OnGetAsync() { /* implementation */ }
    public async Task<IActionResult> OnPostDownloadAsync(int versionId) { /* implementation */ }
}
```

### File Download Implementation
```csharp
public async Task<IActionResult> OnPostDownloadAsync(int versionId)
{
    var downloadResult = await _documentService.DownloadDocumentVersionAsync(Id, versionId);
    if (!downloadResult.Succeeded)
    {
        ErrorMessage = downloadResult.Error;
        return Page();
    }

    var contentType = GetContentType(version.Data.FileType);
    var fileName = $"{Document.Title}_v{version.Data.VersionNumber}{version.Data.FileType}";
    
    return File(downloadResult.Data, contentType, fileName);
}
```

### UI Components to Include
- Bootstrap card layouts for consistency
- Responsive design for mobile/desktop
- Loading states for download operations
- Error message display
- Navigation breadcrumbs
- Action buttons with proper icons

## 🔗 Navigation Flow
1. **Index Page** → **Details Page** (via "View Details" link)
2. **Details Page** → **Edit Page** (via "Edit" button)
3. **Details Page** → **Download** (via "Download" button)
4. **Index Page** → **Download** (via quick download button)

## ⚡ Quick Implementation Tips

### Content Type Detection
```csharp
private static string GetContentType(string fileExtension)
{
    return fileExtension.ToLower() switch
    {
        ".pdf" => "application/pdf",
        ".doc" => "application/msword",
        ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        ".md" => "text/markdown",
        ".txt" => "text/plain",
        ".jpg" or ".jpeg" => "image/jpeg",
        ".png" => "image/png",
        ".gif" => "image/gif",
        _ => "application/octet-stream"
    };
}
```

### File Size Formatting
```csharp
@functions {
    private static string FormatFileSize(long bytes)
    {
        string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
        int counter = 0;
        decimal number = bytes;
        while (Math.Round(number / 1024) >= 1)
        {
            number /= 1024;
            counter++;
        }
        return $"{number:n1} {suffixes[counter]}";
    }
}
```

## 📋 Testing Checklist

### For Each New Page:
- [ ] Page loads without errors
- [ ] All required data is displayed
- [ ] Form validation works
- [ ] Download functionality works
- [ ] Error handling displays properly
- [ ] Responsive design on mobile/desktop
- [ ] Authentication requirements enforced

### End-to-End Testing:
- [ ] Upload document → View in index → View details → Download
- [ ] Upload document → Edit metadata → Verify changes
- [ ] Upload document → Upload new version → Download both versions

## 🚀 Success Criteria for Sprint 3

By the end of Sprint 3, users should be able to:
1. ✅ Upload documents (already working)
2. ✅ View document list (already working)
3. ⏳ View document details with metadata and version history
4. ⏳ Download documents (current version and previous versions)
5. ⏳ Edit document metadata and upload new versions

This completes the core document management functionality and provides a solid foundation for advanced features in future sprints. 