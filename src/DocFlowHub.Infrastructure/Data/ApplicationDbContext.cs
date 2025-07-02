using DocFlowHub.Core.Identity;
using DocFlowHub.Core.Models;
using DocFlowHub.Core.Models.AI;
using DocFlowHub.Core.Models.Documents;
using DocFlowHub.Core.Models.Projects;
using DocFlowHub.Infrastructure.Data.Configurations;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DocFlowHub.Infrastructure.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Team> Teams { get; set; }
    public DbSet<TeamMember> TeamMembers { get; set; }
    public DbSet<Document> Documents { get; set; }
    public DbSet<DocumentVersion> DocumentVersions { get; set; }
    public DbSet<DocumentCategory> DocumentCategories { get; set; }
    
    // Project organization entities
    public DbSet<Project> Projects { get; set; }
    public DbSet<Folder> Folders { get; set; }
    
    // AI-related entities
    public DbSet<DocumentSummary> DocumentSummaries { get; set; }
    public DbSet<VersionComparison> VersionComparisons { get; set; }
    public DbSet<AISettings> AISettings { get; set; }
    public DbSet<AIUsageLog> AIUsageLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        
        // Configure Team entity
        builder.Entity<Team>(entity =>
        {
            entity.HasOne(t => t.Owner)
                .WithMany(u => u.OwnedTeams)
                .HasForeignKey(t => t.OwnerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure TeamMember entity
        builder.Entity<TeamMember>(entity =>
        {
            entity.HasOne(tm => tm.Team)
                .WithMany(t => t.Members)
                .HasForeignKey(tm => tm.TeamId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(tm => tm.User)
                .WithMany(u => u.TeamMemberships)
                .HasForeignKey(tm => tm.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Apply document-related configurations
        builder.ApplyConfiguration(new DocumentConfiguration());
        builder.ApplyConfiguration(new DocumentVersionConfiguration());
        builder.ApplyConfiguration(new DocumentCategoryConfiguration());
        
        // Apply project organization configurations
        builder.ApplyConfiguration(new ProjectConfiguration());
        builder.ApplyConfiguration(new FolderConfiguration());
        
        // Apply AI-related configurations
        builder.ApplyConfiguration(new DocumentSummaryConfiguration());
        builder.ApplyConfiguration(new VersionComparisonConfiguration());
        builder.ApplyConfiguration(new AISettingsConfiguration());
        builder.ApplyConfiguration(new AIUsageLogConfiguration());
    }
} 