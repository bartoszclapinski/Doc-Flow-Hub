# 📄 Doc-Flow-Hub

Document Management System built with ASP.NET Core 9.

![Build Status](https://github.com/bartoszclapinski/Doc-Flow-Hub/actions/workflows/ci.yml/badge.svg)

## 🚀 Description

Doc-Flow-Hub is a modern document management system that helps teams organize, track, and collaborate on documents efficiently. The system provides powerful version control, team collaboration features, and a professional Azure Portal-style user interface.

## 🎯 Current Status

**Sprint 4 Progress**: ~88% Complete - Core document management and team collaboration features are fully implemented and working end-to-end.

### ✅ Fully Working Features
- **Complete Document Lifecycle**: Upload → Browse (with sorting & filtering) → View → Edit → Download
- **Team Collaboration**: Create teams, share documents, team-based filtering, secure access control  
- **Professional UI**: Azure Portal-style interface with responsive design and loading states
- **Security**: Users can only access their own documents and team-shared documents
- **Version Management**: Complete version history with download capabilities
- **Dynamic Dashboard**: Real user statistics and activity feed

## ✨ Features

### 👤 User Management
- **Authentication and Authorization**
  - Secure login and registration
  - Email confirmation
  - Password recovery
- **Profile Management** ✅
  - Customizable user profiles
  - Profile picture upload and management
  - Bio and personal information
  - Password management
- **Role-based Access Control**
  - Admin and user roles
  - Permission-based actions

### 👥 Team Collaboration ✅
- **Team Management** ✅
  - Create and manage teams
  - Team member roles (Owner, Member)
  - Team activity tracking
  - Team invitation system
- **Document Sharing** ✅
  - Share documents with specific teams
  - Team-based document filtering
  - Secure access control (only team members can access shared documents)
  - Real-time sharing/unsharing with feedback messages

### 📝 Document Management ✅
- **Document Organization** ✅
  - Categorization and tagging system (many-to-many relationships)
  - Professional Azure Portal-style table layout
  - Custom metadata (title, description, change summaries)
  - Hierarchical storage in Azure Blob Storage with secure access
- **Version Control** ✅
  - Automatic version tracking with sequential numbering
  - Complete version history with metadata
  - Version restoration and downloads
  - Secure file storage with proper resource management
- **Search and Filtering** ✅
  - Advanced search by title and metadata
  - Filter by team membership, categories, and file types
  - Column sorting (Title, Modified Date, File Size) with visual indicators
  - Pagination with UX-optimized page reset on sorting
  - File type validation (PDF, DOC, DOCX, TXT, MD, images) ✅
  - Size limit enforcement (30MB) ✅

### 🔒 Security ✅
- **Authentication & Authorization** ✅
  - Secure password policies with ASP.NET Core Identity
  - Account lockout protection
  - Email confirmation and password recovery
  - Session management
  - Role-based access control (Admin, User roles)
- **Document Security** ✅
  - User can only access their own documents and team-shared documents
  - Secure document filtering with ownership and team membership validation
  - HTTPS enforcement throughout
  - Secure file access with proper resource management
  - File type and size validation
- **Data Protection** ✅
  - SQL injection prevention with parameterized queries
  - XSS prevention with proper encoding
  - Resource cleanup and proper disposal
  - Comprehensive error handling without information leakage

## 🏁 Getting Started

### Prerequisites

- **.NET 9 SDK**
- **SQL Server**
- **Azure Storage Account**
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
  }
}
```

To get Azure Storage connection string:
1. Go to Azure Portal
2. Navigate to your Storage Account
3. Go to "Access keys" under "Security + networking"
4. Copy the connection string
5. Replace the `Storage:ConnectionString` value in `appsettings.Development.json`

⚠️ Never commit the actual connection string to source control!

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
  - Team entities (Team, TeamMember) with role-based access
  - Service interfaces (IDocumentService, ITeamService, IDocumentStorageService)
  - DTOs and business logic models with validation
  
- **DocFlowHub.Infrastructure** 🔧: Data access, external services, and implementations
  - Entity Framework Core with SQL Server and complex relationships
  - Complete service implementations with async patterns
  - Azure Blob Storage integration with secure file management
  - Database configurations and migrations
  
- **DocFlowHub.Web** 🌐: ASP.NET Core web application with professional UI
  - Razor Pages with Azure Portal-style layouts
  - Responsive Bootstrap 5.3 styling with custom enhancements
  - Professional loading states and toast notification system
  - Security-first approach with proper authorization

## 🔄 CI/CD

The project uses GitHub Actions for continuous integration and deployment:
- Automated builds
- Unit tests execution
- Code quality checks
- Azure integration tests

## 🚀 Recent Improvements

### Sprint 4 Achievements ✅
- **Azure Portal-Style UI**: Professional table layout with horizontal filters and column sorting
- **Enhanced Team Integration**: Seamless document-team collaboration with secure access control
- **Critical Bug Fixes**: Resolved DateTime nullable type mismatches and improved pagination UX
- **Advanced Sorting**: Sortable columns (Title, Modified Date, File Size) with visual indicators
- **Mobile Optimization**: Responsive design without horizontal scrolling issues
- **Professional UX**: Loading states, toast notifications, and enterprise-grade styling

### Technical Excellence ✅
- **Clean Architecture**: Proper separation of concerns with dependency injection
- **Security First**: Document access restricted by ownership and team membership
- **Performance Optimized**: Async/await patterns, proper resource disposal, database-level sorting
- **Quality Assurance**: Comprehensive error handling, automated testing, and code analysis
- **Azure Integration**: Working end-to-end with Azure Blob Storage for document management

## 📜 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details. 