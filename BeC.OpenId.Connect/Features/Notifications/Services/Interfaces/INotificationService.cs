using BeC.OpenId.Connect.Features.Notifications.Dtos;

namespace BeC.OpenId.Connect.Features.Notifications.Services.Interfaces;

/// <summary>
/// Service for managing and sending notifications across multiple channels
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Send a notification to a specific user
    /// </summary>
    Task<Notification> SendNotificationAsync(
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
        DateTime? expiresAt = null);

    /// <summary>
    /// Send job-related notification
    /// </summary>
    Task SendJobNotificationAsync(
        string userId,
        Guid jobId,
        string jobNumber,
        string eventType,
        string message,
        Dictionary<string, object>? additionalData = null);

    /// <summary>
    /// Send payment-related notification
    /// </summary>
    Task SendPaymentNotificationAsync(
        string userId,
        Guid paymentId,
        string paymentNumber,
        string eventType,
        string message,
        decimal amount,
        string currency = "GBP");

    /// <summary>
    /// Send email notification
    /// </summary>
    Task<bool> SendEmailAsync(string email, string subject, string body, bool isHtml = true);

    /// <summary>
    /// Send SMS notification
    /// </summary>
    Task<bool> SendSmsAsync(string phoneNumber, string message);

    /// <summary>
    /// Send push notification
    /// </summary>
    Task<bool> SendPushNotificationAsync(string userId, string title, string body, Dictionary<string, string>? data = null);

    /// <summary>
    /// Get user notification preferences
    /// </summary>
    Task<NotificationPreferences?> GetUserPreferencesAsync(string userId);

    /// <summary>
    /// Check if user has notifications enabled for a specific type
    /// </summary>
    Task<bool> IsNotificationEnabledAsync(string userId, string notificationType, string channel);
}
