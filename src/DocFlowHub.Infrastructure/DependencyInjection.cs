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

namespace DocFlowHub.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

        // Document Services
        services.AddScoped<IDocumentService, DocumentService>();
        services.AddScoped<IDocumentCategoryService, DocumentCategoryService>();
        services.AddScoped<IDocumentStorageService, DocumentStorageService>();
        
        // Team Services
        services.AddScoped<ITeamService, TeamService>();
        
        // Profile Services
        services.AddScoped<IProfileService, ProfileService>();
        
        // Role Services
        services.AddScoped<IRoleService, RoleService>();
        
        // AI Services
        services.AddScoped<IAIService, OpenAIService>();
        services.AddScoped<IDocumentSummaryService, DocumentSummaryService>();
        
        // Configure Document Storage
        services.Configure<DocumentStorageOptions>(
            configuration.GetSection("DocumentStorage"));

        return services;
    }
} 