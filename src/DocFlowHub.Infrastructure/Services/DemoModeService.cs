using DocFlowHub.Core.Services.Interfaces;
using Microsoft.Extensions.Configuration;

namespace DocFlowHub.Infrastructure.Services;

/// <summary>
/// Reads the <c>DemoMode:Enabled</c> flag once at construction. Registered as a
/// singleton — the flag is fixed for the lifetime of the process.
/// </summary>
public class DemoModeService : IDemoModeService
{
    public bool IsEnabled { get; }

    public DemoModeService(IConfiguration configuration)
    {
        IsEnabled = configuration.GetValue<bool>("DemoMode:Enabled");
    }
}
