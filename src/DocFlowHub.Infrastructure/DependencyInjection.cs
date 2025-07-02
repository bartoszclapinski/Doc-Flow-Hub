using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using DocFlowHub.Core.Services.Interfaces;
using DocFlowHub.Infrastructure.Data;
using DocFlowHub.Infrastructure.Services.Documents;
using DocFlowHub.Infrastructure.Services.Teams;
using DocFlowHub.Infrastructure.Services.Storage;
using DocFlowHub.Infrastructure.Services.Profile;
using DocFlowHub.Infrastructure.Services.Role;
using DocFlowHub.Infrastructure.Services.AI;
using DocFlowHub.Infrastructure.Services.Projects;

namespace DocFlowHub.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

        // Add Memory Caching for AI Services Performance
        services.AddMemoryCache();

        // Document Services
        services.AddScoped<IDocumentService, DocumentService>();
        services.AddScoped<IDocumentCategoryService, DocumentCategoryService>();
        services.AddScoped<IDocumentStorageService, DocumentStorageService>();
        
        // Team Services
        services.AddScoped<ITeamService, TeamService>();
        
        // Project and Folder Services
        services.AddScoped<IProjectService, ProjectService>();
        services.AddScoped<IFolderService, FolderService>();
        
        // Profile Services
        services.AddScoped<IProfileService, ProfileService>();
        
        // Role Services
        services.AddScoped<IRoleService, RoleService>();
        
        // AI Services
        services.AddScoped<IAIService, OpenAIService>();
        services.AddScoped<IDocumentSummaryService, DocumentSummaryService>();
        services.AddScoped<IVersionComparisonService, VersionComparisonService>();
        services.AddScoped<ITextExtractionService, TextExtractionService>();
        services.AddScoped<IAISettingsService, AISettingsService>();
        services.AddScoped<IAIUsageTrackingService, AIUsageTrackingService>();
        
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
} 