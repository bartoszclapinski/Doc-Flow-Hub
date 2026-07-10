namespace DocFlowHub.Core.Constants;

/// <summary>
/// Well-known credentials for the public read-only demo. These are intentionally
/// public — they are shown on the sign-in page so visitors can log in. Both
/// accounts are read-only (enforced by <c>DemoModePageFilter</c>). Referenced by
/// the demo login banner and the seeder so they can't drift apart.
/// </summary>
public static class DemoAccounts
{
    public const string Password = "Demo123!";

    public const string UserEmail = "demo@docflowhub.com";
    public const string UserFirstName = "Demo";
    public const string UserLastName = "User";

    public const string AdminEmail = "demo-admin@docflowhub.com";
    public const string AdminFirstName = "Demo";
    public const string AdminLastName = "Admin";
}
