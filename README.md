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
- **Version Control**
  - Automatic version tracking
  - Difference visualization
  - Version restoration
- **Search and Filtering**
  - Search by metadata
  - Filter by various criteria

### 🔒 Security
- Secure password policies
- Account lockout protection
- Email confirmation
- Session management
- HTTPS enforcement

## 🏁 Getting Started

### Prerequisites

- **.NET 9 SDK**
- **SQL Server**
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

4. Update the database
```bash
dotnet ef database update --project src/DocFlowHub.Infrastructure --startup-project src/DocFlowHub.Web
```

5. Run the application
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

## 📜 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details. 