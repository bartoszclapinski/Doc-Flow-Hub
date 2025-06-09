using DocFlowHub.Core.Services.Interfaces;
using DocFlowHub.Infrastructure.Services.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DocFlowHub.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddDocumentServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<DocumentStorageOptions>(
            configuration.GetSection(DocumentStorageOptions.SectionName));
            
        services.AddScoped<IDocumentStorageService, DocumentStorageService>();
        
        return services;
    }
} 