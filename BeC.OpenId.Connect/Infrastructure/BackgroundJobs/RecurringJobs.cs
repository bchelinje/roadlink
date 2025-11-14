using Hangfire;
using Microsoft.EntityFrameworkCore;
using BeC.OpenId.Connect.Dto;
using BeC.OpenId.Connect.Features.Notifications.Services.Interfaces;
using BeC.OpenId.Connect.Infrastructure.Email;

namespace BeC.OpenId.Connect.Infrastructure.BackgroundJobs;

/// <summary>
/// Recurring background jobs for the platform
/// </summary>
public static class RecurringJobs
{
    /// <summary>
    /// Register all recurring jobs
    /// </summary>
    public static void RegisterRecurringJobs()
    {
        // Run daily at 9 AM UTC
        RecurringJob.AddOrUpdate<DocumentExpiryReminderJob>(
            "document-expiry-reminder",
            job => job.SendExpiryRemindersAsync(),
            "0 9 * * *"); // Daily at 9:00 AM UTC

        // Run every hour
        RecurringJob.AddOrUpdate<NotificationCleanupJob>(
            "notification-cleanup",
            job => job.CleanupExpiredNotificationsAsync(),
            "0 * * * *"); // Every hour

        // Run weekly on Monday at 8 AM UTC
        RecurringJob.AddOrUpdate<WeeklyDriverPayoutJob>(
            "weekly-driver-payout",
            job => job.ProcessWeeklyPayoutsAsync(),
            "0 8 * * 1"); // Monday at 8:00 AM UTC

        // Run daily at midnight UTC
        RecurringJob.AddOrUpdate<DailyReportJob>(
            "daily-report",
            job => job.GenerateDailyReportAsync(),
            "0 0 * * *"); // Daily at midnight UTC
    }
}

/// <summary>
/// Send reminders for expiring documents
/// </summary>
public class DocumentExpiryReminderJob
{
    private readonly ApplicationDbContext _context;
    private readonly IEmailService _emailService;
    private readonly IRealtimeNotificationService _realtimeNotificationService;
    private readonly ILogger<DocumentExpiryReminderJob> _logger;

    public DocumentExpiryReminderJob(
        ApplicationDbContext context,
        IEmailService emailService,
        IRealtimeNotificationService realtimeNotificationService,
        ILogger<DocumentExpiryReminderJob> logger)
    {
        _context = context;
        _emailService = emailService;
        _realtimeNotificationService = realtimeNotificationService;
        _logger = logger;
    }

    public async Task SendExpiryRemindersAsync()
    {
        _logger.LogInformation("Starting document expiry reminder job");

        var warningThreshold = DateTime.UtcNow.AddDays(30); // Warn 30 days before expiry

        var expiringDocuments = await _context.DriverDocuments
            .Include(d => d.Driver)
            .Where(d => d.ExpiryDate.HasValue &&
                       d.ExpiryDate.Value <= warningThreshold &&
                       d.ExpiryDate.Value >= DateTime.UtcNow &&
                       d.Status == "verified")
            .ToListAsync();

        foreach (var doc in expiringDocuments)
        {
            try
            {
                var daysUntilExpiry = (doc.ExpiryDate!.Value - DateTime.UtcNow).Days;

                // Send email reminder
                await _emailService.SendEmailAsync(
                    doc.Driver.Email,
                    $"Document Expiring Soon - {doc.Type}",
                    $@"
                        <html>
                        <body style='font-family: Arial, sans-serif;'>
                            <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                                <h2 style='color: #ff9800;'>Document Expiring Soon</h2>
                                <p>Hi {doc.Driver.FirstName},</p>
                                <p>Your <strong>{doc.Type.Replace("_", " ").ToUpper()}</strong> document will expire in <strong>{daysUntilExpiry} days</strong>.</p>
                                <div style='background-color: #fff3e0; padding: 15px; border-radius: 5px; margin: 20px 0; border-left: 4px solid #ff9800;'>
                                    <p><strong>Expiry Date:</strong> {doc.ExpiryDate.Value:MMMM dd, yyyy}</p>
                                </div>
                                <p>Please upload a renewed version to avoid service interruption.</p>
                                <hr style='margin: 30px 0; border: none; border-top: 1px solid #ddd;'>
                                <p style='font-size: 12px; color: #666;'>
                                    BeC Moving Services - Document Management
                                </p>
                            </div>
                        </body>
                        </html>"
                );

                // Send real-time notification
                await _realtimeNotificationService.SendToUserAsync(
                    doc.Driver.UserId,
                    "document_expiry_warning",
                    new
                    {
                        documentId = doc.Id,
                        documentType = doc.Type,
                        expiryDate = doc.ExpiryDate,
                        daysUntilExpiry = daysUntilExpiry
                    });

                _logger.LogInformation("Sent expiry reminder for document {DocumentId} to driver {DriverId}",
                    doc.Id, doc.DriverId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending expiry reminder for document {DocumentId}", doc.Id);
            }
        }

        _logger.LogInformation("Document expiry reminder job completed. Processed {Count} documents", expiringDocuments.Count);
    }
}

/// <summary>
/// Cleanup old and expired notifications
/// </summary>
public class NotificationCleanupJob
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<NotificationCleanupJob> _logger;

