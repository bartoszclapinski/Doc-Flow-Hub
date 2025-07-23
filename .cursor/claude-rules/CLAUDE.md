# CLAUDE.md - DocFlowHub Project Rules

## Project Overview
DocFlowHub is a comprehensive document management system built with ASP.NET Core 9.0, following Clean Architecture principles. The project is currently in Sprint 8 (Modern UI/UX Design System) with 75% completion, focusing on enterprise-grade visual transformation.

## Technology Stack
- **Backend**: ASP.NET Core 9.0 MVC + Razor Pages
- **Database**: Entity Framework Core 9.0 with SQL Server
- **Frontend**: Bootstrap 5.3 + minimal JavaScript
- **Authentication**: ASP.NET Core Identity
- **Storage**: Azure Blob Storage for documents
- **Testing**: xUnit + Moq
- **CI/CD**: GitHub Actions + Azure App Service

## Architecture Guidelines

### Clean Architecture Structure
```
DocFlowHub/
├── DocFlowHub.Core/          # Domain layer (entities, interfaces, DTOs)
├── DocFlowHub.Infrastructure/ # Data access and external services
└── DocFlowHub.Web/           # Presentation layer (Razor Pages)
```

### Coding Standards

#### Backend Development (.NET)
- Use ASP.NET Core 9.0 with minimal APIs for simple endpoints
- Implement MediatR pattern for decoupling request handling
- Apply dependency injection with appropriate lifetimes (scoped for request-specific, singleton for stateless)
- Use proper exception handling with middleware for consistent error responses
- Implement response caching with ETags for high-traffic endpoints

#### Entity Framework Guidelines
- Use repository and unit of work patterns for data access abstraction
- Implement eager loading with Include() to avoid N+1 query problems
- Apply AsNoTracking() for read-only queries to optimize performance
- Use migrations with proper naming conventions for schema changes
- Implement query optimization with compiled queries for frequent operations

#### Document Management Architecture
- Implement versioning using separate Version entity with sequential numbering
- Use content-based file storage with database metadata references
- Apply team-based access control with hierarchical permission model
- Use background services for document processing and AI analysis
- Apply CQRS pattern to separate read and write operations
- Implement robust file type validation and virus scanning

### Development Approach

#### Support Level: Beginner-Friendly
- Execute up to 3 actions at a time and ask for approval
- Write code with clear variable names and explanatory comments
- Provide full implementations with import statements and dependencies
- Add defensive coding patterns and clear error handling
- Suggest simpler solutions first, then offer optimized versions with trade-offs
- Explain approaches and link to relevant documentation
- Confirm before proceeding with fixes and explain root causes
- Include basic test cases demonstrating functionality

#### Database Configuration
- Use SQL Server Express LocalDB for development
- Azure SQL Database (Basic tier) for production
- Implement soft delete with IsDeleted flags
- Apply global query filters for soft-deleted entities
- Use proper cascade delete rules and foreign key constraints

#### Frontend Guidelines
- Use Bootstrap 5.3 for responsive UI components
- Implement glass morphism design with light/dark theme system
- Minimize JavaScript usage (only where necessary)
- Create reusable Razor partial views for common components
- Ensure mobile-responsive design without horizontal scrolling

#### Testing Standards
- Use xUnit for unit testing with Moq for mocking
- Focus on testing core business functionality
- Implement in-memory database testing for EF Core
- Achieve comprehensive test coverage for services
- Test navigation properties and relationships thoroughly

### Project Status

#### Current Sprint: Sprint 8 (Modern UI/UX Design System)
**Progress**: 75% Complete
- ✅ Phase 8.1: Authentication & Landing Pages (Complete)
- ✅ Phase 8.2: Main Application Layout (Complete)  
- ✅ Phase 8.3: Admin Dashboard Transformation (Complete)
- 🎯 Phase 8.4: Dashboard Modernization (Current)
- 📋 Phase 8.5: Feature Pages Transformation (Planned)

#### Completed Features
- User authentication and registration with ASP.NET Core Identity
- Document upload, storage, and management with Azure Blob Storage
- Version tracking and document categorization
- Team-based collaboration and sharing
- Project and folder organization system
- Admin panel with user and role management
- Analytics and system settings
- Modern theme system with light/dark mode

#### Key Commands
- **Build**: `dotnet build`
- **Test**: `dotnet test`
- **Migrations**: `dotnet ef migrations add <Name>` and `dotnet ef database update`
- **Run**: `dotnet run --project DocFlowHub.Web`

### Development Guidelines

#### File Organization
- Keep entities in DocFlowHub.Core/Models/
- Place service interfaces in DocFlowHub.Core/Services/Interfaces/
- Implement services in DocFlowHub.Infrastructure/Services/
- Create Razor Pages in DocFlowHub.Web/Pages/
- Store configurations in DocFlowHub.Infrastructure/Data/Configurations/

#### Error Handling
- Use ServiceResult<T> pattern for service operations
- Implement proper exception handling middleware
- Validate user inputs with data annotations
- Return meaningful error messages to users
- Log errors appropriately without exposing sensitive information

#### Security Practices
- Never expose or log secrets and keys
- Implement proper authorization with role-based access
- Use HTTPS with proper certificate management
- Validate and sanitize all user inputs
- Implement audit trails for sensitive operations

#### Performance Considerations
- Use async/await for I/O operations
- Implement proper caching strategies
- Optimize database queries with proper indexing
- Use pagination for large data sets
- Implement proper resource disposal patterns

### Current Development Context
The project is enterprise-ready with comprehensive document management, team collaboration, and modern UI. Focus on maintaining code quality, following established patterns, and enhancing user experience while preserving existing functionality.

## Notes
- The project uses nullable reference types - handle null values appropriately
- All services use dependency injection - register new services in DependencyInjection.cs
- Document storage uses Azure Blob Storage - ensure proper connection string configuration
- Testing is comprehensive - maintain test coverage when adding new features
- UI follows modern design principles - maintain consistency with established theme system