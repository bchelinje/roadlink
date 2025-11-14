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
        // Original authentication-related emails
        Task SendEmailConfirmationAsync(string email, string userName, string confirmationLink);
        Task SendPasswordChangedNotificationAsync(string email, string userName);

        // Generic email sending
        Task SendEmailAsync(string to, string subject, string body, bool isHtml = true);
        Task SendEmailAsync(string to, string subject, string body, string? cc = null, string? bcc = null, bool isHtml = true);
        Task SendCustomEmailAsync(string to, string subject, string body, bool isHtml = true);

        // Business-specific emails
        Task SendJobConfirmationEmailAsync(string to, string customerName, string jobNumber, DateTime scheduledDate, string pickupAddress, string deliveryAddress);
        Task SendJobAssignmentEmailAsync(string to, string driverName, string jobNumber, DateTime scheduledDate, string pickupAddress, string deliveryAddress);
        Task SendJobCompletionEmailAsync(string to, string customerName, string jobNumber, decimal totalAmount);
        Task SendPaymentReceiptEmailAsync(string to, string customerName, string jobNumber, decimal amount, string paymentMethod, string transactionId);
        Task SendDocumentStatusEmailAsync(string to, string driverName, string documentType, string status, string? reason = null);
        Task SendPayoutNotificationEmailAsync(string to, string driverName, decimal amount, string period);
        Task SendWelcomeEmailAsync(string to, string userName, string role);
        Task SendPasswordResetEmailAsync(string to, string userName, string resetLink);
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

            await SendEmailInternalAsync(email, subject, body);
        }

        public async Task SendPasswordChangedNotificationAsync(string email, string userName)
        {
            _logger.LogInformation("Sending password changed notification to {Email}", email);

            var subject = "Password Changed Successfully";
            var body = _templateService.RenderPasswordChanged(userName);

            await SendEmailInternalAsync(email, subject, body);
        }

        public async Task SendCustomEmailAsync(string to, string subject, string body, bool isHtml = true)
        {
            await SendEmailInternalAsync(to, subject, body, isHtml);
        }

        public async Task SendEmailAsync(string to, string subject, string body, bool isHtml = true)
        {
            await SendEmailInternalAsync(to, subject, body, isHtml);
        }

        public async Task SendEmailAsync(string to, string subject, string body, string? cc = null, string? bcc = null, bool isHtml = true)
        {
            await SendEmailInternalAsync(to, subject, body, isHtml, cc, bcc);
        }

        public async Task SendJobConfirmationEmailAsync(string to, string customerName, string jobNumber, DateTime scheduledDate, string pickupAddress, string deliveryAddress)
        {
            var subject = $"Job Confirmation - {jobNumber}";
            var body = $@"
                <html>
                <body style='font-family: Arial, sans-serif;'>
                    <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                        <h2 style='color: #333;'>Job Confirmation</h2>
                        <p>Dear {customerName},</p>
                        <p>Your moving job has been confirmed. Here are the details:</p>

                        <div style='background-color: #f5f5f5; padding: 15px; border-radius: 5px; margin: 20px 0;'>
                            <p><strong>Job Number:</strong> {jobNumber}</p>
                            <p><strong>Scheduled Date:</strong> {scheduledDate:MMMM dd, yyyy 'at' hh:mm tt}</p>
                            <p><strong>Pickup Address:</strong> {pickupAddress}</p>
                            <p><strong>Delivery Address:</strong> {deliveryAddress}</p>
                        </div>

                        <p>You will receive updates as your job progresses.</p>
                        <p>Thank you for choosing BeC Moving Services!</p>

                        <hr style='margin: 30px 0; border: none; border-top: 1px solid #ddd;'>
                        <p style='font-size: 12px; color: #666;'>
                            This is an automated message. Please do not reply to this email.
                        </p>
                    </div>
                </body>
                </html>";

            await SendEmailAsync(to, subject, body);
        }

        public async Task SendJobAssignmentEmailAsync(string to, string driverName, string jobNumber, DateTime scheduledDate, string pickupAddress, string deliveryAddress)
        {
            var subject = $"New Job Assignment - {jobNumber}";
            var body = $@"
                <html>
                <body style='font-family: Arial, sans-serif;'>
                    <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                        <h2 style='color: #333;'>New Job Assignment</h2>
                        <p>Hi {driverName},</p>
                        <p>You have been assigned a new job:</p>

                        <div style='background-color: #f0f8ff; padding: 15px; border-radius: 5px; margin: 20px 0; border-left: 4px solid #007bff;'>
                            <p><strong>Job Number:</strong> {jobNumber}</p>
                            <p><strong>Scheduled Date:</strong> {scheduledDate:MMMM dd, yyyy 'at' hh:mm tt}</p>
                            <p><strong>Pickup Address:</strong> {pickupAddress}</p>
                            <p><strong>Delivery Address:</strong> {deliveryAddress}</p>
                        </div>

                        <p>Please review the job details in the driver portal and prepare accordingly.</p>
                        <p>Safe travels!</p>

                        <hr style='margin: 30px 0; border: none; border-top: 1px solid #ddd;'>
                        <p style='font-size: 12px; color: #666;'>
                            BeC Moving Services - Driver Support
                        </p>
                    </div>
                </body>
                </html>";

            await SendEmailAsync(to, subject, body);
        }

        public async Task SendJobCompletionEmailAsync(string to, string customerName, string jobNumber, decimal totalAmount)
        {
            var subject = $"Job Completed - {jobNumber}";
            var body = $@"
                <html>
                <body style='font-family: Arial, sans-serif;'>
                    <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                        <h2 style='color: #28a745;'>Job Completed Successfully!</h2>
                        <p>Dear {customerName},</p>
                        <p>Your moving job <strong>{jobNumber}</strong> has been completed successfully.</p>

                        <div style='background-color: #e8f5e9; padding: 15px; border-radius: 5px; margin: 20px 0;'>
                            <p><strong>Total Amount:</strong> ${totalAmount:F2}</p>
                        </div>

                        <p>We'd love to hear about your experience! Please consider leaving a review.</p>
                        <p>Thank you for choosing BeC Moving Services!</p>

                        <hr style='margin: 30px 0; border: none; border-top: 1px solid #ddd;'>
                        <p style='font-size: 12px; color: #666;'>
                            This is an automated message. Please do not reply to this email.
                        </p>
                    </div>
                </body>
                </html>";

            await SendEmailAsync(to, subject, body);
        }

        public async Task SendPaymentReceiptEmailAsync(string to, string customerName, string jobNumber, decimal amount, string paymentMethod, string transactionId)
        {
            var subject = $"Payment Receipt - {jobNumber}";
            var body = $@"
                <html>
                <body style='font-family: Arial, sans-serif;'>
                    <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                        <h2 style='color: #333;'>Payment Receipt</h2>
                        <p>Dear {customerName},</p>
                        <p>Thank you for your payment. Here are your receipt details:</p>

                        <div style='background-color: #f5f5f5; padding: 15px; border-radius: 5px; margin: 20px 0;'>
                            <p><strong>Job Number:</strong> {jobNumber}</p>
                            <p><strong>Amount Paid:</strong> ${amount:F2}</p>
                            <p><strong>Payment Method:</strong> {paymentMethod}</p>
                            <p><strong>Transaction ID:</strong> {transactionId}</p>
                            <p><strong>Date:</strong> {DateTime.UtcNow:MMMM dd, yyyy 'at' hh:mm tt} UTC</p>
                        </div>

                        <p>Please keep this receipt for your records.</p>
                        <p>Thank you for your business!</p>

                        <hr style='margin: 30px 0; border: none; border-top: 1px solid #ddd;'>
                        <p style='font-size: 12px; color: #666;'>
                            BeC Moving Services<br>
                            This is an automated receipt. Please do not reply to this email.
                        </p>
                    </div>
                </body>
                </html>";

            await SendEmailAsync(to, subject, body);
        }

        public async Task SendDocumentStatusEmailAsync(string to, string driverName, string documentType, string status, string? reason = null)
        {
            var subject = $"Document {status} - {documentType}";
            var statusColor = status.ToLower() == "verified" ? "#28a745" : "#dc3545";
            var statusMessage = status.ToLower() == "verified"
                ? "Your document has been verified successfully!"
                : $"Your document has been rejected. {reason}";

            var body = $@"
                <html>
                <body style='font-family: Arial, sans-serif;'>
                    <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                        <h2 style='color: {statusColor};'>Document {status}</h2>
                        <p>Hi {driverName},</p>

                        <div style='background-color: #f5f5f5; padding: 15px; border-radius: 5px; margin: 20px 0;'>
                            <p><strong>Document Type:</strong> {documentType.Replace("_", " ").ToUpper()}</p>
                            <p><strong>Status:</strong> <span style='color: {statusColor};'>{status.ToUpper()}</span></p>
                            {(string.IsNullOrEmpty(reason) ? "" : $"<p><strong>Reason:</strong> {reason}</p>")}
                        </div>

                        <p>{statusMessage}</p>
                        {(status.ToLower() == "rejected" ? "<p>Please upload a new document through your driver portal.</p>" : "")}

                        <hr style='margin: 30px 0; border: none; border-top: 1px solid #ddd;'>
                        <p style='font-size: 12px; color: #666;'>
                            BeC Moving Services - Document Verification Team
                        </p>
                    </div>
                </body>
                </html>";

            await SendEmailAsync(to, subject, body);
        }

        public async Task SendPayoutNotificationEmailAsync(string to, string driverName, decimal amount, string period)
        {
            var subject = $"Payout Notification - {period}";
            var body = $@"
                <html>
                <body style='font-family: Arial, sans-serif;'>
                    <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                        <h2 style='color: #28a745;'>Payout Processed</h2>
                        <p>Hi {driverName},</p>
                        <p>Your payout has been processed successfully!</p>

                        <div style='background-color: #e8f5e9; padding: 15px; border-radius: 5px; margin: 20px 0;'>
                            <p><strong>Period:</strong> {period}</p>
                            <p><strong>Amount:</strong> ${amount:F2}</p>
                            <p><strong>Processing Date:</strong> {DateTime.UtcNow:MMMM dd, yyyy}</p>
                        </div>

                        <p>The funds should arrive in your registered account within 2-3 business days.</p>
                        <p>Thank you for your hard work!</p>

                        <hr style='margin: 30px 0; border: none; border-top: 1px solid #ddd;'>
                        <p style='font-size: 12px; color: #666;'>
                            BeC Moving Services - Payroll Department
                        </p>
                    </div>
                </body>
                </html>";

            await SendEmailAsync(to, subject, body);
        }

        public async Task SendWelcomeEmailAsync(string to, string userName, string role)
        {
            var subject = "Welcome to BeC Moving Services!";
            var body = $@"
                <html>
                <body style='font-family: Arial, sans-serif;'>
                    <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                        <h2 style='color: #007bff;'>Welcome to BeC Moving Services!</h2>
                        <p>Hi {userName},</p>
                        <p>Welcome aboard! Your account has been created successfully.</p>

                        <div style='background-color: #f0f8ff; padding: 15px; border-radius: 5px; margin: 20px 0;'>
                            <p><strong>Account Type:</strong> {role}</p>
                        </div>

                        <p>You can now log in and start using our platform.</p>

                        {(role.ToLower() == "driver" ? @"
                        <p><strong>Next Steps:</strong></p>
                        <ul>
                            <li>Complete your profile</li>
                            <li>Upload required documents</li>
                            <li>Add your vehicle information</li>
                            <li>Start accepting jobs!</li>
                        </ul>
                        " : "")}

                        {(role.ToLower() == "customer" ? @"
                        <p><strong>Get Started:</strong></p>
                        <ul>
                            <li>Browse available services</li>
                            <li>Book your first moving job</li>
                            <li>Track your deliveries in real-time</li>
                        </ul>
                        " : "")}

                        <p>If you have any questions, feel free to contact our support team.</p>

                        <hr style='margin: 30px 0; border: none; border-top: 1px solid #ddd;'>
                        <p style='font-size: 12px; color: #666;'>
                            BeC Moving Services<br>
                            Professional Moving & Delivery Solutions
                        </p>
                    </div>
                </body>
                </html>";

            await SendEmailAsync(to, subject, body);
        }

        public async Task SendPasswordResetEmailAsync(string to, string userName, string resetLink)
        {
            var subject = "Password Reset Request";
            var body = $@"
                <html>
                <body style='font-family: Arial, sans-serif;'>
                    <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                        <h2 style='color: #333;'>Password Reset Request</h2>
                        <p>Hi {userName},</p>
                        <p>We received a request to reset your password. Click the button below to create a new password:</p>

                        <div style='text-align: center; margin: 30px 0;'>
                            <a href='{resetLink}' style='background-color: #007bff; color: white; padding: 12px 30px; text-decoration: none; border-radius: 5px; display: inline-block;'>
                                Reset Password
                            </a>
                        </div>

                        <p style='color: #666; font-size: 14px;'>
                            If you didn't request this password reset, please ignore this email or contact support if you have concerns.
                        </p>

                        <p style='color: #666; font-size: 14px;'>
                            This link will expire in 24 hours for security reasons.
                        </p>

                        <hr style='margin: 30px 0; border: none; border-top: 1px solid #ddd;'>
                        <p style='font-size: 12px; color: #666;'>
                            BeC Moving Services - Security Team<br>
                            This is an automated message. Please do not reply to this email.
                        </p>
                    </div>
                </body>
                </html>";

            await SendEmailAsync(to, subject, body);
        }

        private async Task SendEmailInternalAsync(string to, string subject, string body, bool isHtml = true, string? cc = null, string? bcc = null)
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

                if (!string.IsNullOrEmpty(cc))
                    message.CC.Add(new MailAddress(cc));

                if (!string.IsNullOrEmpty(bcc))
                    message.Bcc.Add(new MailAddress(bcc));

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

        public Task SendPasswordChangedNotificationAsync(string email, string userName)
        {
            LogEmail(email, "Password Changed", $"User: {userName}");
            return Task.CompletedTask;
        }

        public Task SendEmailAsync(string to, string subject, string body, bool isHtml = true)
        {
            LogEmail(to, subject, body);
            return Task.CompletedTask;
        }

        public Task SendEmailAsync(string to, string subject, string body, string? cc, string? bcc, bool isHtml = true)
        {
            LogEmail(to, subject, $"{body} [CC: {cc}, BCC: {bcc}]");
            return Task.CompletedTask;
        }

        public Task SendCustomEmailAsync(string to, string subject, string body, bool isHtml = true)
        {
            LogEmail(to, subject, body);
            return Task.CompletedTask;
        }

        public Task SendJobConfirmationEmailAsync(string to, string customerName, string jobNumber, DateTime scheduledDate, string pickupAddress, string deliveryAddress)
        {
            LogEmail(to, $"Job Confirmation - {jobNumber}", $"Customer: {customerName}, Date: {scheduledDate}, From: {pickupAddress}, To: {deliveryAddress}");
            return Task.CompletedTask;
        }

        public Task SendJobAssignmentEmailAsync(string to, string driverName, string jobNumber, DateTime scheduledDate, string pickupAddress, string deliveryAddress)
        {
            LogEmail(to, $"Job Assignment - {jobNumber}", $"Driver: {driverName}, Date: {scheduledDate}, From: {pickupAddress}, To: {deliveryAddress}");
            return Task.CompletedTask;
        }

        public Task SendJobCompletionEmailAsync(string to, string customerName, string jobNumber, decimal totalAmount)
        {
            LogEmail(to, $"Job Completed - {jobNumber}", $"Customer: {customerName}, Amount: ${totalAmount}");
            return Task.CompletedTask;
        }

        public Task SendPaymentReceiptEmailAsync(string to, string customerName, string jobNumber, decimal amount, string paymentMethod, string transactionId)
        {
            LogEmail(to, $"Payment Receipt - {jobNumber}", $"Customer: {customerName}, Amount: ${amount}, Method: {paymentMethod}, TxID: {transactionId}");
            return Task.CompletedTask;
        }

        public Task SendDocumentStatusEmailAsync(string to, string driverName, string documentType, string status, string? reason = null)
        {
            LogEmail(to, $"Document {status} - {documentType}", $"Driver: {driverName}, Reason: {reason ?? "N/A"}");
            return Task.CompletedTask;
        }

        public Task SendPayoutNotificationEmailAsync(string to, string driverName, decimal amount, string period)
        {
            LogEmail(to, $"Payout Notification - {period}", $"Driver: {driverName}, Amount: ${amount}");
            return Task.CompletedTask;
        }

        public Task SendWelcomeEmailAsync(string to, string userName, string role)
        {
            LogEmail(to, "Welcome to BeC Moving Services!", $"User: {userName}, Role: {role}");
            return Task.CompletedTask;
        }

        public Task SendPasswordResetEmailAsync(string to, string userName, string resetLink)
        {
            LogEmail(to, "Password Reset Request", $"User: {userName}, Link: {resetLink}");
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