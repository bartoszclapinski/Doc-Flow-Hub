# 📄 Doc-Flow-Hub

Document Management System built with ASP.NET Core 9.

![Build Status](https://github.com/bartoszclapinski/Doc-Flow-Hub/actions/workflows/ci.yml/badge.svg)

## 🚀 Description

Doc-Flow-Hub is a modern **AI-powered document management system** that helps teams organize, track, and collaborate on documents efficiently. The system provides powerful version control, team collaboration features, AI-powered document analysis, and a professional Azure Portal-style user interface.

## 🎯 Current Status

**Sprint 5: 100% COMPLETE** 🎉 - **AI-Powered Document Platform Achieved**

### ✅ Production-Ready AI Platform
- **Complete AI Integration**: Document summarization and version comparison working end-to-end
- **User AI Configuration**: Professional settings interface with smart defaults and real-time feedback
- **AI Analytics Dashboard**: Comprehensive usage monitoring and cost optimization
- **Performance Optimized**: Multi-level caching reducing API costs and improving response times
- **Enterprise Grade**: Real OpenAI API integration with professional error handling

### ✅ Fully Working Features
- **Complete Document Lifecycle**: Upload → Browse (with sorting & filtering) → View → Edit → Download
- **AI Document Analysis**: Automatic summarization and intelligent version comparison
- **Team Collaboration**: Create teams, share documents, team-based filtering, secure access control  
- **Professional UI**: Azure Portal-style interface with responsive design and loading states
- **Complete Admin System**: System statistics, user management, AI analytics, and user limits monitoring
- **Security**: Users can only access their own documents and team-shared documents
- **Version Management**: Complete version history with AI-powered difference analysis
- **Dynamic Dashboard**: Real user statistics and activity feed

## ✨ Features

### 👤 User Management
- **Authentication and Authorization**
  - Secure login and registration
  - Email confirmation
  - Password recovery
- **Profile Management** 
  - Customizable user profiles
  - Profile picture upload and management
  - Bio and personal information
  - Password management
- **Role-based Access Control** 
  - Admin and user roles with complete dashboard
  - Permission-based actions and system management
  - User statistics and administrative tools

### 👥 Team Collaboration 
- **Team Management** 
  - Create and manage teams
  - Team member roles (Owner, Member)
  - Team activity tracking
  - Team invitation system
- **Document Sharing** 
  - Share documents with specific teams
  - Team-based document filtering
  - Secure access control (only team members can access shared documents)
  - Real-time sharing/unsharing with feedback messages

### 📝 Document Management 
- **Document Organization** 
  - Categorization and tagging system (many-to-many relationships)
  - Professional Azure Portal-style table layout
  - Custom metadata (title, description, change summaries)
  - Hierarchical storage in Azure Blob Storage with secure access
- **Version Control** 
  - Automatic version tracking with sequential numbering
  - Complete version history with metadata
  - Version restoration and downloads
  - Secure file storage with proper resource management
- **Search and Filtering** 
  - Advanced search by title and metadata
  - Filter by team membership, categories, and file types
  - Column sorting (Title, Modified Date, File Size) with visual indicators
  - Pagination with UX-optimized page reset on sorting
  - File type validation (PDF, DOC, DOCX, TXT, MD, images) 
  - Size limit enforcement (30MB) 

### 🔒 Security 
- **Authentication & Authorization** 
  - Secure password policies with ASP.NET Core Identity
  - Account lockout protection
  - Email confirmation and password recovery
  - Session management
  - Role-based access control (Admin, User roles)
- **Document Security** 
  - User can only access their own documents and team-shared documents
  - Secure document filtering with ownership and team membership validation
  - HTTPS enforcement throughout
  - Secure file access with proper resource management
  - File type and size validation
- **Data Protection** 
  - SQL injection prevention with parameterized queries
  - XSS prevention with proper encoding
  - Resource cleanup and proper disposal
  - Comprehensive error handling without information leakage

### 🤖 AI-Powered Features
- **Document Summarization**
  - Automatic AI summary generation on upload
  - Confidence scores and key points extraction
  - Background processing with real OpenAI API integration
  - Professional UI display with metadata
- **AI Version Comparison**
  - Intelligent analysis of document differences
  - AI-powered change detection and summarization
  - Model selection with cost estimation
  - Professional comparison results display
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

### Sprint 5 Achievements ✅ **MAJOR MILESTONE COMPLETED**
- **AI Document Summarization**: Real OpenAI API integration with automatic summary generation on upload
- **AI Version Comparison**: AI-powered analysis of document differences with professional UI
- **Complete AI Settings System**: Backend + UI + upload integration with user configuration
- **AI Analytics Dashboard**: Interactive charts, usage monitoring, and cost optimization insights
- **User Limits Management**: Rate limiting system with admin monitoring and controls
- **Performance Optimization**: Multi-level caching reducing API costs and improving response times
- **Model Selection UI**: Dynamic loading with proper helper methods and cost estimation
- **Real-time Progress Feedback**: Professional upload experience with AI processing stages

### Technical Excellence ✅
- **AI Platform Architecture**: Clean separation with proper service layer and dependency injection
- **Database Enhancements**: 3 new migrations for AI entities with proper relationships
- **Testing Coverage**: All 21/21 tests continue to pass with comprehensive AI infrastructure
- **Production Ready**: Real OpenAI API integration with comprehensive error handling
- **Performance Optimized**: Intelligent caching strategies and cost management
- **Zero Technical Debt**: Professional implementation maintaining code quality standards

### Sprint 5 Scope Achievement ✅
- ✅ **Core AI Features**: Document summarization and version comparison (100% complete)
- ✅ **User AI Configuration**: Complete settings system with professional interface
- ✅ **AI Analytics**: Comprehensive monitoring and usage tracking system
- ✅ **Performance Optimization**: Multi-level caching and cost optimization
- 📋 **Project/Folder Organization**: Moved to Sprint 6 for focused approach
- 📋 **Smart Categorization + Document Locking**: Moved to Final Sprint

## 📜 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details. 