# Sprint 1: Project Setup - Detailed Plan

## Repository and Project Setup

### 1. Create GitHub Repository
- Create new GitHub repository "Doc-Flow-Hub"
- Initialize with README.md, .gitignore (Visual Studio template), and MIT license
- Set up branch protection rules for main branch
- Create initial project structure with folders:
  - `/src` - for source code
  - `/tests` - for test projects
  - `/docs` - for documentation
  - `/.github/workflows` - for GitHub Actions

### 2. Setup ASP.NET Core 8 Project
- Create ASP.NET Core 8 project with Razor Pages
  ```
  dotnet new webapp -n DocFlowHub.Web -o src/DocFlowHub.Web
  ```
- Create solution file
  ```
  dotnet new sln -n DocFlowHub
  dotnet sln add src/DocFlowHub.Web
  ```
- Create additional class library projects:
  ```
  dotnet new classlib -n DocFlowHub.Core -o src/DocFlowHub.Core
  dotnet new classlib -n DocFlowHub.Infrastructure -o src/DocFlowHub.Infrastructure
  dotnet sln add src/DocFlowHub.Core src/DocFlowHub.Infrastructure
  ```
- Set up project references:
  ```
  # Infrastructure depends on Core
  dotnet add src/DocFlowHub.Infrastructure reference src/DocFlowHub.Core

  # Web project depends on both Core and Infrastructure
  dotnet add src/DocFlowHub.Web reference src/DocFlowHub.Core
  dotnet add src/DocFlowHub.Web reference src/DocFlowHub.Infrastructure
  ```

## Database and Identity Setup

### 3. Configure Entity Framework Core
- Add EF Core packages to DocFlowHub.Infrastructure:
  ```
  dotnet add src/DocFlowHub.Infrastructure package Microsoft.EntityFrameworkCore.SqlServer
  dotnet add src/DocFlowHub.Infrastructure package Microsoft.EntityFrameworkCore.Tools
  dotnet add src/DocFlowHub.Infrastructure package Microsoft.EntityFrameworkCore.Design
  ```
- Create data models in Core project:
  - Document.cs
  - DocumentVersion.cs
  - Category.cs
- Create AppDbContext in Infrastructure project
- Set up database connection string in appsettings.json
- Create initial migration:
  ```
  dotnet ef migrations add InitialCreate --project src/DocFlowHub.Infrastructure --startup-project src/DocFlowHub.Web
  ```

### 4. Implement ASP.NET Core Identity
- Add Identity packages to web project
- Configure Identity in Startup.cs/Program.cs
- Create custom user class extending IdentityUser if needed
- Add Identity tables to AppDbContext
- Add Identity migration:
  ```
  dotnet ef migrations add AddIdentity --project src/DocFlowHub.Infrastructure --startup-project src/DocFlowHub.Web
  ```
- Create basic login and registration pages

## CI/CD and Basic Layout

### 5. Setup GitHub Actions for CI/CD
- Create GitHub Actions workflow file (.github/workflows/ci.yml)
- Configure workflow to:
  - Restore dependencies
  - Build solution
  - Run tests
  - Report test results
- Test workflow with sample commit
- Add build status badge to README.md

### 6. Create Basic Page Layout
- Install Bootstrap via libman.json or npm
- Create _Layout.cshtml with responsive design
- Implement navigation bar with:
  - Logo/Home link
  - Documents section
  - User account section
- Create basic footer with copyright info
- Set up CSS file structure
- Implement simple responsive dashboard page

## Final Configuration and Setup Verification

### 7. Configure Application Settings
- Set up environment-specific settings (Development/Production)
- Configure logging
- Set up static file serving
- Configure error handling and developer exception page
- Set up HTTPS redirection

### 8. Verify and Test Setup
- Run database migrations
- Test user registration and login
- Verify that CI/CD pipeline works
- Check for any configuration issues
- Document setup process for team reference
- Create first release tag v0.1.0

## Definition of Done for Sprint 1
- GitHub repository is set up with proper structure
- ASP.NET Core 8 project with Razor Pages is configured
- Entity Framework Core is set up with SQL Server
- Initial data models are created
- Identity authentication is implemented with login and registration
- GitHub Actions CI/CD pipeline is operational
- Basic page layout with Bootstrap is implemented
- All configurations are properly documented 