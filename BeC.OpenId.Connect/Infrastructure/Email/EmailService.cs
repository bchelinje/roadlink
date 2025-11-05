using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BeC.OpenId.Connect.Infrastructure.Email
{
    /// <summary>
    /// Configuration options for the email service
    /// </summary>
    public class EmailSettings
    {
        public string SmtpHost { get; set; } = "smtp.gmail.com";
        public int SmtpPort { get; set; } = 587;
        public bool EnableSsl { get; set; } = true;
        public string FromEmail { get; set; } = "";
        public string FromName { get; set; } = "BeC OpenId Connect";
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
        public int TimeoutSeconds { get; set; } = 30;
        public bool UseMockService { get; set; } = false; // For development/testing
    }

    /// <summary>
    /// Interface for email service operations
    /// </summary>
    public interface IEmailService
    {
        Task SendEmailConfirmationAsync(string email, string userName, string confirmationLink);
        Task SendPasswordResetAsync(string email, string userName, string resetLink);
        Task SendWelcomeEmailAsync(string email, string userName);
        Task SendPasswordChangedNotificationAsync(string email, string userName);
        Task SendCustomEmailAsync(string to, string subject, string body, bool isHtml = true);
    }

    /// <summary>
    /// Production email service implementation using SMTP
    /// </summary>
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _settings;
        private readonly ILogger<EmailService> _logger;
        private readonly IEmailTemplateService _templateService;

        public EmailService(
            IOptions<EmailSettings> settings,
            ILogger<EmailService> logger,
            IEmailTemplateService templateService)
        {
            _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _templateService = templateService ?? throw new ArgumentNullException(nameof(templateService));

            ValidateConfiguration();
        }

        private void ValidateConfiguration()
        {
            if (string.IsNullOrWhiteSpace(_settings.FromEmail))
            {
                throw new InvalidOperationException("FromEmail must be configured in EmailSettings");
            }

            if (string.IsNullOrWhiteSpace(_settings.SmtpHost))
            {
                throw new InvalidOperationException("SmtpHost must be configured in EmailSettings");
            }
        }

        public async Task SendEmailConfirmationAsync(string email, string userName, string confirmationLink)
        {
            _logger.LogInformation("Sending email confirmation to {Email}", email);

            var subject = "Confirm Your Email Address";
            var body = _templateService.RenderEmailConfirmation(userName, confirmationLink);

            await SendEmailAsync(email, subject, body);
        }

        public async Task SendPasswordResetAsync(string email, string userName, string resetLink)
        {
            _logger.LogInformation("Sending password reset email to {Email}", email);

            var subject = "Reset Your Password";
            var body = _templateService.RenderPasswordReset(userName, resetLink);

            await SendEmailAsync(email, subject, body);
        }

        public async Task SendWelcomeEmailAsync(string email, string userName)
        {
            _logger.LogInformation("Sending welcome email to {Email}", email);

            var subject = "Welcome to BeC OpenId Connect";
            var body = _templateService.RenderWelcome(userName);

            await SendEmailAsync(email, subject, body);
        }

        public async Task SendPasswordChangedNotificationAsync(string email, string userName)
        {
            _logger.LogInformation("Sending password changed notification to {Email}", email);

            var subject = "Password Changed Successfully";
            var body = _templateService.RenderPasswordChanged(userName);

            await SendEmailAsync(email, subject, body);
        }

        public async Task SendCustomEmailAsync(string to, string subject, string body, bool isHtml = true)
        {
            await SendEmailAsync(to, subject, body, isHtml);
        }

        private async Task SendEmailAsync(string to, string subject, string body, bool isHtml = true)
        {
            try
            {
                using var message = new MailMessage
                {
                    From = new MailAddress(_settings.FromEmail, _settings.FromName),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = isHtml
                };

                message.To.Add(new MailAddress(to));

                using var client = new SmtpClient(_settings.SmtpHost, _settings.SmtpPort)
                {
                    EnableSsl = _settings.EnableSsl,
                    Timeout = _settings.TimeoutSeconds * 1000,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(_settings.Username, _settings.Password)
                };

                await client.SendMailAsync(message);
                _logger.LogInformation("Email sent successfully to {Email}", to);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {Email}", to);
                throw new EmailSendException($"Failed to send email to {to}", ex);
            }
        }
    }

    /// <summary>
    /// Mock email service for development and testing
    /// </summary>
    public class MockEmailService : IEmailService
    {
        private readonly ILogger<MockEmailService> _logger;
        private readonly List<EmailLog> _sentEmails = new();

        public MockEmailService(ILogger<MockEmailService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public IReadOnlyList<EmailLog> SentEmails => _sentEmails.AsReadOnly();

        public Task SendEmailConfirmationAsync(string email, string userName, string confirmationLink)
        {
            LogEmail(email, "Email Confirmation", $"User: {userName}, Link: {confirmationLink}");
            return Task.CompletedTask;
        }

        public Task SendPasswordResetAsync(string email, string userName, string resetLink)
        {
            LogEmail(email, "Password Reset", $"User: {userName}, Link: {resetLink}");
            return Task.CompletedTask;
        }

        public Task SendWelcomeEmailAsync(string email, string userName)
        {
            LogEmail(email, "Welcome", $"User: {userName}");
            return Task.CompletedTask;
        }

        public Task SendPasswordChangedNotificationAsync(string email, string userName)
        {
            LogEmail(email, "Password Changed", $"User: {userName}");
            return Task.CompletedTask;
        }

        public Task SendCustomEmailAsync(string to, string subject, string body, bool isHtml = true)
        {
            LogEmail(to, subject, body);
            return Task.CompletedTask;
        }

        private void LogEmail(string to, string subject, string content)
        {
            var log = new EmailLog
            {
                To = to,
                Subject = subject,
                Content = content,
                SentAt = DateTime.UtcNow
            };

            _sentEmails.Add(log);
            _logger.LogInformation("[MOCK EMAIL] To: {To}, Subject: {Subject}", to, subject);
            _logger.LogDebug("[MOCK EMAIL] Content: {Content}", content);
        }

        public class EmailLog
        {
            public string To { get; set; } = "";
            public string Subject { get; set; } = "";
            public string Content { get; set; } = "";
            public DateTime SentAt { get; set; }
        }
    }

    /// <summary>
    /// Custom exception for email send failures
    /// </summary>
    public class EmailSendException : Exception
    {
        public EmailSendException(string message) : base(message) { }
        public EmailSendException(string message, Exception innerException) : base(message, innerException) { }
    }
}