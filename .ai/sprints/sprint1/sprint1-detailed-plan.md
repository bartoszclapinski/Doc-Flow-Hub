# Sprint 1: Project Setup - Detailed Plan ✅

## Repository and Project Setup ✅

### 1. Create GitHub Repository ✅
- Create new GitHub repository "Doc-Flow-Hub"
- Initialize with README.md, .gitignore (Visual Studio template), and MIT license
- Set up branch protection rules for main branch
- Create initial project structure with folders:
  - `/src` - for source code
  - `/tests` - for test projects
  - `/docs` - for documentation
  - `/.github/workflows` - for GitHub Actions

### 2. Setup ASP.NET Core 9 Project ✅
- Create ASP.NET Core 9 project with Razor Pages
  ```
  dotnet new webapp -n DocFlowHub.Web -o src/DocFlowHub.Web
  ```
- Create solution file ✅
  ```
  dotnet new sln -n DocFlowHub
  dotnet sln add src/DocFlowHub.Web
  ```
- Create additional class library projects ✅
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

### 3. Configure Entity Framework Core ✅
- Add EF Core packages to DocFlowHub.Infrastructure (version 9.0.4):
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

### 4. Implement ASP.NET Core Identity ✅
- Add Identity packages (version 9.0.4) to web project
- Configure Identity in Program.cs
- Create custom ApplicationUser class with additional profile fields
- Add Identity tables to AppDbContext
- Add Identity migration:
  ```
  dotnet ef migrations add AddIdentity --project src/DocFlowHub.Infrastructure --startup-project src/DocFlowHub.Web
  ```
- Create and style login and registration pages
- Move Identity pages from Areas to Pages/Account structure
- Implement proper routing and navigation

## CI/CD and Basic Layout ✅

### 5. Setup GitHub Actions for CI/CD ✅
- Create GitHub Actions workflow file (.github/workflows/ci.yml)
- Configure workflow to:
  - Restore dependencies
  - Build solution
  - Run tests
  - Report test results
- Test workflow with sample commit
- Add build status badge to README.md

### 6. Create Basic Page Layout ✅
- Install Bootstrap 5.3 via libman.json
- Create _Layout.cshtml with responsive design
- Implement navigation bar with:
  - Logo/Home link
  - Documents section
  - User account section
  - Admin section (for authorized users)
- Create basic footer with copyright info
- Set up CSS file structure
- Implement responsive dashboard page
- Add proper authorization checks

## Final Configuration and Setup Verification ✅

### 7. Configure Application Settings ✅
- Set up environment-specific settings (Development/Production)
- Configure logging
- Set up static file serving
- Configure error handling and developer exception page
- Set up HTTPS redirection

### 8. Verify and Test Setup ✅
- Run database migrations
- Test user registration and login
- Verify that CI/CD pipeline works
- Check for any configuration issues
- Document setup process for team reference
- Create first release tag v0.1.0

## Additional Implemented Features ✅
- Profile picture upload functionality
- User profile management pages
- Admin section with proper authorization
- Team management foundation
- Proper page organization and routing
- Enhanced security configurations

## Definition of Done for Sprint 1 ✅
- GitHub repository is set up with proper structure
- ASP.NET Core 9 project with Razor Pages is configured
- Entity Framework Core 9.0.4 is set up with SQL Server
- Initial data models are created and implemented
- Identity authentication is implemented with login and registration
- Pages properly organized in feature folders
- GitHub Actions CI/CD pipeline is operational
- Basic page layout with Bootstrap 5.3 is implemented
- Profile management features are working
- All configurations are properly documented 