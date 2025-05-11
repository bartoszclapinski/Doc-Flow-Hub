using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Logging;

namespace DocFlowHub.Web.Services
{
    public class EmailSender : IEmailSender
    {
        private readonly ILogger<EmailSender> _logger;

        public EmailSender(ILogger<EmailSender> logger)
        {
            _logger = logger;
        }

        public Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            _logger.LogInformation($"Email to {email} with subject {subject} would be sent here.");
            _logger.LogInformation($"Message content: {htmlMessage}");
            return Task.CompletedTask;
        }
    }
} 