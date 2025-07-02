using DocFlowHub.Core.Models.Projects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DocFlowHub.Infrastructure.Data.Configurations;

public class FolderConfiguration : IEntityTypeConfiguration<Folder>
{
    public void Configure(EntityTypeBuilder<Folder> builder)
    {
        builder.HasKey(f => f.Id);
        
        builder.Property(f => f.Name)
            .IsRequired()
            .HasMaxLength(100);
            
        builder.Property(f => f.Description)
            .HasMaxLength(500);
            
        builder.Property(f => f.Path)
            .IsRequired()
            .HasMaxLength(1000);
            
        builder.Property(f => f.Level)
            .IsRequired()
            .HasDefaultValue(0);
            
        builder.Property(f => f.CreatedByUserId)
            .IsRequired()
            .HasMaxLength(450);
            
        builder.Property(f => f.CreatedAt)
            .IsRequired();
            
        builder.Property(f => f.UpdatedAt);
            
        // Self-referencing hierarchy
        builder.HasOne(f => f.Parent)
            .WithMany(f => f.Children)
            .HasForeignKey(f => f.ParentFolderId)
            .OnDelete(DeleteBehavior.Restrict); // Prevent accidental deletion of parent folders
            
        // Project relationship
        builder.HasOne(f => f.Project)
            .WithMany(p => p.Folders)
            .HasForeignKey(f => f.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);
            
        // Created by user relationship
        builder.HasOne(f => f.CreatedByUser)
            .WithMany()
            .HasForeignKey(f => f.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
            
        // Documents relationship is configured in DocumentConfiguration
            
        // Indexes for performance optimization
        builder.HasIndex(f => f.ProjectId);
        builder.HasIndex(f => f.ParentFolderId);
        builder.HasIndex(f => f.CreatedByUserId);
        builder.HasIndex(f => f.Path);
        builder.HasIndex(f => f.Level);
    }
} 