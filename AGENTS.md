# AGENTS.md

## Cursor Cloud specific instructions

### Project Overview

DocFlowHub is an ASP.NET Core 9 Razor Pages document management platform. See `CLAUDE.md` and `README.md` for full architecture and feature details.

### Services

| Service | How to start | Port |
|---------|-------------|------|
| **SQL Server** | `sudo docker start sqlserver` (or create: `sudo docker run -d --name sqlserver -e 'ACCEPT_EULA=Y' -e 'MSSQL_SA_PASSWORD=DocFlow@Hub2024!' -p 1433:1433 mcr.microsoft.com/mssql/server:2022-latest`) | 1433 |
| **Azurite** (Azure Storage emulator) | `sudo docker start azurite` (or create: `sudo docker run -d --name azurite -p 10000:10000 -p 10001:10001 -p 10002:10002 mcr.microsoft.com/azure-storage/azurite`) | 10000-10002 |
| **Web App** | `dotnet run --project src/DocFlowHub.Web --launch-profile http` | 5118 |

### Non-obvious caveats

- **Docker daemon must be started first**: Run `sudo dockerd &>/tmp/dockerd.log &` and wait ~5s before starting containers. The VM environment is Docker-in-Docker with `fuse-overlayfs` storage driver and `iptables-legacy`.
- **appsettings files are gitignored**: `appsettings.json`, `appsettings.Development.json`, and `appsettings.Test.json` must be created manually. They are not in the repo. See the `README.md` "Quick Setup" section for the template. For local dev with Docker, use:
  - SQL Server connection: `Server=localhost,1433;Database=DocFlowHub;User Id=sa;Password=DocFlow@Hub2024!;TrustServerCertificate=True;MultipleActiveResultSets=true`
  - Azurite connection: `DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=http://127.0.0.1:10000/devstoreaccount1`
  - The web project config uses `DocumentStorage` as the section name, while the test project uses `Storage`.
- **OpenAI service throws on startup if API key is empty**: The `OpenAIService` constructor throws `InvalidOperationException` if `OpenAI:ApiKey` is missing. It's a scoped service so it only crashes when a page that resolves `IAIService` is accessed, not at app startup. Use a placeholder key like `sk-placeholder-for-dev` to avoid constructor failures (actual AI features will fail gracefully).
- **Client-side libraries via LibMan**: Frontend assets (Bootstrap, jQuery, Chart.js) are managed via `libman.json`. Run `libman restore` from `src/DocFlowHub.Web/` to download them. jQuery is also needed but not in libman.json; manually download to `wwwroot/lib/jquery/dist/jquery.min.js`.
- **Database auto-migrates on startup**: `DbInitializer.InitializeAsync()` runs all migrations and seeds roles (`Administrator`, `Manager`, `User`) plus a default admin user (`admin@docflowhub.com` / `Admin123!`).
- **Test project not in solution file**: The `tests/DocFlowHub.Tests/` project is not listed in `DocFlowHub.sln`. Run tests with: `dotnet test tests/DocFlowHub.Tests/DocFlowHub.Tests.csproj`

### Key commands

See `CLAUDE.md` "Key Commands" section. Additionally:
- **Lint (build warnings)**: `dotnet build` (warnings are the primary lint check; no separate linter configured)
- **Tests**: `dotnet test tests/DocFlowHub.Tests/DocFlowHub.Tests.csproj --verbosity normal`
- **LibMan restore**: `cd src/DocFlowHub.Web && libman restore`
