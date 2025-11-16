using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using BeC.OpenId.Connect.Dto;
using BeC.OpenId.Connect.Features.Notifications.Dtos;
using BeC.OpenId.Connect.Features.Notifications.Services.Interfaces;
using BeC.OpenId.Connect.Features.Users.Dtos;

namespace BeC.OpenId.Connect.Features.Notifications.Services;

public class NotificationService : INotificationService
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<NotificationService> _logger;
    private readonly IConfiguration _configuration;

    public NotificationService(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        ILogger<NotificationService> logger,
        IConfiguration configuration)
    {
        _context = context;
        _userManager = userManager;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<Notification> SendNotificationAsync(
        string userId,
        string title,
        string message,
        string type,
        string? category = null,
        string? entityType = null,
        string? entityId = null,
        string? actionUrl = null,
        string? actionText = null,
        string priority = "normal",
        bool sendEmail = false,
        bool sendSms = false,
        bool sendPush = false,
        Dictionary<string, object>? data = null,
        DateTime? expiresAt = null)
    {
        // Get user preferences to check if notifications are enabled
        var preferences = await GetUserPreferencesAsync(userId);

        // Apply user preferences if they exist
        if (preferences != null)
        {
            sendEmail = sendEmail && await IsNotificationEnabledAsync(userId, category ?? type, "email");
            sendSms = sendSms && await IsNotificationEnabledAsync(userId, category ?? type, "sms");
            sendPush = sendPush && await IsNotificationEnabledAsync(userId, category ?? type, "push");

            // Check quiet hours
            if (preferences.EnableQuietHours && IsInQuietHours(preferences))
            {
                // Don't send SMS or Push during quiet hours, only email
                sendSms = false;
                sendPush = false;
            }
        }

        var notification = new Notification
        {
            UserId = userId,
            Title = title,
            Message = message,
            Type = type,
            Category = category,
            EntityType = entityType,
            EntityId = entityId,
            ActionUrl = actionUrl,
            ActionText = actionText,
            Priority = priority,
            SendEmail = sendEmail,
            SendSms = sendSms,
            SendPush = sendPush,
            Data = data != null ? JsonSerializer.Serialize(data) : null,
            ExpiresAt = expiresAt
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        // Send notifications asynchronously (fire and forget with proper error handling)
        _ = Task.Run(async () =>
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null) return;

                if (sendEmail && !string.IsNullOrEmpty(user.Email))
                {
                    var emailSent = await SendEmailAsync(user.Email, title, message);
                    if (emailSent)
                    {
                        notification.EmailSent = true;
                        notification.EmailSentAt = DateTime.UtcNow;
                    }
                }

                if (sendSms && !string.IsNullOrEmpty(user.PhoneNumber))
                {
                    var smsSent = await SendSmsAsync(user.PhoneNumber, $"{title}: {message}");
                    if (smsSent)
                    {
                        notification.SmsSent = true;
                        notification.SmsSentAt = DateTime.UtcNow;
                    }
                }

                if (sendPush)
                {
                    var pushSent = await SendPushNotificationAsync(userId, title, message,
                        data?.ToDictionary(k => k.Key, v => v.Value.ToString() ?? ""));
                    if (pushSent)
                    {
                        notification.PushSent = true;
                        notification.PushSentAt = DateTime.UtcNow;
                    }
                }

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending notification {NotificationId}", notification.Id);
            }
        });

        return notification;
    }

    public async Task SendJobNotificationAsync(
        string userId,
        Guid jobId,
        string jobNumber,
        string eventType,
        string message,
        Dictionary<string, object>? additionalData = null)
    {
        var data = new Dictionary<string, object>
        {
            { "jobId", jobId },
            { "jobNumber", jobNumber },
            { "eventType", eventType }
        };

        if (additionalData != null)
        {
            foreach (var kvp in additionalData)
            {
                data[kvp.Key] = kvp.Value;
            }
        }

        var title = eventType switch
        {
            "job_assigned" => $"New Job Assigned - {jobNumber}",
            "job_started" => $"Job Started - {jobNumber}",
            "job_completed" => $"Job Completed - {jobNumber}",
            "job_cancelled" => $"Job Cancelled - {jobNumber}",
            "job_rescheduled" => $"Job Rescheduled - {jobNumber}",
            "job_updated" => $"Job Updated - {jobNumber}",
            _ => $"Job Notification - {jobNumber}"
        };

        var priority = eventType == "job_cancelled" ? "high" : "normal";
        var sendSms = eventType == "job_cancelled" || eventType == "job_assigned";

        await SendNotificationAsync(
            userId,
            title,
            message,
            "job",
            eventType,
            "Job",
            jobId.ToString(),
            $"/jobs/{jobId}",
            "View Job",
            priority,
            sendEmail: true,
            sendSms: sendSms,
            sendPush: true,
            data: data
        );
    }

    public async Task SendPaymentNotificationAsync(
        string userId,
        Guid paymentId,
        string paymentNumber,
        string eventType,
        string message,
        decimal amount,
        string currency = "GBP")
    {
        var data = new Dictionary<string, object>
        {
            { "paymentId", paymentId },
            { "paymentNumber", paymentNumber },
            { "amount", amount },
            { "currency", currency },
            { "eventType", eventType }
        };

        var title = eventType switch
        {
            "payment_received" => $"Payment Received - {amount:C} {currency}",
            "payment_failed" => $"Payment Failed - {paymentNumber}",
            "payout_processed" => $"Payout Processed - {amount:C} {currency}",
            "refund_processed" => $"Refund Processed - {amount:C} {currency}",
            _ => $"Payment Notification - {paymentNumber}"
        };

        var priority = eventType == "payment_failed" ? "high" : "normal";

        await SendNotificationAsync(
            userId,
            title,
            message,
            "payment",
            eventType,
            "Payment",
            paymentId.ToString(),
            $"/payments/{paymentId}",
            "View Payment",
            priority,
            sendEmail: true,
            sendSms: true,
            sendPush: true,
            data: data
        );
    }

    public async Task<bool> SendEmailAsync(string email, string subject, string body, bool isHtml = true)
    {
        try
        {
            // TODO: Implement actual email sending using SendGrid, AWS SES, or similar
            // For now, just log the email
            _logger.LogInformation(
                "Email would be sent to {Email} with subject: {Subject}",
                email,
                subject
            );

            // Example integration with SendGrid:
            /*
            var apiKey = _configuration["SendGrid:ApiKey"];
            var client = new SendGridClient(apiKey);
            var from = new EmailAddress(_configuration["SendGrid:FromEmail"], _configuration["SendGrid:FromName"]);
            var to = new EmailAddress(email);
            var msg = MailHelper.CreateSingleEmail(from, to, subject, isHtml ? null : body, isHtml ? body : null);
            var response = await client.SendEmailAsync(msg);
            return response.StatusCode == System.Net.HttpStatusCode.OK || response.StatusCode == System.Net.HttpStatusCode.Accepted;
            */

            // Simulate successful send
            await Task.Delay(10);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending email to {Email}", email);
            return false;
        }
    }

    public async Task<bool> SendSmsAsync(string phoneNumber, string message)
    {
        try
        {
            // TODO: Implement actual SMS sending using Twilio, AWS SNS, or similar
            // For now, just log the SMS
            _logger.LogInformation(
                "SMS would be sent to {PhoneNumber} with message: {Message}",
                phoneNumber,
                message
            );

            // Example integration with Twilio:
            /*
            var accountSid = _configuration["Twilio:AccountSid"];
            var authToken = _configuration["Twilio:AuthToken"];
            TwilioClient.Init(accountSid, authToken);

            var messageResource = await MessageResource.CreateAsync(
                body: message,
                from: new PhoneNumber(_configuration["Twilio:PhoneNumber"]),
                to: new PhoneNumber(phoneNumber)
            );

            return messageResource.Status != MessageResource.StatusEnum.Failed;
            */

            // Simulate successful send
            await Task.Delay(10);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending SMS to {PhoneNumber}", phoneNumber);
            return false;
        }
    }

    public async Task<bool> SendPushNotificationAsync(string userId, string title, string body, Dictionary<string, string>? data = null)
    {
        try
        {
            // TODO: Implement actual push notification using Firebase Cloud Messaging, OneSignal, or similar
            // For now, just log the push notification
            _logger.LogInformation(
                "Push notification would be sent to user {UserId} with title: {Title}",
                userId,
                title
            );

            // Example integration with Firebase Cloud Messaging:
            /*
            var messaging = FirebaseMessaging.DefaultInstance;
            var message = new Message
            {
                Token = deviceToken, // You'd need to store device tokens for users
                Notification = new Notification
                {
                    Title = title,
                    Body = body
                },
                Data = data
            };

            var response = await messaging.SendAsync(message);
            return !string.IsNullOrEmpty(response);
            */

            // Simulate successful send
            await Task.Delay(10);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending push notification to user {UserId}", userId);
            return false;
        }
    }

    public async Task<NotificationPreferences?> GetUserPreferencesAsync(string userId)
    {
        var preferences = await _context.Set<NotificationPreferences>()
            .FirstOrDefaultAsync(p => p.UserId == userId);

        // Create default preferences if they don't exist
        if (preferences == null)
        {
            preferences = new NotificationPreferences
            {
                UserId = userId
            };
            _context.Set<NotificationPreferences>().Add(preferences);
            await _context.SaveChangesAsync();
        }

        return preferences;
    }

    public async Task<bool> IsNotificationEnabledAsync(string userId, string notificationType, string channel)
    {
        var preferences = await GetUserPreferencesAsync(userId);
        if (preferences == null) return true; // Default to enabled if no preferences

        // Check channel is enabled
        var channelEnabled = channel.ToLower() switch
        {
            "email" => preferences.EmailEnabled,
            "sms" => preferences.SmsEnabled,
            "push" => preferences.PushEnabled,
            _ => true
        };

        if (!channelEnabled) return false;

        // Check specific notification type
        return notificationType.ToLower() switch
        {
            "job_assigned" => channel.ToLower() switch
            {
                "email" => preferences.JobAssignedEmail,
                "sms" => preferences.JobAssignedSms,
                "push" => preferences.JobAssignedPush,
                _ => true
            },
            "job_completed" => channel.ToLower() switch
            {
                "email" => preferences.JobCompletedEmail,
                "sms" => preferences.JobCompletedSms,
                "push" => preferences.JobCompletedPush,
                _ => true
            },
            "job_cancelled" => channel.ToLower() switch
            {
                "email" => preferences.JobCancelledEmail,
                "sms" => preferences.JobCancelledSms,
                "push" => preferences.JobCancelledPush,
                _ => true
            },
            "job_rescheduled" => channel.ToLower() switch
            {
                "email" => preferences.JobRescheduledEmail,
                "sms" => preferences.JobRescheduledSms,
                "push" => preferences.JobRescheduledPush,
                _ => true
            },
            "payment_received" => channel.ToLower() switch
            {
                "email" => preferences.PaymentReceivedEmail,
                "sms" => preferences.PaymentReceivedSms,
                "push" => preferences.PaymentReceivedPush,
                _ => true
            },
            "payout_processed" => channel.ToLower() switch
            {
                "email" => preferences.PayoutProcessedEmail,
                "sms" => preferences.PayoutProcessedSms,
                "push" => preferences.PayoutProcessedPush,
                _ => true
            },
            "refund_processed" => channel.ToLower() switch
            {
                "email" => preferences.RefundProcessedEmail,
                "sms" => preferences.RefundProcessedSms,
                "push" => preferences.RefundProcessedPush,
                _ => true
            },
            "review_received" => channel.ToLower() switch
            {
                "email" => preferences.ReviewReceivedEmail,
                "sms" => preferences.ReviewReceivedSms,
                "push" => preferences.ReviewReceivedPush,
                _ => true
            },
            "review_response" => channel.ToLower() switch
            {
                "email" => preferences.ReviewResponseEmail,
                "sms" => preferences.ReviewResponseSms,
                "push" => preferences.ReviewResponsePush,
                _ => true
            },
            _ => true // Default to enabled for unknown types
        };
    }

    private bool IsInQuietHours(NotificationPreferences preferences)
    {
        if (!preferences.QuietHoursStart.HasValue || !preferences.QuietHoursEnd.HasValue)
            return false;

        var now = DateTime.UtcNow.TimeOfDay;
        var start = preferences.QuietHoursStart.Value;
        var end = preferences.QuietHoursEnd.Value;

        // Handle quiet hours that span midnight
        if (start < end)
        {
            return now >= start && now <= end;
        }
        else
        {
            return now >= start || now <= end;
        }
    }
}
