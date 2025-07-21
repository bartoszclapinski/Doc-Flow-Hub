using DocFlowHub.Core.Models.Admin;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DocFlowHub.Infrastructure.Data.Configurations;

public class UserActivityLogConfiguration : IEntityTypeConfiguration<UserActivityLog>
{
    public void Configure(EntityTypeBuilder<UserActivityLog> builder)
    {
        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.UserId)
            .IsRequired()
            .HasMaxLength(450);
            
        builder.Property(x => x.ActivityType)
            .IsRequired()
            .HasMaxLength(100);
            
        builder.Property(x => x.Description)
            .IsRequired()
            .HasMaxLength(500);
            
        builder.Property(x => x.ResourceId)
            .HasMaxLength(450);
            
        builder.Property(x => x.ResourceName)
            .HasMaxLength(200);
            
        builder.Property(x => x.IpAddress)
            .IsRequired()
            .HasMaxLength(45);
            
        builder.Property(x => x.UserAgent)
            .IsRequired()
            .HasMaxLength(500);
            
        builder.Property(x => x.Timestamp)
            .IsRequired();
            
        builder.Property(x => x.AdditionalData)
            .HasColumnType("nvarchar(max)");

        // Indexes for performance
        builder.HasIndex(x => x.UserId)
            .HasDatabaseName("IX_UserActivityLogs_UserId");
            
        builder.HasIndex(x => x.ActivityType)
            .HasDatabaseName("IX_UserActivityLogs_ActivityType");
            
        builder.HasIndex(x => x.Timestamp)
            .HasDatabaseName("IX_UserActivityLogs_Timestamp");
            
        builder.HasIndex(x => new { x.UserId, x.Timestamp })
            .HasDatabaseName("IX_UserActivityLogs_UserId_Timestamp");
    }
}

public class UserSecurityEventConfiguration : IEntityTypeConfiguration<UserSecurityEvent>
{
    public void Configure(EntityTypeBuilder<UserSecurityEvent> builder)
    {
        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.UserId)
            .IsRequired()
            .HasMaxLength(450);
            
        builder.Property(x => x.EventType)
            .IsRequired()
            .HasMaxLength(100);
            
        builder.Property(x => x.Description)
            .IsRequired()
            .HasMaxLength(500);
            
        builder.Property(x => x.Severity)
            .IsRequired()
            .HasMaxLength(20);
            
        builder.Property(x => x.IpAddress)
            .HasMaxLength(45);
            
        builder.Property(x => x.UserAgent)
            .HasMaxLength(500);
            
        builder.Property(x => x.IsResolved)
            .IsRequired()
            .HasDefaultValue(false);
            
        builder.Property(x => x.ResolvedBy)
            .HasMaxLength(450);
            
        builder.Property(x => x.Timestamp)
            .IsRequired();
            
        builder.Property(x => x.EventData)
            .HasColumnType("nvarchar(max)");

        // Indexes
        builder.HasIndex(x => x.UserId)
            .HasDatabaseName("IX_UserSecurityEvents_UserId");
            
        builder.HasIndex(x => x.EventType)
            .HasDatabaseName("IX_UserSecurityEvents_EventType");
            
        builder.HasIndex(x => x.Severity)
            .HasDatabaseName("IX_UserSecurityEvents_Severity");
            
        builder.HasIndex(x => x.IsResolved)
            .HasDatabaseName("IX_UserSecurityEvents_IsResolved");
            
        builder.HasIndex(x => x.Timestamp)
            .HasDatabaseName("IX_UserSecurityEvents_Timestamp");
    }
}

public class UserCommunicationConfiguration : IEntityTypeConfiguration<UserCommunicationEntity>
{
    public void Configure(EntityTypeBuilder<UserCommunicationEntity> builder)
    {
        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.UserId)
            .IsRequired()
            .HasMaxLength(450);
            
        builder.Property(x => x.AdminId)
            .HasMaxLength(450);
            
        builder.Property(x => x.Subject)
            .IsRequired()
            .HasMaxLength(200);
            
        builder.Property(x => x.Message)
            .IsRequired()
            .HasColumnType("nvarchar(max)");
            
        builder.Property(x => x.Type)
            .IsRequired()
            .HasMaxLength(50);
            
        builder.Property(x => x.SentAt)
            .IsRequired();
            
        builder.Property(x => x.IsRead)
            .IsRequired()
            .HasDefaultValue(false);
            
