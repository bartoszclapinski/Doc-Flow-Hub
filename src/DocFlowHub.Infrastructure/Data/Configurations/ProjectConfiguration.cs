using DocFlowHub.Core.Models.Projects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DocFlowHub.Infrastructure.Data.Configurations;

public class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.HasKey(p => p.Id);
        
        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(100);
            
        builder.Property(p => p.Description)
            .HasMaxLength(500);
            
        builder.Property(p => p.Color)
            .HasMaxLength(7);
            
        builder.Property(p => p.Icon)
            .HasMaxLength(50);
            
        builder.Property(p => p.IsActive)
            .IsRequired()
            .HasDefaultValue(true);
            
        builder.Property(p => p.OwnerId)
            .IsRequired()
            .HasMaxLength(450);
            
        builder.Property(p => p.CreatedAt)
            .IsRequired();
            
        builder.Property(p => p.UpdatedAt);
            
        // Foreign key relationships
        builder.HasOne(p => p.Owner)
            .WithMany()
            .HasForeignKey(p => p.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);
            
        // Navigation properties
        builder.HasMany(p => p.Folders)
            .WithOne(f => f.Project)
            .HasForeignKey(f => f.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);
            
        builder.HasMany(p => p.Documents)
            .WithOne(d => d.Project)
            .HasForeignKey(d => d.ProjectId)
            .OnDelete(DeleteBehavior.SetNull);
            
        // Index for performance
        builder.HasIndex(p => p.OwnerId);
        builder.HasIndex(p => p.IsActive);
    }
} 