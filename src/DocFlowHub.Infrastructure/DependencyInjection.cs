using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using DocFlowHub.Core.Services.Interfaces;
using DocFlowHub.Infrastructure.Data;
using DocFlowHub.Infrastructure.Data.Sqlite;
using DocFlowHub.Infrastructure.Services.Documents;
using DocFlowHub.Infrastructure.Services.Teams;
using DocFlowHub.Infrastructure.Services.Storage;
using DocFlowHub.Infrastructure.Services.Profile;
using DocFlowHub.Infrastructure.Services.Role;
using DocFlowHub.Infrastructure.Services.AI;
using DocFlowHub.Infrastructure.Services.Projects;
using DocFlowHub.Infrastructure.Services.Admin;
using DocFlowHub.Infrastructure.Services.Analytics;
using DocFlowHub.Infrastructure.Services;

namespace DocFlowHub.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (IsProvider(configuration["Database:Provider"], "Sqlite"))
        {
            // Self-contained public demo: SQLite file inside the container, rebuilt on start.
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlite(connectionString)
                    .AddInterceptors(new SqliteCaseInsensitiveSearchInterceptor()));
        }
        else
        {
            // Retry transient errors (e.g. Azure SQL waking from auto-pause); see the strategy
            // for why explicit transactions are left un-retried.
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(
                    connectionString,
                    sql => sql.ExecutionStrategy(dependencies => new ResilientSqlServerExecutionStrategy(dependencies))));
        }

        // Add Memory Caching for AI Services Performance
        services.AddMemoryCache();

        // Demo mode (read-only public demo) — flag read once from config.
        services.AddSingleton<IDemoModeService, DemoModeService>();

        // Document Services
        services.AddScoped<IDocumentService, DocumentService>();
        services.AddScoped<IDocumentCategoryService, DocumentCategoryService>();
        if (IsProvider(configuration["DocumentStorage:Provider"], "Local"))
        {
            services.AddScoped<IDocumentStorageService, LocalDocumentStorageService>();
        }
        else
        {
            services.AddScoped<IDocumentStorageService, DocumentStorageService>();
        }
        
        // Team Services
        services.AddScoped<ITeamService, TeamService>();
        
        // Project and Folder Services
        services.AddScoped<IProjectService, ProjectService>();
        services.AddScoped<IFolderService, FolderService>();
        
        // Profile Services
        services.AddScoped<IProfileService, ProfileService>();
        
        // Role Services
        services.AddScoped<IRoleService, RoleService>();
        
        // AI Services — IAIService routes per requested model ("claude-*" → Anthropic,
        // otherwise OpenAI). Provider services are stateless config holders wrapping a
        // provider client (AnthropicClient / OpenAIClient), so they are SINGLETONS — this
        // pools the underlying HttpClient/connections instead of rebuilding one per request.
        // The router resolves them lazily, so only the provider actually used needs a key.
        services.AddSingleton<OpenAIService>();
        services.AddSingleton<ClaudeAIService>();
        services.AddScoped<IAIService, AIServiceRouter>();
        services.AddScoped<IDocumentSummaryService, DocumentSummaryService>();
        services.AddScoped<IVersionComparisonService, VersionComparisonService>();
        services.AddScoped<ITextExtractionService, TextExtractionService>();
        services.AddScoped<IAISettingsService, AISettingsService>();
        services.AddScoped<IAIUsageTrackingService, AIUsageTrackingService>();
        
        // Admin Services
        services.AddScoped<ISystemSettingsService, SystemSettingsService>();
        services.AddScoped<IAnalyticsService, AnalyticsService>();
        
        // User Management Services
        services.AddScoped<IUserManagementService, UserManagementService>();
        services.AddScoped<IUserActivityService, UserActivityService>();
        services.AddScoped<IUserSecurityService, UserSecurityService>();
        services.AddScoped<IUserCommunicationService, UserCommunicationService>();
        
        // Configure Document Storage
        services.Configure<DocumentStorageOptions>(options =>
        {
            var section = configuration.GetSection("DocumentStorage");
            section.Bind(options);
            
            // Parse connection string to extract AccountName and AccountKey if not explicitly provided
            if (!string.IsNullOrEmpty(options.ConnectionString) && 
                (string.IsNullOrEmpty(options.AccountName) || string.IsNullOrEmpty(options.AccountKey)))
            {
                var connectionStringParts = options.ConnectionString.Split(';');
                foreach (var part in connectionStringParts)
                {
                    var keyValue = part.Split('=', 2);
                    if (keyValue.Length == 2)
                    {
                        var key = keyValue[0].Trim();
                        var value = keyValue[1].Trim();
                        
                        if (key.Equals("AccountName", StringComparison.OrdinalIgnoreCase) && string.IsNullOrEmpty(options.AccountName))
                        {
                            options.AccountName = value;
                        }
                        else if (key.Equals("AccountKey", StringComparison.OrdinalIgnoreCase) && string.IsNullOrEmpty(options.AccountKey))
                        {
                            options.AccountKey = value;
                        }
                    }
                }
            }
        });

        return services;
    }

    private static bool IsProvider(string? configured, string provider) =>
        string.Equals(configured, provider, StringComparison.OrdinalIgnoreCase);
} 