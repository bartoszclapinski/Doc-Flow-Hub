using DocFlowHub.Core.Services.Interfaces;
using DocFlowHub.Infrastructure.Services.Documents;
using DocFlowHub.Infrastructure.Services.Storage;
using DocFlowHub.Infrastructure.Services.Teams;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;

namespace DocFlowHub.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Document services
        services.AddScoped<IDocumentService, DocumentService>();
        services.AddScoped<IDocumentCategoryService, DocumentCategoryService>();
        services.AddScoped<IDocumentStorageService, DocumentStorageService>();
        services.AddScoped<ITeamService, TeamService>();

        // Configure document storage
        var storageSection = configuration.GetSection(DocumentStorageOptions.SectionName);
        var connectionString = storageSection.GetValue<string>("ConnectionString") ?? string.Empty;

        // Parse connection string to get account name and key
        var parts = connectionString.Split(';')
            .Select(part => part.Split('=', 2))
            .Where(parts => parts.Length == 2)
            .ToDictionary(parts => parts[0], parts => parts[1]);

        var accountName = parts.GetValueOrDefault("AccountName", string.Empty);
        var accountKey = parts.GetValueOrDefault("AccountKey", string.Empty);

        services.Configure<DocumentStorageOptions>(options =>
        {
            options.ConnectionString = connectionString;
            options.ContainerName = storageSection.GetValue<string>("ContainerName") ?? "documents";
            options.MaxFileSizeBytes = storageSection.GetValue<long>("MaxFileSizeBytes", 31_457_280);
            options.AllowedFileTypes = storageSection.GetSection("AllowedFileTypes").Get<string[]>() 
                ?? new[] { ".md", ".doc", ".docx", ".pdf", ".jpg", ".jpeg", ".png" };
            options.AccountName = accountName;
            options.AccountKey = accountKey;
        });

        return services;
    }
} 