using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DocFlowHub.Core.Models.AI;

namespace DocFlowHub.Infrastructure.Data.Configurations;

/// <summary>
/// Entity Framework configuration for AIUsageLog entity
/// </summary>
public class AIUsageLogConfiguration : IEntityTypeConfiguration<AIUsageLog>
{
    public void Configure(EntityTypeBuilder<AIUsageLog> builder)
    {
        builder.ToTable("AIUsageLogs");

        // Primary key
        builder.HasKey(a => a.Id);

        // Required fields with constraints
        builder.Property(a => a.UserId)
            .IsRequired()
            .HasMaxLength(450); // Standard ASP.NET Identity user ID length

        builder.Property(a => a.OperationType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(a => a.AIModel)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(a => a.TokensUsed)
            .IsRequired();

        builder.Property(a => a.EstimatedCost)
            .HasColumnType("decimal(10,6)") // Precision for cost tracking
            .IsRequired();

        builder.Property(a => a.ResponseTime)
            .IsRequired();

        builder.Property(a => a.IsSuccess)
            .IsRequired();

        builder.Property(a => a.ErrorMessage)
            .HasMaxLength(1000); // Optional error message

        builder.Property(a => a.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(a => a.DocumentId)
            .IsRequired(false); // Optional document reference

        builder.Property(a => a.InputSize)
            .IsRequired();

        builder.Property(a => a.OutputSize)
            .IsRequired();

        builder.Property(a => a.ServedFromCache)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(a => a.IpAddress)
            .HasMaxLength(45); // IPv6 max length

        builder.Property(a => a.UserAgent)
            .HasMaxLength(500);

        builder.Property(a => a.QualitySetting)
            .IsRequired(false);

        builder.Property(a => a.Metadata)
            .HasColumnType("nvarchar(max)"); // JSON metadata

        // Indexes for performance
        builder.HasIndex(a => a.UserId)
            .HasDatabaseName("IX_AIUsageLogs_UserId");

        builder.HasIndex(a => a.CreatedAt)
            .HasDatabaseName("IX_AIUsageLogs_CreatedAt");

        builder.HasIndex(a => new { a.UserId, a.CreatedAt })
            .HasDatabaseName("IX_AIUsageLogs_UserId_CreatedAt");

        builder.HasIndex(a => a.OperationType)
            .HasDatabaseName("IX_AIUsageLogs_OperationType");

        builder.HasIndex(a => a.DocumentId)
            .HasDatabaseName("IX_AIUsageLogs_DocumentId")
            .HasFilter("[DocumentId] IS NOT NULL");

        builder.HasIndex(a => a.AIModel)
            .HasDatabaseName("IX_AIUsageLogs_AIModel");

        // Foreign key relationship to ApplicationUser
        builder.HasOne<DocFlowHub.Core.Identity.ApplicationUser>()
            .WithMany()
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Optional foreign key relationship to Document
        builder.HasOne<DocFlowHub.Core.Models.Documents.Document>()
            .WithMany()
            .HasForeignKey(a => a.DocumentId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);
    }
} 