        builder.Property(x => x.SenderIpAddress)
            .HasMaxLength(45);

        // Navigation properties
        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
            
        builder.HasOne(x => x.Admin)
            .WithMany()
            .HasForeignKey(x => x.AdminId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(x => x.UserId)
            .HasDatabaseName("IX_UserCommunications_UserId");
            
        builder.HasIndex(x => x.AdminId)
            .HasDatabaseName("IX_UserCommunications_AdminId");
            
        builder.HasIndex(x => x.Type)
            .HasDatabaseName("IX_UserCommunications_Type");
            
        builder.HasIndex(x => x.IsRead)
            .HasDatabaseName("IX_UserCommunications_IsRead");
            
        builder.HasIndex(x => x.SentAt)
            .HasDatabaseName("IX_UserCommunications_SentAt");
    }
}

public class UserLoginAttemptConfiguration : IEntityTypeConfiguration<UserLoginAttempt>
{
    public void Configure(EntityTypeBuilder<UserLoginAttempt> builder)
    {
        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.UserId)
            .IsRequired()
            .HasMaxLength(450);
            
        builder.Property(x => x.Email)
            .HasMaxLength(100);
            
        builder.Property(x => x.IpAddress)
            .IsRequired()
            .HasMaxLength(45);
            
        builder.Property(x => x.UserAgent)
            .HasMaxLength(500);
            
        builder.Property(x => x.IsSuccessful)
            .IsRequired();
            
        builder.Property(x => x.FailureReason)
            .HasMaxLength(200);
            
        builder.Property(x => x.AttemptedAt)
            .IsRequired();
            
        builder.Property(x => x.Country)
            .HasMaxLength(100);
            
        builder.Property(x => x.City)
            .HasMaxLength(100);

        // Indexes
        builder.HasIndex(x => x.UserId)
            .HasDatabaseName("IX_UserLoginAttempts_UserId");
            
        builder.HasIndex(x => x.IpAddress)
            .HasDatabaseName("IX_UserLoginAttempts_IpAddress");
            
        builder.HasIndex(x => x.IsSuccessful)
            .HasDatabaseName("IX_UserLoginAttempts_IsSuccessful");
            
        builder.HasIndex(x => x.AttemptedAt)
            .HasDatabaseName("IX_UserLoginAttempts_AttemptedAt");
            
        builder.HasIndex(x => new { x.UserId, x.AttemptedAt })
            .HasDatabaseName("IX_UserLoginAttempts_UserId_AttemptedAt");
    }
}

public class UserDeviceConfiguration : IEntityTypeConfiguration<UserDevice>
{
    public void Configure(EntityTypeBuilder<UserDevice> builder)
    {
        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.UserId)
            .IsRequired()
            .HasMaxLength(450);
            
        builder.Property(x => x.DeviceFingerprint)
            .IsRequired()
            .HasMaxLength(500);
            
        builder.Property(x => x.DeviceType)
            .HasMaxLength(100);
            
        builder.Property(x => x.OperatingSystem)
            .HasMaxLength(100);
            
        builder.Property(x => x.Browser)
            .HasMaxLength(100);
            
        builder.Property(x => x.LastIpAddress)
            .HasMaxLength(45);
            
        builder.Property(x => x.FirstSeen)
            .IsRequired();
            
        builder.Property(x => x.LastSeen)
            .IsRequired();
            
        builder.Property(x => x.IsTrusted)
            .IsRequired()
            .HasDefaultValue(false);
            
        builder.Property(x => x.DeviceName)
            .HasMaxLength(200);

        // Navigation property
        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(x => x.UserId)
            .HasDatabaseName("IX_UserDevices_UserId");
            
        builder.HasIndex(x => x.DeviceFingerprint)
            .HasDatabaseName("IX_UserDevices_DeviceFingerprint");
            
        builder.HasIndex(x => x.IsTrusted)
            .HasDatabaseName("IX_UserDevices_IsTrusted");
            
        builder.HasIndex(x => x.LastSeen)
            .HasDatabaseName("IX_UserDevices_LastSeen");
            
        // Unique constraint on UserId + DeviceFingerprint
        builder.HasIndex(x => new { x.UserId, x.DeviceFingerprint })
            .IsUnique()
            .HasDatabaseName("IX_UserDevices_UserId_DeviceFingerprint_Unique");
    }
} 