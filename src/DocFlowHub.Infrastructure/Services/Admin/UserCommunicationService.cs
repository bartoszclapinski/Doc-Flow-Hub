using DocFlowHub.Core.Models.Admin;
using DocFlowHub.Core.Models.Common;
using DocFlowHub.Core.Services.Interfaces;
using DocFlowHub.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DocFlowHub.Infrastructure.Services.Admin;

public class UserCommunicationService : IUserCommunicationService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<UserCommunicationService> _logger;

    public UserCommunicationService(ApplicationDbContext context, ILogger<UserCommunicationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ServiceResult> SendMessageToUserAsync(string userId, string adminId, string subject, string message, CommunicationType type = CommunicationType.AdminMessage)
    {
        try
        {
            var communication = new UserCommunicationEntity
            {
                UserId = userId,
                AdminId = adminId,
                Subject = subject,
                Message = message,
                Type = type.ToString(),
                SentAt = DateTime.UtcNow,
                IsRead = false
            };

            _context.UserCommunications.Add(communication);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Message sent to user {UserId} by admin {AdminId} with subject '{Subject}'", userId, adminId, subject);
            return ServiceResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending message to user {UserId}", userId);
            return ServiceResult.Failure("Error sending message");
        }
    }

    public async Task<ServiceResult> SendBulkMessageAsync(List<string> userIds, string adminId, string subject, string message)
    {
        try
        {
            var communications = userIds.Select(userId => new UserCommunicationEntity
            {
                UserId = userId,
                AdminId = adminId,
                Subject = subject,
                Message = message,
                Type = CommunicationType.BulkMessage.ToString(),
                SentAt = DateTime.UtcNow,
                IsRead = false
            }).ToList();

            _context.UserCommunications.AddRange(communications);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Bulk message sent to {UserCount} users by admin {AdminId} with subject '{Subject}'", userIds.Count, adminId, subject);
            return ServiceResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending bulk message to {UserCount} users", userIds.Count);
            return ServiceResult.Failure("Error sending bulk message");
        }
    }

            public async Task<PagedResult<UserCommunicationEntity>> GetUserCommunicationsAsync(string userId, int page = 1, int pageSize = 20)
        {
            try
            {
                var query = _context.UserCommunications
                    .Where(c => c.UserId == userId)
                    .Include(c => c.Admin)
                    .OrderByDescending(c => c.SentAt);

                var totalCount = await query.CountAsync();
                var communications = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                return new PagedResult<UserCommunicationEntity>
                {
                    Items = communications,
                    TotalItems = totalCount,
                    PageNumber = page,
                    PageSize = pageSize
                };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting communications for user {UserId}", userId);
            return new PagedResult<UserCommunicationEntity>();
        }
    }

            public async Task<PagedResult<UserCommunicationEntity>> GetAdminCommunicationsAsync(string adminId, int page = 1, int pageSize = 50)
        {
            try
            {
                var query = _context.UserCommunications
                    .Where(c => c.AdminId == adminId)
                    .Include(c => c.User)
                    .OrderByDescending(c => c.SentAt);

                var totalCount = await query.CountAsync();
                var communications = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                return new PagedResult<UserCommunicationEntity>
                {
                    Items = communications,
                    TotalItems = totalCount,
                    PageNumber = page,
                    PageSize = pageSize
                };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting communications for admin {AdminId}", adminId);
            return new PagedResult<UserCommunicationEntity>();
        }
    }

    public async Task<ServiceResult> SendWelcomeMessageAsync(string userId)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return ServiceResult.Failure("User not found");

            var subject = "Welcome to DocFlowHub!";
            var message = $@"
                <h3>Welcome to DocFlowHub, {user.FirstName}!</h3>
                <p>We're excited to have you join our document management platform.</p>
                <p>Here are some quick tips to get started:</p>
                <ul>
                    <li>Upload your first document using the Upload button</li>
                    <li>Create projects to organize your documents</li>
                    <li>Invite team members to collaborate</li>
                    <li>Use our AI features to summarize and compare documents</li>
                </ul>
                <p>If you have any questions, please don't hesitate to contact our support team.</p>
                <p>Happy document managing!</p>
                <p><em>The DocFlowHub Team</em></p>
            ";

            return await SendSystemMessageAsync(userId, subject, message, CommunicationType.Welcome);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending welcome message to user {UserId}", userId);
            return ServiceResult.Failure("Error sending welcome message");
        }
    }

    public async Task<ServiceResult> SendAccountUpdateNotificationAsync(string userId, string updateType)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return ServiceResult.Failure("User not found");

            var subject = "Account Update Notification";
            var message = $@"
                <h3>Account Update</h3>
                <p>Hello {user.FirstName},</p>
                <p>This is to notify you that your account has been updated:</p>
                <p><strong>Update Type:</strong> {updateType}</p>
                <p><strong>Updated At:</strong> {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</p>
                <p>If you did not make this change or have concerns about your account security, please contact support immediately.</p>
                <p><em>The DocFlowHub Team</em></p>
            ";

            return await SendSystemMessageAsync(userId, subject, message, CommunicationType.AccountUpdate);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending account update notification to user {UserId}", userId);
            return ServiceResult.Failure("Error sending notification");
        }
    }

    public async Task<ServiceResult> SendSecurityAlertAsync(string userId, string alertMessage)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return ServiceResult.Failure("User not found");

            var subject = "🔒 Security Alert - DocFlowHub";
            var message = $@"
                <h3 style='color: #dc3545;'>Security Alert</h3>
                <p>Hello {user.FirstName},</p>
                <p>We detected unusual activity on your DocFlowHub account:</p>
                <div style='background-color: #f8d7da; border: 1px solid #f5c6cb; padding: 10px; border-radius: 5px; margin: 10px 0;'>
                    <strong>Alert:</strong> {alertMessage}
                </div>
                <p><strong>Time:</strong> {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</p>
                <p><strong>What should you do?</strong></p>
                <ul>
                    <li>Review your recent account activity</li>
                    <li>Change your password if you suspect unauthorized access</li>
                    <li>Enable two-factor authentication for added security</li>
                    <li>Contact support if you have concerns</li>
                </ul>
                <p>If this activity was performed by you, you can safely ignore this message.</p>
                <p><em>The DocFlowHub Security Team</em></p>
            ";

            return await SendSystemMessageAsync(userId, subject, message, CommunicationType.SecurityAlert);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending security alert to user {UserId}", userId);
            return ServiceResult.Failure("Error sending security alert");
        }
    }

    public async Task<ServiceResult> SendPasswordResetNotificationAsync(string userId)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return ServiceResult.Failure("User not found");

            var subject = "Password Reset Request - DocFlowHub";
            var message = $@"
                <h3>Password Reset Request</h3>
                <p>Hello {user.FirstName},</p>
                <p>A password reset has been initiated for your DocFlowHub account.</p>
                <p><strong>Requested At:</strong> {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</p>
                <p>A password reset link has been sent to your email address. Please check your email and follow the instructions to reset your password.</p>
                <p><strong>Important:</strong> If you did not request a password reset, please contact support immediately as this may indicate unauthorized access to your account.</p>
                <p>The reset link will expire in 24 hours for security reasons.</p>
                <p><em>The DocFlowHub Team</em></p>
            ";

            return await SendSystemMessageAsync(userId, subject, message, CommunicationType.PasswordReset);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending password reset notification to user {UserId}", userId);
            return ServiceResult.Failure("Error sending password reset notification");
        }
    }

    public async Task<ServiceResult> MarkMessageAsReadAsync(int communicationId, string userId)
    {
        try
        {
            var communication = await _context.UserCommunications
                .FirstOrDefaultAsync(c => c.Id == communicationId && c.UserId == userId);

            if (communication == null)
                return ServiceResult.Failure("Communication not found");

            if (!communication.IsRead)
            {
                communication.IsRead = true;
                communication.ReadAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            return ServiceResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking communication {CommunicationId} as read", communicationId);
            return ServiceResult.Failure("Error marking message as read");
        }
    }

    public async Task<ServiceResult> DeleteCommunicationAsync(int communicationId, string adminId)
    {
        try
        {
            var communication = await _context.UserCommunications
                .FirstOrDefaultAsync(c => c.Id == communicationId && c.AdminId == adminId);

            if (communication == null)
                return ServiceResult.Failure("Communication not found");

            _context.UserCommunications.Remove(communication);
            await _context.SaveChangesAsync();

            return ServiceResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting communication {CommunicationId}", communicationId);
            return ServiceResult.Failure("Error deleting communication");
        }
    }

    public async Task<int> GetUnreadMessageCountAsync(string userId)
    {
        try
        {
            return await _context.UserCommunications
                .CountAsync(c => c.UserId == userId && !c.IsRead);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting unread message count for user {UserId}", userId);
            return 0;
        }
    }

    public async Task<List<UserCommunicationEntity>> GetRecentCommunicationsAsync(string userId, int count = 5)
    {
        try
        {
            return await _context.UserCommunications
                .Where(c => c.UserId == userId)
                .Include(c => c.Admin)
                .OrderByDescending(c => c.SentAt)
                .Take(count)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recent communications for user {UserId}", userId);
            return new List<UserCommunicationEntity>();
        }
    }

    // Helper method for sending system messages
    private async Task<ServiceResult> SendSystemMessageAsync(string userId, string subject, string message, CommunicationType type)
    {
        var systemAdminId = "system"; // In real implementation, would be a system account

        var communication = new UserCommunicationEntity
        {
            UserId = userId,
            AdminId = systemAdminId,
            Subject = subject,
            Message = message,
            Type = type.ToString(),
            SentAt = DateTime.UtcNow,
            IsRead = false
        };

        _context.UserCommunications.Add(communication);
        await _context.SaveChangesAsync();

        return ServiceResult.Success();
    }
} 