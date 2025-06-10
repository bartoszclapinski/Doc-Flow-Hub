# 📄 Doc-Flow-Hub

Document Management System built with ASP.NET Core 9.

![Build Status](https://github.com/bartoszclapinski/Doc-Flow-Hub/actions/workflows/ci.yml/badge.svg)

## 🚀 Description

Doc-Flow-Hub is a modern document management system that helps teams organize, track, and collaborate on documents efficiently. The system provides powerful version control, team collaboration features, and a clean user interface.

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

### 👥 Team Collaboration
- **Team Management**
  - Create and manage teams
  - Team member roles (Admin, Member)
  - Team activity tracking
- **Document Sharing**
  - Share documents with specific teams
  - Control access permissions

### 📝 Document Management
- **Document Organization**
  - Categorization and tagging
  - Folder/project structure
  - Custom metadata
  - Hierarchical storage in Azure Blob Storage ✅
- **Version Control**
  - Automatic version tracking
  - Difference visualization
  - Version restoration
  - Secure file storage with SAS tokens ✅
- **Search and Filtering**
  - Search by metadata
  - Filter by various criteria
  - File type validation ✅
  - Size limit enforcement ✅

### 🔒 Security
- Secure password policies
- Account lockout protection
- Email confirmation
- Session management
- HTTPS enforcement
- Secure file access with SAS tokens ✅
- Resource cleanup and proper disposal ✅

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

The solution follows Clean Architecture principles:

- **DocFlowHub.Core** 📦: Domain models, interfaces, and business logic
  - Models for documents, teams, and user profiles
  - Service interfaces
  - Domain logic
  
- **DocFlowHub.Infrastructure** 🔧: Data access, external services, and implementations
  - Entity Framework Core implementation
  - Service implementations
  - Azure Blob Storage integration
  - External integrations
  
- **DocFlowHub.Web** 🌐: ASP.NET Core web application and UI
  - Razor Pages
  - Controllers and views
  - Bootstrap styling

## 🔄 CI/CD

The project uses GitHub Actions for continuous integration and deployment:
- Automated builds
- Unit tests execution
- Code quality checks
- Azure integration tests

## 📜 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details. 