namespace DocFlowHub.Core.Services.Interfaces;

/// <summary>
/// Exposes whether the app is running as a public, read-only "demo"
/// (controlled by the <c>DemoMode:Enabled</c> config flag). Used to enforce
/// read-only behavior, block registration, short-circuit AI generation, and
/// surface the demo banner. Off by default — has no effect in normal runs.
/// </summary>
public interface IDemoModeService
{
    bool IsEnabled { get; }
}
