using DocFlowHub.Core.Models.AI;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DocFlowHub.Infrastructure.Data.Configurations;

/// <summary>
/// Entity Framework configuration for DocumentSummary entity
/// </summary>
public class DocumentSummaryConfiguration : IEntityTypeConfiguration<DocumentSummary>
{
    public void Configure(EntityTypeBuilder<DocumentSummary> builder)
    {
        // Table configuration
        builder.ToTable("DocumentSummaries");
        
        // Primary key
        builder.HasKey(ds => ds.Id);
        
        // Properties configuration
        builder.Property(ds => ds.DocumentId)
            .IsRequired();
            
        builder.Property(ds => ds.Summary)
            .IsRequired()
            .HasMaxLength(2000); // Allow for longer summaries
            
        builder.Property(ds => ds.KeyPoints)
            .HasMaxLength(1000); // Optional key points
            
        builder.Property(ds => ds.GeneratedAt)
            .IsRequired();
            
        builder.Property(ds => ds.AIModel)
            .IsRequired()
            .HasMaxLength(50);
            
        builder.Property(ds => ds.ConfidenceScore)
            .HasPrecision(3, 2); // e.g., 0.95
        
        // Indexes for performance
        builder.HasIndex(ds => ds.DocumentId)
            .IsUnique(); // One summary per document
            
        builder.HasIndex(ds => ds.GeneratedAt);
        
        // Foreign key relationship to Document
        // Note: We'll establish this relationship in a way that doesn't break existing Document entity
        builder.HasIndex(ds => ds.DocumentId);
    }
} 