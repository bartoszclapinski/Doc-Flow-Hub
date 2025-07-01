using DocFlowHub.Core.Models.AI;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DocFlowHub.Infrastructure.Data.Configurations;

/// <summary>
/// Entity Framework configuration for AISettings entity
/// </summary>
public class AISettingsConfiguration : IEntityTypeConfiguration<AISettings>
{
    public void Configure(EntityTypeBuilder<AISettings> builder)
    {
        // Table configuration
        builder.ToTable("AISettings");
        
        // Primary key
        builder.HasKey(ai => ai.Id);
        
        // Properties configuration
        builder.Property(ai => ai.UserId)
            .IsRequired()
            .HasMaxLength(450); // Standard ASP.NET Identity user ID length
            
        builder.Property(ai => ai.PreferredModel)
            .IsRequired()
            .HasConversion<int>(); // Store enum as integer
            
        builder.Property(ai => ai.CustomApiKey)
            .HasMaxLength(200); // Optional, nullable
            
        builder.Property(ai => ai.UseCustomApiKey)
            .IsRequired()
            .HasDefaultValue(false);
            
        builder.Property(ai => ai.SummarizationEnabled)
            .IsRequired()
            .HasDefaultValue(true);
            
        builder.Property(ai => ai.VersionComparisonEnabled)
            .IsRequired()
            .HasDefaultValue(true);
            
        builder.Property(ai => ai.AutoSummarizeOnUpload)
            .IsRequired()
            .HasDefaultValue(true);
            
        builder.Property(ai => ai.QualityPreference)
            .IsRequired()
            .HasPrecision(3, 2) // 0.00 to 1.00
            .HasDefaultValue(0.7);
            
        builder.Property(ai => ai.MaxTokensPerOperation)
            .IsRequired()
            .HasDefaultValue(2000);
            
        builder.Property(ai => ai.ComparisonSensitivity)
            .IsRequired()
            .HasPrecision(3, 2) // 0.00 to 1.00
            .HasDefaultValue(0.5);
            
        builder.Property(ai => ai.EnableCaching)
            .IsRequired()
            .HasDefaultValue(true);
            
        builder.Property(ai => ai.CacheDurationHours)
            .IsRequired()
            .HasDefaultValue(24);
            
        builder.Property(ai => ai.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");
            
        builder.Property(ai => ai.UpdatedAt)
            .IsRequired(false); // Nullable
            
        builder.Property(ai => ai.IsActive)
            .IsRequired()
            .HasDefaultValue(true);
        
        // Indexes for performance
        builder.HasIndex(ai => ai.UserId)
            .IsUnique() // One settings record per user
            .HasDatabaseName("IX_AISettings_UserId");
            
        builder.HasIndex(ai => ai.IsActive)
            .HasDatabaseName("IX_AISettings_IsActive");
            
        builder.HasIndex(ai => ai.CreatedAt)
            .HasDatabaseName("IX_AISettings_CreatedAt");
        
        // Foreign key relationship to ApplicationUser
        builder.HasOne(ai => ai.User)
            .WithOne() // One-to-one relationship
            .HasForeignKey<AISettings>(ai => ai.UserId)
            .OnDelete(DeleteBehavior.Cascade); // Delete settings when user is deleted
            
        // Add check constraints for validation
        builder.HasCheckConstraint("CK_AISettings_QualityPreference", 
            "[QualityPreference] >= 0.0 AND [QualityPreference] <= 1.0");
            
        builder.HasCheckConstraint("CK_AISettings_ComparisonSensitivity", 
            "[ComparisonSensitivity] >= 0.0 AND [ComparisonSensitivity] <= 1.0");
            
        builder.HasCheckConstraint("CK_AISettings_MaxTokensPerOperation", 
            "[MaxTokensPerOperation] >= 100 AND [MaxTokensPerOperation] <= 10000");
            
        builder.HasCheckConstraint("CK_AISettings_CacheDurationHours", 
            "[CacheDurationHours] >= 1 AND [CacheDurationHours] <= 168");
    }
} 