    public NotificationCleanupJob(
        ApplicationDbContext context,
        ILogger<NotificationCleanupJob> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task CleanupExpiredNotificationsAsync()
    {
        _logger.LogInformation("Starting notification cleanup job");

        // Delete notifications older than 30 days or expired
        var cutoffDate = DateTime.UtcNow.AddDays(-30);

        var expiredNotifications = await _context.Notifications
            .Where(n => n.CreatedAt < cutoffDate || (n.ExpiresAt.HasValue && n.ExpiresAt.Value < DateTime.UtcNow))
            .ToListAsync();

        _context.Notifications.RemoveRange(expiredNotifications);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Notification cleanup job completed. Removed {Count} notifications", expiredNotifications.Count);
    }
}

/// <summary>
/// Process weekly payouts for drivers
/// </summary>
public class WeeklyDriverPayoutJob
{
    private readonly ApplicationDbContext _context;
    private readonly IEmailService _emailService;
    private readonly IRealtimeNotificationService _realtimeNotificationService;
    private readonly ILogger<WeeklyDriverPayoutJob> _logger;

    public WeeklyDriverPayoutJob(
        ApplicationDbContext context,
        IEmailService emailService,
        IRealtimeNotificationService realtimeNotificationService,
        ILogger<WeeklyDriverPayoutJob> logger)
    {
        _context = context;
        _emailService = emailService;
        _realtimeNotificationService = realtimeNotificationService;
        _logger = logger;
    }

    public async Task ProcessWeeklyPayoutsAsync()
    {
        _logger.LogInformation("Starting weekly driver payout job");

        var startDate = DateTime.UtcNow.AddDays(-7);
        var endDate = DateTime.UtcNow;

        // Get all pending payouts for the past week
        var pendingPayouts = await _context.Payouts
            .Include(p => p.Driver)
            .Where(p => p.Status == "pending" && p.CreatedAt >= startDate)
            .ToListAsync();

        foreach (var payout in pendingPayouts)
        {
            try
            {
                // Mark as processing
                payout.Status = "processing";
                await _context.SaveChangesAsync();

                // Here you would integrate with payment provider (Stripe, PayPal, etc.)
                // For now, we'll just mark as completed
                await Task.Delay(100); // Simulate API call

                payout.Status = "completed";
                payout.ProcessedDate = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                // Send notification email
                var period = $"{startDate:MMM dd} - {endDate:MMM dd, yyyy}";
                await _emailService.SendPayoutNotificationEmailAsync(
                    payout.Driver.Email,
                    $"{payout.Driver.FirstName} {payout.Driver.LastName}",
                    payout.Amount,
                    period);

                // Send real-time notification
                await _realtimeNotificationService.SendToUserAsync(
                    payout.Driver.UserId,
                    "payout_completed",
                    new
                    {
                        payoutId = payout.Id,
                        amount = payout.Amount,
                        period = period
                    });

                _logger.LogInformation("Processed payout {PayoutId} for driver {DriverId} - Amount: {Amount}",
                    payout.Id, payout.DriverId, payout.Amount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing payout {PayoutId}", payout.Id);
                payout.Status = "failed";
                payout.ProcessedDate = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }

        _logger.LogInformation("Weekly driver payout job completed. Processed {Count} payouts", pendingPayouts.Count);
    }
}

/// <summary>
/// Generate daily platform report
/// </summary>
public class DailyReportJob
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DailyReportJob> _logger;

    public DailyReportJob(
        ApplicationDbContext context,
        ILogger<DailyReportJob> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task GenerateDailyReportAsync()
    {
        _logger.LogInformation("Starting daily report generation job");

        var yesterday = DateTime.UtcNow.Date.AddDays(-1);
        var today = DateTime.UtcNow.Date;

        var stats = new
        {
            JobsCreated = await _context.Jobs.CountAsync(j => j.CreatedAt >= yesterday && j.CreatedAt < today),
            JobsCompleted = await _context.Jobs.CountAsync(j => j.Status == "completed" && j.UpdatedAt >= yesterday && j.UpdatedAt < today),
            NewDrivers = await _context.Drivers.CountAsync(d => d.CreatedAt >= yesterday && d.CreatedAt < today),
            TotalRevenue = await _context.Payments
                .Where(p => p.Status == "completed" && p.CreatedAt >= yesterday && p.CreatedAt < today)
                .SumAsync(p => (decimal?)p.Amount) ?? 0,
            Date = yesterday.ToString("yyyy-MM-dd")
        };

        _logger.LogInformation(
            "Daily report for {Date}: Jobs Created: {JobsCreated}, Jobs Completed: {JobsCompleted}, New Drivers: {NewDrivers}, Revenue: ${Revenue}",
            stats.Date, stats.JobsCreated, stats.JobsCompleted, stats.NewDrivers, stats.TotalRevenue);

        // You could save this to a DailyReports table or send to admins via email
    }
}
