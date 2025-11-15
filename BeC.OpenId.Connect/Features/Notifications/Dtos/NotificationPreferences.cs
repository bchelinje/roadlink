using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BeC.OpenId.Connect.Features.Notifications.Dtos;

/// <summary>
/// User notification preferences
/// </summary>
[Table("NotificationPreferences")]
public class NotificationPreferences
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public required string UserId { get; set; } // FK to AspNetUsers

    // Channel preferences
    public bool EmailEnabled { get; set; } = true;
    public bool SmsEnabled { get; set; } = false;
    public bool PushEnabled { get; set; } = true;

    // Job notifications
    public bool JobAssignedEmail { get; set; } = true;
    public bool JobAssignedSms { get; set; } = false;
    public bool JobAssignedPush { get; set; } = true;

    public bool JobCompletedEmail { get; set; } = true;
    public bool JobCompletedSms { get; set; } = false;
    public bool JobCompletedPush { get; set; } = true;

    public bool JobCancelledEmail { get; set; } = true;
    public bool JobCancelledSms { get; set; } = true;
    public bool JobCancelledPush { get; set; } = true;

    public bool JobRescheduledEmail { get; set; } = true;
    public bool JobRescheduledSms { get; set; } = false;
    public bool JobRescheduledPush { get; set; } = true;

    // Payment notifications
    public bool PaymentReceivedEmail { get; set; } = true;
    public bool PaymentReceivedSms { get; set; } = false;
    public bool PaymentReceivedPush { get; set; } = true;

    public bool PayoutProcessedEmail { get; set; } = true;
    public bool PayoutProcessedSms { get; set; } = true;
    public bool PayoutProcessedPush { get; set; } = true;

    public bool RefundProcessedEmail { get; set; } = true;
    public bool RefundProcessedSms { get; set; } = false;
    public bool RefundProcessedPush { get; set; } = true;

    // Review notifications
    public bool ReviewReceivedEmail { get; set; } = true;
    public bool ReviewReceivedSms { get; set; } = false;
    public bool ReviewReceivedPush { get; set; } = true;

    public bool ReviewResponseEmail { get; set; } = true;
    public bool ReviewResponseSms { get; set; } = false;
    public bool ReviewResponsePush { get; set; } = true;

    // System notifications
    public bool SystemAlertsEmail { get; set; } = true;
    public bool SystemAlertsSms { get; set; } = false;
    public bool SystemAlertsPush { get; set; } = true;

    public bool AccountUpdatesEmail { get; set; } = true;
    public bool AccountUpdatesSms { get; set; } = false;
    public bool AccountUpdatesPush { get; set; } = false;

    // Marketing and promotions
    public bool PromotionalEmail { get; set; } = true;
    public bool PromotionalSms { get; set; } = false;
    public bool PromotionalPush { get; set; } = false;

    // Quiet hours
    public bool EnableQuietHours { get; set; } = false;
    public TimeSpan? QuietHoursStart { get; set; } // e.g., 22:00
    public TimeSpan? QuietHoursEnd { get; set; }   // e.g., 08:00

    // Digest settings
    public bool EnableEmailDigest { get; set; } = false;
    public string? DigestFrequency { get; set; } // daily, weekly

    // Audit
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
