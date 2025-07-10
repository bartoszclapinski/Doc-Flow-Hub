# 📄 Doc-Flow-Hub

Document Management System built with ASP.NET Core 9.

![Build Status](https://github.com/bartoszclapinski/Doc-Flow-Hub/actions/workflows/ci.yml/badge.svg)

## 🚀 Description

Doc-Flow-Hub is a modern **AI-powered enterprise document management system** that helps teams organize, track, and collaborate on documents efficiently. The system provides powerful hierarchical organization with projects and folders, advanced version control, team collaboration features, AI-powered document analysis, and a professional Azure Portal-style user interface.

## 🎯 Current Status

**Sprint 6: 100% COMPLETE** 🎉 - **Complete Enterprise Document Management Platform Achieved**

### ✅ Production-Ready Enterprise Platform
- **Complete Organizational System**: Hierarchical project and folder structure with unlimited nesting capability
- **Advanced Features**: Professional drag-and-drop interface, bulk operations, enhanced search and filtering
- **Enhanced Security**: All identified vulnerabilities resolved with improved authentication patterns
- **AI Integration**: Document summarization and version comparison working end-to-end with organizational context
- **Quality Assurance**: 23/23 tests passing, comprehensive bug fixes, enhanced stability
- **Enterprise Grade**: Professional Azure Portal-style interface with comprehensive navigation

### ✅ Fully Working Features
- **Complete Document Lifecycle**: Upload → Browse (with sorting & filtering) → View → Edit → Download → Delete (Individual/Bulk/Version)
- **Hierarchical Organization**: Projects and folders with unlimited nesting, tree navigation, breadcrumbs
- **Advanced Operations**: Drag-and-drop document organization, bulk move operations, context menus
- **AI Document Analysis**: Automatic summarization and intelligent version comparison integrated with organizational structure
- **Team Collaboration**: Create teams, share documents and projects, team-based filtering, hierarchical access control  
- **Professional UI**: Azure Portal-style interface with responsive design, enhanced animations, and tree navigation
- **Complete Admin System**: System statistics, user management, AI analytics, and security monitoring
- **Security**: Enhanced authentication patterns, comprehensive authorization, vulnerability fixes
- **Version Management**: Complete version history with AI-powered difference analysis and version deletion

## ✨ Features

### 🏗️ Enterprise Document Organization
- **Project Management**
  - Top-level project containers with team sharing and customization
  - Project creation with color/icon selection and live preview
  - Complete CRUD operations (Create, Read, Update, Delete, Archive, Restore)
  - Project statistics and team collaboration features
  - Professional card layout with filtering, sorting, and pagination
- **Hierarchical Folder Structure**
  - Unlimited nesting capability with parent-child relationships
  - Professional tree navigation with expand/collapse functionality
  - Breadcrumb navigation showing complete hierarchy path
  - Folder CRUD operations with circular reference prevention
  - Context menus for efficient folder management operations
- **Document Organization**
  - Project and folder assignment with flexible reassignment
  - Enhanced upload experience with project/folder selection
  - Organizational context throughout document lifecycle
  - Document move operations between projects and folders with validation

### 🎮 Advanced User Operations
- **Drag-and-Drop Interface**
  - Professional drag-and-drop for documents and folders with visual feedback
  - Touch support for mobile devices and tablets
  - Real-time validation during drag operations
  - Undo/redo functionality for organization changes
- **Bulk Operations**
  - Multi-select interface for projects, folders, and documents
  - Bulk move operations with destination picker and validation
  - Bulk sharing operations with team management
  - Progress indicators with comprehensive error handling and detailed feedback
- **Enhanced Search & Filtering**
  - Project-scoped and folder-scoped search with hierarchical results
  - Advanced filter combinations (projects + folders + teams + categories + AI status)
  - Quick filter chips for common searches and organizational patterns
  - Organization-aware search results with contextual display

### 👤 User Management
- **Authentication and Authorization**
  - Secure login and registration with enhanced security patterns
  - Email confirmation and password recovery
  - Enhanced authentication with vulnerability fixes
- **Profile Management** 
  - Customizable user profiles with profile picture upload and management
  - Bio and personal information management
  - Password management with secure policies
- **Role-based Access Control** 
  - Admin and user roles with complete dashboard
  - Permission-based actions and system management
  - User statistics and administrative tools with security monitoring

### 👥 Team Collaboration 
- **Team Management** 
  - Create and manage teams with member roles (Owner, Member)
  - Team activity tracking and invitation system
  - Project-level team sharing with hierarchical access control
- **Document & Project Sharing** 
  - Share documents with specific teams with granular permissions
  - Project-level team collaboration with comprehensive access control
  - Team-based document and project filtering
  - Secure access control with enhanced authorization patterns
  - Real-time sharing/unsharing with feedback messages

### 📝 Document Management 
- **Document Organization** 
  - Hierarchical organization within projects and folders
  - Categorization and tagging system (many-to-many relationships)
  - Professional Azure Portal-style table layout with organizational context
  - Custom metadata (title, description, change summaries)
  - Hierarchical storage in Azure Blob Storage with secure access
- **Complete Document Lifecycle**
  - **Upload**: Professional upload with AI integration, team sharing, and project/folder assignment
  - **Browse**: Enhanced listing with organizational context and tree navigation
  - **View**: Detailed document views with version history and organizational context
  - **Edit**: Document editing with version management and organizational updates
  - **Delete**: Complete deletion suite (Individual, Bulk, Version) with enterprise UX and professional confirmations
- **Version Control** 
  - Automatic version tracking with sequential numbering
  - Complete version history with metadata and organizational context
  - Version restoration and downloads with proper authorization
  - Version deletion with safety checks and owner-only authorization
  - Secure file storage with proper resource management
- **Search and Filtering** 
  - Advanced search by title, metadata, and organizational structure
  - Filter by team membership, categories, projects, folders, and file types
  - Column sorting (Title, Modified Date, File Size) with visual indicators
  - Pagination with UX-optimized page reset on sorting
  - File type validation (PDF, DOC, DOCX, TXT, MD, images) 
  - Size limit enforcement (30MB) 

### 🔒 Enhanced Security 
- **Authentication & Authorization** 
  - Enhanced secure password policies with ASP.NET Core Identity
  - Improved authentication patterns with vulnerability fixes
  - Account lockout protection and email confirmation
  - Session management with enhanced security
  - Role-based access control (Admin, User roles) with comprehensive authorization
- **Document Security** 
  - Enhanced authorization preventing unauthorized document access
  - Secure document filtering with ownership and team membership validation
  - Project and folder level access control with hierarchical permissions
  - HTTPS enforcement throughout with secure authentication patterns
  - Secure file access with proper resource management and enhanced validation
  - File type and size validation with comprehensive security checks
- **Data Protection** 
  - SQL injection prevention with parameterized queries
  - XSS prevention with proper encoding
  - Enhanced resource cleanup and proper disposal
  - Comprehensive error handling without information leakage with security improvements
  - Vulnerability fixes and enhanced authentication patterns

### 🤖 AI-Powered Features
- **Document Summarization**
  - Automatic AI summary generation on upload with organizational context
  - Confidence scores and key points extraction
  - Background processing with real OpenAI API integration
  - Professional UI display with metadata and project/folder context
- **AI Version Comparison**
  - Intelligent analysis of document differences with organizational awareness
  - AI-powered change detection and summarization
  - Model selection with cost estimation
  - Professional comparison results display with enhanced navigation
- **AI Settings & Configuration**
  - Complete user AI preferences management
  - Model selection (GPT-4o, GPT-4o-mini, GPT-4.1, GPT-4.1-mini)
  - Feature toggles and performance settings
  - API key management and validation
  - Smart defaults and cost estimation
- **AI Analytics & Monitoring**
  - Comprehensive usage tracking and analytics
  - Admin dashboard with interactive charts
  - User rate limits and monitoring system
  - Performance metrics and optimization insights
  - Cost analysis and usage trends

## 🏁 Getting Started

### Prerequisites

- **.NET 9 SDK**
- **SQL Server**
- **Azure Storage Account**
- **OpenAI API Account** (for AI features)
- **Visual Studio 2022** or **VS Code**

### Installation

1. Clone the repository
```bash
git clone https://github.com/bartoszclapinski/Doc-Flow-Hub.git
```

2. Navigate to the project directory
```bash
cd Doc-Flow-Hub
```

3. Restore dependencies
```bash
dotnet restore
```

4. Configure the application
   
Create or update `appsettings.Development.json` in the `src/DocFlowHub.Web` directory:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=DocFlowHub;Trusted_Connection=True;MultipleActiveResultSets=true"
  },
  "Storage": {
    "ConnectionString": "DefaultEndpointsProtocol=https;AccountName=your_account_name;AccountKey=your_account_key;EndpointSuffix=core.windows.net"
  },
  "OpenAI": {
    "ApiKey": "your_openai_api_key_here",
    "Model": "gpt-4o-mini",
    "Temperature": 0.7
  }
}
```

To get Azure Storage connection string:
1. Go to Azure Portal
2. Navigate to your Storage Account
3. Go to "Access keys" under "Security + networking"
4. Copy the connection string
5. Replace the `Storage:ConnectionString` value in `appsettings.Development.json`

To get OpenAI API Key:
1. Go to [OpenAI Platform](https://platform.openai.com/api-keys)
2. Create a new API key
3. Replace the `OpenAI:ApiKey` value in `appsettings.Development.json`

⚠️ Never commit actual API keys or connection strings to source control!

5. Update the database
```bash
dotnet ef database update --project src/DocFlowHub.Infrastructure --startup-project src/DocFlowHub.Web
```

6. Run the application
```bash
dotnet run --project src/DocFlowHub.Web
```

## 🏗️ Project Structure

The solution follows Clean Architecture principles with full separation of concerns:

- **DocFlowHub.Core** 📦: Domain models, interfaces, and business logic
  - Document entities (Document, DocumentVersion, DocumentCategory)
  - Project and Folder entities (Project, Folder) with hierarchical relationships
  - Team entities (Team, TeamMember) with role-based access
  - Service interfaces (IDocumentService, IProjectService, IFolderService, ITeamService, IDocumentStorageService)
  - DTOs and business logic models with validation and organizational support
  
- **DocFlowHub.Infrastructure** 🔧: Data access, external services, and implementations
  - Entity Framework Core with SQL Server and complex hierarchical relationships
  - Complete service implementations with async patterns and organizational operations
  - Azure Blob Storage integration with secure file management
  - Database configurations and migrations including organizational schema
  
- **DocFlowHub.Web** 🌐: ASP.NET Core web application with professional UI
  - Razor Pages with Azure Portal-style layouts and tree navigation
  - Responsive Bootstrap 5.3 styling with custom enhancements and organizational components
  - Professional loading states and toast notification system
  - Security-first approach with proper authorization and enhanced authentication patterns

## 🔄 CI/CD

The project uses GitHub Actions for continuous integration and deployment:
- Automated builds
- Unit tests execution (23/23 tests passing)
- Code quality checks
- Azure integration tests

## 🚀 Recent Major Achievements

### Sprint 6 Achievements ✅ **ENTERPRISE PLATFORM COMPLETED**
- **Complete Organizational System**: Full project and folder hierarchy with unlimited nesting capability
- **Advanced User Operations**: Professional drag-and-drop interface, bulk operations, enhanced search functionality
- **Enhanced Security**: All identified vulnerabilities resolved with improved authentication patterns
- **Quality Assurance**: Comprehensive bug fixes, 23/23 tests passing, enhanced stability and security
- **Professional UX**: Complete Azure Portal-style interface with tree navigation, breadcrumbs, and context menus

### Sprint 5 Achievements ✅ **AI PLATFORM FOUNDATION**
- **AI Document Summarization**: Real OpenAI API integration with automatic summary generation on upload
- **AI Version Comparison**: AI-powered analysis of document differences with professional UI
- **Complete AI Settings System**: Backend + UI + upload integration with user configuration
- **AI Analytics Dashboard**: Interactive charts, usage monitoring, and cost optimization insights
- **User Limits Management**: Rate limiting system with admin monitoring and controls
- **Performance Optimization**: Multi-level caching reducing API costs and improving response times

### Technical Excellence ✅
- **Complete Platform Architecture**: Clean separation with proper service layer and dependency injection
- **Database Excellence**: Multiple migrations for organizational and AI entities with proper relationships
- **Testing Coverage**: All 23/23 tests passing with comprehensive coverage including organizational features
- **Production Ready**: Real OpenAI API integration with comprehensive error handling
- **Performance Optimized**: Intelligent caching strategies, hierarchical query optimization, and cost management
- **Enhanced Security**: Vulnerability fixes, improved authentication patterns, comprehensive authorization
- **Zero Technical Debt**: Professional implementation maintaining code quality standards throughout

### Sprint 6 Scope Achievement ✅
- ✅ **Complete Organizational System**: Project and folder hierarchy with advanced features (100% complete)
- ✅ **Advanced Operations**: Drag-and-drop interface, bulk operations, enhanced search (100% complete)
- ✅ **Quality Assurance**: All critical bugs resolved, enhanced security, comprehensive testing (100% complete)
- ✅ **AI Integration**: All AI features working seamlessly with organizational structure
- 🚀 **Advanced Enterprise Features**: Ready for Sprint 7 (Smart categorization, external integrations)

## 📜 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details. 