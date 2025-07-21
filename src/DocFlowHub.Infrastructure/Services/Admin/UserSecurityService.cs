using DocFlowHub.Core.Models.Admin;
using DocFlowHub.Core.Models.Common;
using DocFlowHub.Core.Services.Interfaces;
using DocFlowHub.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DocFlowHub.Infrastructure.Services.Admin;

public class UserSecurityService : IUserSecurityService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<UserSecurityService> _logger;

    public UserSecurityService(ApplicationDbContext context, ILogger<UserSecurityService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<UserSecurityStatus> GetUserSecurityStatusAsync(string userId)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return new UserSecurityStatus();

            var recentIps = await GetRecentIpAddressesAsync(userId);
            var recentDevices = await GetRecentDevicesAsync(userId);
            var failedLogins = await GetRecentFailedLoginCountAsync(userId);
            var securityAlerts = await GetUserSecurityAlertsAsync(userId, false);
            var securityScore = await CalculateSecurityScoreAsync(userId);

            return new UserSecurityStatus
            {
                IsEmailConfirmed = user.EmailConfirmed,
                IsTwoFactorEnabled = user.TwoFactorEnabled,
                IsLockedOut = user.LockoutEnd.HasValue && user.LockoutEnd > DateTimeOffset.UtcNow,
                LockoutEnd = user.LockoutEnd?.DateTime,
                FailedLoginAttempts = failedLogins,
                LastPasswordChange = null, // Would track in real implementation
                RequiresPasswordChange = false,
                RecentIpAddresses = recentIps,
                RecentDevices = recentDevices,
                SecurityScore = securityScore,
                SecurityAlerts = securityAlerts
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting security status for user {UserId}", userId);
            return new UserSecurityStatus();
        }
    }

    public async Task<int> CalculateSecurityScoreAsync(string userId)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return 0;

            var score = 100;

            // Email confirmation
            if (!user.EmailConfirmed) score -= 20;

            // Two-factor authentication
            if (!user.TwoFactorEnabled) score -= 15;

            // Recent failed logins
            var failedLogins = await GetRecentFailedLoginCountAsync(userId);
            if (failedLogins > 5) score -= 20;
            else if (failedLogins > 3) score -= 10;

            // Security events
            var unresolved = await _context.UserSecurityEvents
                .CountAsync(se => se.UserId == userId && !se.IsResolved);
            score -= unresolved * 5;

            // Account lockouts
            if (user.LockoutEnd.HasValue && user.LockoutEnd > DateTimeOffset.UtcNow)
                score -= 25;

            return Math.Max(0, score);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating security score for user {UserId}", userId);
            return 0;
        }
    }

    public async Task<List<SecurityAlert>> GetUserSecurityAlertsAsync(string userId, bool includeResolved = false)
    {
        try
        {
            var query = _context.UserSecurityEvents
                .Where(se => se.UserId == userId);

            if (!includeResolved)
                query = query.Where(se => !se.IsResolved);

            return await query
                .OrderByDescending(se => se.Timestamp)
                .Take(20)
                .Select(se => new SecurityAlert
                {
                    AlertType = se.EventType,
                    Message = se.Description,
                    Timestamp = se.Timestamp,
                    Severity = se.Severity,
                    IsResolved = se.IsResolved
                })
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting security alerts for user {UserId}", userId);
            return new List<SecurityAlert>();
        }
    }

    public async Task<ServiceResult> CheckForSuspiciousActivityAsync(string userId)
    {
        try
        {
            // Check for multiple failed logins
            var recentFailedLogins = await _context.UserLoginAttempts
                .Where(la => la.UserId == userId && !la.IsSuccessful && la.AttemptedAt >= DateTime.UtcNow.AddHours(-1))
                .CountAsync();

            if (recentFailedLogins >= 5)
            {
                await CreateSecurityEventAsync(userId, "SuspiciousLogin", 
                    $"Multiple failed login attempts detected ({recentFailedLogins})", "High");
            }

            // Check for unusual IP addresses
            var recentIps = await _context.UserLoginAttempts
                .Where(la => la.UserId == userId && la.IsSuccessful && la.AttemptedAt >= DateTime.UtcNow.AddDays(-1))
                .Select(la => la.IpAddress)
                .Distinct()
                .ToListAsync();

            var historicalIps = await _context.UserLoginAttempts
                .Where(la => la.UserId == userId && la.IsSuccessful && la.AttemptedAt < DateTime.UtcNow.AddDays(-1))
                .Select(la => la.IpAddress)
                .Distinct()
                .ToListAsync();

            var newIps = recentIps.Except(historicalIps).ToList();
            if (newIps.Count > 0)
            {
                await CreateSecurityEventAsync(userId, "UnusualLocation", 
                    $"Login from {newIps.Count} new IP address(es)", "Medium");
            }

            return ServiceResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking suspicious activity for user {UserId}", userId);
            return ServiceResult.Failure("Error checking suspicious activity");
        }
    }

    public async Task<ServiceResult> DetectUnusualLoginPatternsAsync(string userId, string ipAddress, string userAgent)
    {
        try
        {
            // Check if this is a new device
            var deviceFingerprint = GenerateDeviceFingerprint(userAgent, ipAddress);
            var knownDevice = await _context.UserDevices
                .AnyAsync(d => d.UserId == userId && d.DeviceFingerprint == deviceFingerprint);

            if (!knownDevice)
            {
                await CreateSecurityEventAsync(userId, "NewDevice", 
                    $"Login from new device: {userAgent}", "Medium");
            }

            // Check for rapid login attempts
            var recentLogins = await _context.UserLoginAttempts
                .Where(la => la.UserId == userId && la.AttemptedAt >= DateTime.UtcNow.AddMinutes(-5))
                .CountAsync();

            if (recentLogins > 10)
            {
                await CreateSecurityEventAsync(userId, "RapidLogins", 
                    $"Rapid login attempts detected ({recentLogins})", "High");
            }

            return ServiceResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error detecting unusual login patterns for user {UserId}", userId);
            return ServiceResult.Failure("Error detecting login patterns");
        }
    }

    public async Task<List<UserSecurityEvent>> GetSecurityEventsAsync(string? userId = null, int count = 100, string? severity = null)
    {
        try
        {
            var query = _context.UserSecurityEvents.AsQueryable();

            if (!string.IsNullOrEmpty(userId))
                query = query.Where(se => se.UserId == userId);

            if (!string.IsNullOrEmpty(severity))
                query = query.Where(se => se.Severity == severity);

            return await query
                .OrderByDescending(se => se.Timestamp)
                .Take(count)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting security events");
            return new List<UserSecurityEvent>();
        }
    }

    public async Task<ServiceResult> ResolveSecurityEventAsync(int eventId, string adminId)
    {
        try
        {
            var securityEvent = await _context.UserSecurityEvents.FindAsync(eventId);
            if (securityEvent == null)
                return ServiceResult.Failure("Security event not found");

            securityEvent.IsResolved = true;
            securityEvent.ResolvedAt = DateTime.UtcNow;
            securityEvent.ResolvedBy = adminId;

            await _context.SaveChangesAsync();

            return ServiceResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resolving security event {EventId}", eventId);
            return ServiceResult.Failure("Error resolving security event");
        }
    }

    public async Task<ServiceResult> ValidatePasswordStrengthAsync(string password)
    {
        try
        {
            var issues = new List<string>();

            if (password.Length < 8)
                issues.Add("Password must be at least 8 characters long");

            if (!password.Any(char.IsUpper))
                issues.Add("Password must contain at least one uppercase letter");

            if (!password.Any(char.IsLower))
                issues.Add("Password must contain at least one lowercase letter");

            if (!password.Any(char.IsDigit))
                issues.Add("Password must contain at least one number");

            if (!password.Any(c => "!@#$%^&*()_+-=[]{}|;:,.<>?".Contains(c)))
                issues.Add("Password must contain at least one special character");

            return issues.Count == 0 
                ? ServiceResult.Success() 
                : ServiceResult.Failure(string.Join("; ", issues));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating password strength");
            return ServiceResult.Failure("Error validating password");
        }
    }

    public async Task<ServiceResult> CheckPasswordHistoryAsync(string userId, string newPassword)
    {
        // In real implementation, would check against hashed password history
        await Task.CompletedTask;
        return ServiceResult.Success(); // Placeholder
    }

    public async Task<bool> IsPasswordExpiredAsync(string userId)
    {
        // In real implementation, would check password age against policy
        await Task.CompletedTask;
        return false; // Placeholder
    }

    public async Task<ServiceResult> ForcePasswordChangeAsync(string userId, string adminId)
    {
        try
        {
            // In real implementation, would set flag requiring password change on next login
            await CreateSecurityEventAsync(userId, "PasswordChangeForced", 
                $"Password change forced by admin {adminId}", "Medium");

            return ServiceResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error forcing password change for user {UserId}", userId);
            return ServiceResult.Failure("Error forcing password change");
        }
    }

    // Helper methods
    private async Task<List<string>> GetRecentIpAddressesAsync(string userId)
    {
        return await _context.UserLoginAttempts
            .Where(la => la.UserId == userId && la.AttemptedAt >= DateTime.UtcNow.AddDays(-30))
            .Select(la => la.IpAddress)
            .Distinct()
            .Take(10)
            .ToListAsync();
    }

    private async Task<List<string>> GetRecentDevicesAsync(string userId)
    {
        return await _context.UserDevices
            .Where(d => d.UserId == userId)
            .OrderByDescending(d => d.LastSeen)
            .Take(5)
            .Select(d => $"{d.DeviceType} - {d.Browser}")
            .ToListAsync();
    }

    private async Task<int> GetRecentFailedLoginCountAsync(string userId)
    {
        return await _context.UserLoginAttempts
            .CountAsync(la => la.UserId == userId && !la.IsSuccessful && la.AttemptedAt >= DateTime.UtcNow.AddDays(-7));
    }

    private async Task CreateSecurityEventAsync(string userId, string eventType, string description, string severity)
    {
        try
        {
            var securityEvent = new UserSecurityEvent
            {
                UserId = userId,
                EventType = eventType,
                Description = description,
                Severity = severity,
                IpAddress = "127.0.0.1", // Would get from HttpContext in real implementation
                Timestamp = DateTime.UtcNow
            };

            _context.UserSecurityEvents.Add(securityEvent);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating security event");
        }
    }

    private static string GenerateDeviceFingerprint(string userAgent, string ipAddress)
    {
        // Simple fingerprint generation - would be more sophisticated in real implementation
        return $"{userAgent}_{ipAddress}".GetHashCode().ToString();
    }
} 