using DocFlowHub.Core.Models.Documents;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DocFlowHub.Infrastructure.Data.Configurations;

public class DocumentVersionConfiguration : IEntityTypeConfiguration<DocumentVersion>
{
    public void Configure(EntityTypeBuilder<DocumentVersion> builder)
    {
        builder.HasKey(v => v.Id);
        
        builder.Property(v => v.FilePath)
            .IsRequired()
            .HasMaxLength(1000);
            
        builder.Property(v => v.FileHash)
            .IsRequired()
            .HasMaxLength(64);
            
        builder.Property(v => v.ChangeSummary)
            .HasMaxLength(500);
            
        builder.Property(v => v.CreatedAt)
            .IsRequired();
            
        builder.Property(v => v.FileSize)
            .IsRequired();
            
        builder.Property(v => v.VersionNumber)
            .IsRequired();
            
        builder.HasOne(v => v.Document)
            .WithMany(d => d.Versions)
            .HasForeignKey(v => v.DocumentId)
            .OnDelete(DeleteBehavior.ClientCascade)
            .IsRequired(false);
            
        builder.HasOne(v => v.User)
            .WithMany()
            .HasForeignKey(v => v.UserId)
            .OnDelete(DeleteBehavior.Restrict);
            
        builder.HasQueryFilter(v => v.Document == null || !v.Document.IsDeleted);
    }
} 