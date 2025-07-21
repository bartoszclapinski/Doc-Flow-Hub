namespace DocFlowHub.Web.Pages.Admin.Roles;

public class RoleDetailViewModel
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public int UsersCount { get; set; }
}

public class RoleDeleteViewModel
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public int UsersCount { get; set; }
}

public class RoleStatisticsViewModel
{
    public int UsersCount { get; set; }
    public int ActiveUsers { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class UserInRoleViewModel
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public bool EmailConfirmed { get; set; }
    public DateTime CreatedAt { get; set; }
    
    public string FullName => $"{FirstName} {LastName}".Trim();
} 