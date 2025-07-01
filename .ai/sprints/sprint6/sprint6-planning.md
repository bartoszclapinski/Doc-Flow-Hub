# Sprint 6: Project/Folder Organization System - Planning Document

## 📊 SPRINT 6 OVERVIEW
**Sprint**: Sprint 6 (Project/Folder Organization System)  
**Duration**: 2-3 weeks  
**Start Date**: January 1, 2025  
**Primary Objective**: Transform document management from flat structure to organized, hierarchical system  
**Approach**: Incremental implementation following established patterns from Sprint 5

## 🎯 SPRINT 6 GOALS

### **Primary Objective**
Transform DocFlowHub from a flat document structure to a professional hierarchical organization system with projects and folders, enabling enterprise-grade document management.

### **Success Criteria**
- ✅ Users can create projects as top-level containers
- ✅ Users can create nested folder structures within projects
- ✅ Documents can be organized into projects and folders
- ✅ Professional folder tree navigation with breadcrumbs
- ✅ Drag-and-drop document organization interface
- ✅ Team-based project collaboration
- ✅ Bulk operations for efficient document management
- ✅ All existing AI and collaboration features continue to work
- ✅ All tests continue to pass (maintain 21/21 test suite)

## 🚨 CRITICAL MISSING FEATURE IDENTIFIED

### **0. Document Deletion System** ⚠️ **MUST IMPLEMENT FIRST**
**Problem**: Backend delete functionality exists but NO frontend interface for deletion!
- **Individual Document Deletion**: Add delete buttons to dropdown menus and details pages
- **Bulk Document Deletion**: Multi-select interface for efficient deletion of multiple documents
- **Confirmation Dialogs**: Professional confirmation modals with document metadata
- **Soft Delete Management**: Show/hide deleted documents, restore functionality for users
- **Admin Hard Delete**: Permanent deletion interface for administrators

## 🚀 SPRINT 6 FEATURES

### **1. Project Management System**
- **Project Entity**: Top-level containers for organizing documents
- **Project CRUD**: Create, read, update, delete projects
- **Project Sharing**: Team-based project collaboration
- **Project Statistics**: Document counts, team members, activity

### **2. Hierarchical Folder System**
- **Folder Entity**: Nested folder structure within projects
- **Parent-Child Relationships**: Support unlimited nesting levels
- **Folder Path Management**: Automatic path generation and tracking
- **Folder Operations**: Create, rename, move, delete with validation

### **3. Document Organization**
- **Project Assignment**: Documents belong to projects
- **Folder Assignment**: Documents can be placed in specific folders
- **Document Moving**: Transfer documents between projects/folders
- **Bulk Operations**: Move multiple documents simultaneously

### **4. Enhanced Navigation**
- **Folder Tree View**: Professional hierarchical navigation
- **Breadcrumb Navigation**: Clear path display and navigation
- **Quick Access**: Recent projects and folders
- **Search Within**: Project/folder scoped search

### **5. Team Integration**
- **Project-Level Sharing**: Share entire projects with teams
- **Folder Permissions**: Granular access control
- **Team Project Templates**: Standardized project structures
- **Collaborative Organization**: Team-based folder management

## 🛠️ IMPLEMENTATION PHASES

### **Phase 6.0.5: Document Deletion** 🚨 **IMMEDIATE PRIORITY** (2-3 days)
**Why First**: Critical usability gap - users can upload but can't delete documents!

#### **6.0.5.1: Individual Document Deletion**
- Add delete button to document dropdown menu in `Documents/Index.cshtml`
- Add delete button to `Documents/Details.cshtml` for document owners
- Implement confirmation modal with document metadata (title, size, date)
- Add delete handler to page models with proper authorization checks
- Success/error feedback with toast notifications

#### **6.0.5.2: Bulk Document Deletion**
- Add multi-select checkboxes to document listing
- Implement bulk delete interface with confirmation
- Progress indicator for bulk operations
- Handle partial failures with detailed feedback

#### **6.0.5.3: Soft Delete Management**
- "Show Deleted Documents" toggle for users and admins
- Restore deleted documents functionality
- Proper authorization (only owners can restore their documents)
- Admin interface for viewing all deleted documents

### **Week 1: Foundation (Phase 6.1)**

#### **Phase 6.1.1: Database Models & Schema**
- Create `Project.cs` entity with properties:
  - ProjectId, Name, Description, CreatedAt, UpdatedAt
  - CreatedByUserId (foreign key to ApplicationUser)
  - IsActive, Color/Icon for UI customization
- Create `Folder.cs` entity with properties:
  - FolderId, Name, ProjectId (foreign key)
  - ParentFolderId (self-referencing for hierarchy)
  - Path (computed), Level (depth indicator)
  - CreatedAt, UpdatedAt, CreatedByUserId
- Update `Document.cs` to include:
  - ProjectId (foreign key, nullable for backward compatibility)
  - FolderId (foreign key, nullable)
- Create Entity Framework configurations
- Design and apply database migration

#### **Phase 6.1.2: Service Layer Foundation**
- Create `IProjectService.cs` interface with methods:
  - GetProjectsForUserAsync, GetProjectByIdAsync
  - CreateProjectAsync, UpdateProjectAsync, DeleteProjectAsync
  - GetProjectStatisticsAsync, ShareProjectWithTeamAsync
- Create `IFolderService.cs` interface with methods:
  - GetFoldersInProjectAsync, GetFolderHierarchyAsync
  - CreateFolderAsync, UpdateFolderAsync, DeleteFolderAsync
  - MoveFolderAsync, GetFolderPathAsync
