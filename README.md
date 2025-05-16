# Doc-Flow-Hub

Document Management System built with ASP.NET Core 8.


## Description

Doc-Flow-Hub is a modern document management system that helps teams organize, track, and collaborate on documents efficiently.

## Features

### User Management
- User Authentication and Authorization
- Profile Management
  - Customizable user profiles
  - Profile picture support
  - Bio and personal information
- Role-based Access Control

### Team Collaboration
- Team Management
  - Create and manage teams
  - Team member roles (Admin, Member)
  - Team activity tracking
- Document Version Control
- Real-time Collaboration

### Security
- Secure password policies
- Account lockout protection
- Email confirmation
- Session management

## Getting Started

### Prerequisites

- .NET 8 SDK
- SQL Server
- Visual Studio 2022 or VS Code

### Installation

1. Clone the repository
```bash
git clone https://github.com/10xDevs/Doc-Flow-Hub.git
```

2. Navigate to the project directory
```bash
cd Doc-Flow-Hub
```

3. Restore dependencies
```bash
dotnet restore
```

4. Run the application
```bash
dotnet run --project src/DocFlowHub.Web
```

## Project Structure

The solution follows Clean Architecture principles:

- **DocFlowHub.Core**: Domain models, interfaces, and business logic
- **DocFlowHub.Infrastructure**: Data access, external services, and implementations
- **DocFlowHub.Web**: ASP.NET Core web application and UI

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details. 