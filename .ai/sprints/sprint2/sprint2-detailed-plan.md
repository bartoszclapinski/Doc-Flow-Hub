# Sprint 2: Core Document Management Features - Detailed Plan

## Document Management Implementation

### 1. Document Model and Storage ✅
- Created document storage service in Infrastructure project:
  ```
  dotnet add src/DocFlowHub.Infrastructure package Azure.Storage.Blobs
  ```
- Implemented document models in Core project:
  ```csharp
  // Document.cs
  public class Document
  {
      public int Id { get; set; }
      public string Title { get; set; }
      public string Description { get; set; }
      public string FilePath { get; set; }
      public string FileType { get; set; }
      public long FileSize { get; set; }
      public string OwnerId { get; set; }
      public virtual ApplicationUser Owner { get; set; }
      public int? TeamId { get; set; }
      public virtual Team Team { get; set; }
      public virtual ICollection<DocumentVersion> Versions { get; set; }
      public DateTime CreatedAt { get; set; }
      public DateTime? UpdatedAt { get; set; }
  }
  ```
- Created document repository interface and implementation
- Added document-related migrations:
  ```
  dotnet ef migrations add AddDocumentManagement --project src/DocFlowHub.Infrastructure --startup-project src/DocFlowHub.Web
  ```
- Configured proper relationships and constraints
- Set up soft delete functionality
- Implemented proper cascade behaviors

### 2. Document CRUD Operations (In Progress)
- Create document service interface in Core project:
  ```csharp
  public interface IDocumentService
  {
      Task<ServiceResult<DocumentDto>> CreateDocumentAsync(CreateDocumentRequest request, string userId);
      Task<ServiceResult<DocumentDto>> UpdateDocumentAsync(UpdateDocumentRequest request, string userId);
      Task<ServiceResult> DeleteDocumentAsync(int id, string userId);
      Task<ServiceResult<DocumentDto>> GetDocumentByIdAsync(int id, string userId);
      Task<ServiceResult<IEnumerable<DocumentDto>>> GetUserDocumentsAsync(string userId, DocumentFilter filter);
      Task<ServiceResult<DocumentDto>> RestoreVersionAsync(int documentId, int versionId, string userId);
      Task<ServiceResult<IEnumerable<DocumentVersionDto>>> GetDocumentVersionsAsync(int documentId, string userId);
  }
  ```
- Implement document service in Infrastructure project
- Create document DTOs for data transfer
- Add document validation using FluentValidation
- Implement file type and size validation (30MB limit)

### 3. Document List and Details Pages (Not Started)
- Create /Pages/Documents/Index.cshtml for document list:
  - Implement grid view with sorting
  - Add basic filtering options
  - Show document metadata
  - Add pagination support
- Create /Pages/Documents/Details.cshtml:
  - Display document metadata
  - Show version history
  - Add download button
  - Implement edit and delete options
- Style pages with Bootstrap 5.3

### 4. Document Upload and Edit
- Create /Pages/Documents/Upload.cshtml:
  - Implement file upload with progress bar
  - Add metadata form
  - Validate file type and size
  - Show upload status
- Create /Pages/Documents/Edit.cshtml:
  - Allow metadata updates
  - Enable file replacement
  - Implement version tracking
  - Add validation messages

## Document Versioning System

### 5. Version Management
- Implement version models:
  ```csharp
  public class DocumentVersion
  {
      public int Id { get; set; }
      public int DocumentId { get; set; }
      public virtual Document Document { get; set; }
      public string FilePath { get; set; }
      public int VersionNumber { get; set; }
      public string ChangeSummary { get; set; }
      public string UserId { get; set; }
      public virtual ApplicationUser User { get; set; }
      public DateTime CreatedAt { get; set; }
  }
  ```
- Create version tracking service
- Implement automatic version creation
- Add version comparison functionality

### 6. Version History UI
- Create version history partial view
- Implement version restore functionality
- Add version comparison view
- Create version download options

## Document Organization

### 7. Category Management
- Create category models and DTOs
- Implement category service
- Create category management UI
- Add category selection to document forms

### 8. Project/Folder Structure
- Implement folder models and logic
- Create folder management UI
- Add document-folder relationships
- Implement folder navigation

## Testing and Documentation

### 9. Unit Tests
- Create test project:
  ```
  dotnet new xunit -n DocFlowHub.Tests -o tests/DocFlowHub.Tests
  dotnet sln add tests/DocFlowHub.Tests
  ```
- Add test packages:
  ```
  dotnet add tests/DocFlowHub.Tests package Moq
  dotnet add tests/DocFlowHub.Tests package FluentAssertions
  ```
- Write tests for:
  - Document service
  - Version tracking
  - Category management
  - Validation logic

### 10. Integration Tests
- Create integration test project
- Test file upload and storage
- Test version management
- Test category system

## Definition of Done for Sprint 2
- Document CRUD operations fully implemented
- Version tracking system working correctly
- File upload with validation functioning
- Document list and details pages styled and responsive
- Category and folder system operational
- All unit tests passing
- Integration tests completed
- Documentation updated
- Code reviewed and merged
- Performance tested with sample data 