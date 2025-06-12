using DocFlowHub.Core.Models.Documents;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DocFlowHub.Infrastructure.Data.Configurations;

public class DocumentConfiguration : IEntityTypeConfiguration<Document>
{
    public void Configure(EntityTypeBuilder<Document> builder)
    {
        builder.HasKey(d => d.Id);
        
        builder.Property(d => d.Title)
            .IsRequired()
            .HasMaxLength(200);
            
        builder.Property(d => d.Description)
            .HasMaxLength(1000);
            
        builder.Property(d => d.FileType)
            .IsRequired()
            .HasMaxLength(50);
            
        builder.Property(d => d.FileSize)
            .IsRequired();
            
        builder.Property(d => d.OwnerId)
            .IsRequired()
            .HasMaxLength(450);
            
        builder.Property(d => d.CreatedAt)
            .IsRequired();
            
        builder.Property(d => d.UpdatedAt);
            
        builder.HasOne(d => d.Owner)
            .WithMany()
            .HasForeignKey(d => d.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);
            
        builder.HasOne(d => d.Team)
            .WithMany()
            .HasForeignKey(d => d.TeamId)
            .OnDelete(DeleteBehavior.SetNull);
            
        builder.HasOne(d => d.CurrentVersion)
            .WithMany()
            .HasForeignKey(d => d.CurrentVersionId)
            .OnDelete(DeleteBehavior.Restrict);
            
        builder.HasMany(d => d.Categories)
            .WithMany(c => c.Documents)
            .UsingEntity(j => j.ToTable("DocumentCategoryMapping"));
            
        builder.HasQueryFilter(d => !d.IsDeleted);
    }
} 