- Implement `ProjectService.cs` with full business logic
- Implement `FolderService.cs` with hierarchical operations
- Update `DocumentService.cs` for project/folder support

### **Week 2: User Interface (Phase 6.2)**

#### **Phase 6.2.1: Project Management Pages**
- `/Pages/Projects/Index.cshtml` - Project listing with statistics
- `/Pages/Projects/Create.cshtml` - Project creation form
- `/Pages/Projects/Details.cshtml` - Project dashboard with folder tree
- `/Pages/Projects/Edit.cshtml` - Project settings and sharing
- Implement project card layout with statistics and quick actions
- Add team sharing interface and member management

#### **Phase 6.2.2: Folder Navigation Interface**
- Enhanced `/Pages/Documents/Index.cshtml` with:
  - Folder tree sidebar navigation
  - Breadcrumb navigation at top
  - Project/folder selection dropdown
  - Context menus for folder operations
- Implement folder tree component with:
  - Expandable/collapsible nodes
  - Drag-and-drop support preparation
  - Right-click context menus
  - Keyboard navigation support

#### **Phase 6.2.3: Document Organization**
- Update `/Pages/Documents/Upload.cshtml` with:
  - Project selection dropdown
  - Folder picker with tree view
  - Option to create new folders during upload
- Enhanced document listing with:
  - Project/folder filtering
  - Hierarchical view toggle
  - Bulk selection checkboxes

### **Week 2-3: Advanced Features (Phase 6.3)**

#### **Phase 6.3.1: Drag-and-Drop Interface**
- Implement JavaScript drag-and-drop functionality:
  - Drag documents to folders
  - Drag folders to reorganize hierarchy
  - Visual feedback during drag operations
  - Validation and error handling
- Add touch support for mobile devices
- Implement undo/redo for organization changes

#### **Phase 6.3.2: Bulk Operations**
- Multi-select interface for documents and folders
- Bulk operations modal with options:
  - Move to project/folder
  - Share with teams
  - Apply categories
  - Delete multiple items
- Progress indicators for bulk operations
- Comprehensive validation and rollback

#### **Phase 6.3.3: Enhanced Search & Filtering**
- Project-scoped search functionality
- Folder-scoped search within projects
- Advanced filters combining:
  - Projects + Folders + Teams + Categories + AI status
- Search result organization by project/folder
- Quick filter chips for common searches

## 📋 TECHNICAL CONSIDERATIONS

### **Database Design**
```sql
-- Project Table
Projects: ProjectId, Name, Description, CreatedByUserId, CreatedAt, UpdatedAt, IsActive

-- Folder Table (Self-referencing hierarchy)
Folders: FolderId, ProjectId, ParentFolderId, Name, Path, Level, CreatedByUserId, CreatedAt, UpdatedAt

-- Updated Document Table
Documents: DocumentId, ProjectId (nullable), FolderId (nullable), [existing fields...]

-- Project Team Sharing
ProjectTeamShares: ProjectId, TeamId, SharedAt, SharedByUserId
```

### **Performance Considerations**
- Indexed foreign keys for project/folder relationships
- Computed columns for folder paths
- Efficient hierarchical queries using CTEs
- Pagination for large folder structures
- Caching for frequently accessed folder trees

### **Backward Compatibility**
- Documents without project/folder assignment remain accessible
- Gradual migration path for existing documents
- Default "Unorganized" project for backward compatibility
- Maintain all existing APIs and functionality

## 🧪 TESTING STRATEGY

### **Unit Tests**
- ProjectService tests for CRUD operations
- FolderService tests for hierarchical operations
- DocumentService tests for organization features
- Validation tests for business rules

### **Integration Tests**
- Database relationship tests
- File system integration tests
- Team sharing integration tests
- Search functionality tests

### **UI Tests**
- Navigation component tests
- Drag-and-drop functionality tests
- Bulk operations tests
- Responsive design tests

## 📊 SUCCESS METRICS

### **Functional Goals**
- Users can create unlimited projects and folders
- Document organization reduces search time by 50%
- Team collaboration improves with project-based sharing
- Zero data loss during organization operations

### **Technical Goals**
- All existing features continue to work perfectly
- Test suite remains at 100% (21/21 tests + new tests)
- Performance maintained for large document collections
- Clean, maintainable code following established patterns

### **User Experience Goals**
- Intuitive folder navigation matching file explorer patterns
- Professional drag-and-drop interface
- Responsive design on all devices
- Accessible interface following WCAG guidelines

## 🔄 MIGRATION STRATEGY

### **Phase 1: Foundation Setup**
1. Deploy database changes with backward compatibility
2. Update service layer with new interfaces
3. Test thoroughly with existing data

### **Phase 2: UI Rollout**
1. Deploy project management pages
2. Enhance document pages with organization features
3. Enable folder navigation and bulk operations

### **Phase 3: User Migration**
1. Provide migration tools for existing documents
2. User education and onboarding materials
3. Gradual feature adoption with fallbacks

## 🎯 READY FOR IMPLEMENTATION

**Prerequisites Met:**
- ✅ Sprint 5 AI platform complete and stable
- ✅ Solid service layer architecture established
- ✅ Professional UI patterns proven
- ✅ Team collaboration system working
- ✅ Database migration experience
- ✅ Testing infrastructure ready

**Sprint 6 Status**: ✅ **READY TO BEGIN** 🚀

This Sprint 6 will complete the transformation of DocFlowHub into a professional enterprise-grade document management platform with hierarchical organization, maintaining all AI features while adding comprehensive document organization capabilities. 