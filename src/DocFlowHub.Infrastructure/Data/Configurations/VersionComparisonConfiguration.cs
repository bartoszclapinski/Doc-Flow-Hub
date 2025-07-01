using DocFlowHub.Core.Models.AI;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DocFlowHub.Infrastructure.Data.Configurations;

/// <summary>
/// Entity Framework configuration for VersionComparison entity
/// </summary>
public class VersionComparisonConfiguration : IEntityTypeConfiguration<VersionComparison>
{
    public void Configure(EntityTypeBuilder<VersionComparison> builder)
    {
        // Table configuration
        builder.ToTable("VersionComparisons");
        
        // Primary key
        builder.HasKey(vc => vc.Id);
        
        // Properties configuration
        builder.Property(vc => vc.DocumentId)
            .IsRequired();
            
        builder.Property(vc => vc.FromVersionId)
            .IsRequired();
            
        builder.Property(vc => vc.ToVersionId)
            .IsRequired();
            
        builder.Property(vc => vc.ChangeSummary)
            .IsRequired()
            .HasMaxLength(2000); // Allow for detailed change summaries
            
        builder.Property(vc => vc.DetailedChanges)
            .HasMaxLength(5000); // Optional detailed breakdown
            
        builder.Property(vc => vc.ChangeType)
            .IsRequired()
            .HasConversion<int>(); // Store enum as integer
            
        builder.Property(vc => vc.Significance)
            .IsRequired()
            .HasConversion<int>(); // Store enum as integer
            
        builder.Property(vc => vc.AIModel)
            .IsRequired()
            .HasMaxLength(50);
            
        builder.Property(vc => vc.ConfidenceScore)
            .HasPrecision(3, 2); // e.g., 0.95
            
        builder.Property(vc => vc.GeneratedAt)
            .IsRequired();
            
        builder.Property(vc => vc.ProcessingTimeMs)
            .IsRequired(false); // Optional performance tracking
            
        builder.Property(vc => vc.TokensUsed)
            .IsRequired(false); // Optional token usage tracking
        
        // Indexes for performance
        builder.HasIndex(vc => vc.DocumentId);
        
        builder.HasIndex(vc => vc.FromVersionId);
        
        builder.HasIndex(vc => vc.ToVersionId);
        
        builder.HasIndex(vc => vc.GeneratedAt);
        
        // Composite index for finding specific comparisons quickly
        builder.HasIndex(vc => new { vc.DocumentId, vc.FromVersionId, vc.ToVersionId })
            .IsUnique(); // One comparison per version pair per document
            
        // Index for querying by significance
        builder.HasIndex(vc => vc.Significance);
        
        // Foreign key relationships
        // Note: We'll establish these relationships in a way that doesn't break existing entities
        builder.HasIndex(vc => vc.DocumentId);
        builder.HasIndex(vc => vc.FromVersionId);
        builder.HasIndex(vc => vc.ToVersionId);
    }
} 