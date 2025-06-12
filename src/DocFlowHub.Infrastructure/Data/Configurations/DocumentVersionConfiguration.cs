using DocFlowHub.Core.Models.Documents;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DocFlowHub.Infrastructure.Data.Configurations;

public class DocumentVersionConfiguration : IEntityTypeConfiguration<DocumentVersion>
{
    public void Configure(EntityTypeBuilder<DocumentVersion> builder)
    {
        builder.HasKey(v => v.Id);
        
        builder.Property(v => v.DocumentId)
            .IsRequired();
            
        builder.Property(v => v.VersionNumber)
            .IsRequired();
            
        builder.Property(v => v.FilePath)
            .IsRequired()
            .HasMaxLength(1000);
            
        builder.Property(v => v.FileType)
            .IsRequired()
            .HasMaxLength(50);
            
        builder.Property(v => v.FileSize)
            .IsRequired();
            
        builder.Property(v => v.CreatedAt)
            .IsRequired();
            
        builder.Property(v => v.UserId)
            .IsRequired()
            .HasMaxLength(450);
            
        builder.Property(v => v.UserName)
            .IsRequired()
            .HasMaxLength(256);
            
        builder.Property(v => v.ChangeSummary)
            .HasMaxLength(1000);
            
        builder.HasOne(v => v.Document)
            .WithMany(d => d.Versions)
            .HasForeignKey(v => v.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
} 