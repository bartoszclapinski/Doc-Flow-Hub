using DocFlowHub.Core.Models.Admin;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DocFlowHub.Infrastructure.Data.Configurations;

public class SystemSettingsConfiguration : IEntityTypeConfiguration<SystemSettings>
{
    public void Configure(EntityTypeBuilder<SystemSettings> builder)
    {
        builder.ToTable("SystemSettings");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Key)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(s => s.Value)
            .IsRequired();

        builder.Property(s => s.Description)
            .HasMaxLength(500);

        builder.Property(s => s.Category)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(s => s.DataType)
            .IsRequired()
            .HasMaxLength(20)
            .HasDefaultValue("String");

        builder.Property(s => s.IsSensitive)
            .HasDefaultValue(false);

        builder.Property(s => s.RequiresRestart)
            .HasDefaultValue(false);

        builder.Property(s => s.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(s => s.UpdatedAt)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(s => s.UpdatedBy)
            .HasMaxLength(450);

        // Indexes
        builder.HasIndex(s => s.Key)
            .IsUnique()
            .HasDatabaseName("IX_SystemSettings_Key");

        builder.HasIndex(s => s.Category)
            .HasDatabaseName("IX_SystemSettings_Category");

        // Seed default settings
        builder.HasData(GetDefaultSettings());
    }

    private static List<SystemSettings> GetDefaultSettings()
    {
        var baseDate = new DateTime(2025, 1, 3, 0, 0, 0, DateTimeKind.Utc);
        
        return new List<SystemSettings>
        {
            // AI Settings
            new SystemSettings
            {
                Id = 1,
                Key = "AI.OpenAI.Model",
                Value = "gpt-4o-mini",
                Description = "Default OpenAI model for AI operations",
                Category = "AI",
                DataType = "String",
                DefaultValue = "gpt-4o-mini",
                IsSensitive = false,
                RequiresRestart = false,
                CreatedAt = baseDate,
                UpdatedAt = baseDate
            },
            new SystemSettings
            {
                Id = 2,
                Key = "AI.OpenAI.MaxTokens",
                Value = "4000",
                Description = "Maximum tokens per AI request",
                Category = "AI",
                DataType = "Number",
                DefaultValue = "4000",
                IsSensitive = false,
                RequiresRestart = false,
                CreatedAt = baseDate,
                UpdatedAt = baseDate
            },
            new SystemSettings
            {
                Id = 3,
                Key = "AI.Usage.DailyLimit",
                Value = "1000",
                Description = "Daily AI usage limit per user",
                Category = "AI",
                DataType = "Number",
                DefaultValue = "1000",
                IsSensitive = false,
                RequiresRestart = false,
                CreatedAt = baseDate,
                UpdatedAt = baseDate
            },

            // Security Settings
            new SystemSettings
            {
                Id = 4,
                Key = "Security.Session.TimeoutMinutes",
                Value = "60",
                Description = "Session timeout in minutes",
                Category = "Security",
                DataType = "Number",
                DefaultValue = "60",
                IsSensitive = false,
                RequiresRestart = true,
                CreatedAt = baseDate,
                UpdatedAt = baseDate
            },
            new SystemSettings
            {
                Id = 5,
                Key = "Security.Password.RequireDigit",
                Value = "true",
                Description = "Require digit in passwords",
                Category = "Security",
                DataType = "Boolean",
                DefaultValue = "true",
                IsSensitive = false,
                RequiresRestart = true,
                CreatedAt = baseDate,
                UpdatedAt = baseDate
            },
            new SystemSettings
            {
                Id = 6,
                Key = "Security.Password.RequiredLength",
                Value = "8",
                Description = "Minimum password length",
                Category = "Security",
                DataType = "Number",
                DefaultValue = "8",
                IsSensitive = false,
                RequiresRestart = true,
                CreatedAt = baseDate,
                UpdatedAt = baseDate
            },

            // Performance Settings
            new SystemSettings
            {
                Id = 7,
                Key = "Performance.Cache.DefaultExpirationMinutes",
                Value = "30",
                Description = "Default cache expiration in minutes",
                Category = "Performance",
                DataType = "Number",
                DefaultValue = "30",
                IsSensitive = false,
                RequiresRestart = false,
                CreatedAt = baseDate,
                UpdatedAt = baseDate
            },
            new SystemSettings
            {
                Id = 8,
                Key = "Performance.Upload.MaxFileSizeMB",
                Value = "50",
                Description = "Maximum file upload size in MB",
                Category = "Performance",
                DataType = "Number",
                DefaultValue = "50",
                IsSensitive = false,
                RequiresRestart = true,
                CreatedAt = baseDate,
                UpdatedAt = baseDate
            },

            // System Settings
            new SystemSettings
            {
                Id = 9,
                Key = "System.Registration.AllowPublicRegistration",
                Value = "true",
                Description = "Allow public user registration",
                Category = "System",
                DataType = "Boolean",
                DefaultValue = "true",
                IsSensitive = false,
                RequiresRestart = false,
                CreatedAt = baseDate,
                UpdatedAt = baseDate
            },
            new SystemSettings
            {
                Id = 10,
                Key = "System.Maintenance.MaintenanceMode",
                Value = "false",
                Description = "Enable maintenance mode",
                Category = "System",
                DataType = "Boolean",
                DefaultValue = "false",
                IsSensitive = false,
                RequiresRestart = false,
                CreatedAt = baseDate,
                UpdatedAt = baseDate
            }
        };
    }
} 