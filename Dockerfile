# Self-contained public demo image (used by Render, see render.yaml).
# Everything runs inside this one container: SQLite database + document files on local disk,
# rebuilt from the demo seed on every start. Read-only demo mode, no AI keys.

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore first so this layer is cached until a project file changes.
COPY DocFlowHub.sln ./
COPY src/DocFlowHub.Core/DocFlowHub.Core.csproj src/DocFlowHub.Core/
COPY src/DocFlowHub.Infrastructure/DocFlowHub.Infrastructure.csproj src/DocFlowHub.Infrastructure/
COPY src/DocFlowHub.Web/DocFlowHub.Web.csproj src/DocFlowHub.Web/
RUN dotnet restore src/DocFlowHub.Web/DocFlowHub.Web.csproj

# Client libraries (Bootstrap, jQuery, Chart.js) are not in git; restore them from libman.json.
# The libman tool targets .NET 8, so let it roll forward to the .NET 10 runtime in this image.
RUN dotnet tool install --global Microsoft.Web.LibraryManager.Cli --version 3.0.71
ENV PATH="${PATH}:/root/.dotnet/tools"
COPY src/DocFlowHub.Web/libman.json src/DocFlowHub.Web/
RUN cd src/DocFlowHub.Web && DOTNET_ROLL_FORWARD=Major libman restore

COPY src/ src/
RUN dotnet publish src/DocFlowHub.Web/DocFlowHub.Web.csproj -c Release --no-restore -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .

# - FORWARDEDHEADERS: Render terminates HTTPS in front of the app; trust its X-Forwarded-* headers.
# - PORT: listen on 10000 (Render's default) unless the host sets PORT.
# - Logging: Microsoft=Warning keeps EF from logging every SQL command of the seed.
ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_FORWARDEDHEADERS_ENABLED=true \
    PORT=10000 \
    DemoMode__Enabled=true \
    Database__Provider=Sqlite \
    ConnectionStrings__DefaultConnection="Data Source=/tmp/docflowhub/demo.db" \
    DocumentStorage__Provider=Local \
    DocumentStorage__LocalPath=/tmp/docflowhub/files \
    Logging__LogLevel__Default=Information \
    Logging__LogLevel__Microsoft=Warning

# Fresh data on every start: the container filesystem is thrown away when it stops.
RUN mkdir -p /tmp/docflowhub && chown -R $APP_UID /tmp/docflowhub
USER $APP_UID
EXPOSE 10000
ENTRYPOINT ["dotnet", "DocFlowHub.Web.dll"]
