using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using BeC.Common.Data.Repositories.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Validation.AspNetCore;
using BeC.OpenId.Connect.Dto;
using BeC.OpenId.Connect.Features.Notifications.Dtos;
using BeC.OpenId.Connect.Features.Users.Dtos;
using BeC.OpenId.Connect.Features.ActivityLogs.Services.Interfaces;
using BeC.OpenId.Connect.Features.Notifications.Services.Interfaces;
using BeC.OpenId.Connect.Infrastructure.Authorization;
using AuthRoles = BeC.OpenId.Connect.Infrastructure.Authorization.Roles;

namespace BeC.OpenId.Connect.Features.Notifications.Controllers;

/// <summary>
/// Notification management endpoints
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
[Produces("application/json")]
public class NotificationsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IActivityLogService _activityLogService;
    private readonly INotificationService _notificationService;
    private readonly ILogger<NotificationsController> _logger;

    public NotificationsController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IActivityLogService activityLogService,
        INotificationService notificationService,
        ILogger<NotificationsController> logger)
    {
        _context = context;
        _userManager = userManager;
        _activityLogService = activityLogService;
        _notificationService = notificationService;
        _logger = logger;
    }

    /// <summary>
    /// Get my notifications
    /// </summary>
    [HttpGet("me")]
    [ProducesResponseType(typeof(List<Notification>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<Notification>>> GetMyNotifications(
        [FromQuery] bool? isRead = null,
        [FromQuery] string? type = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var query = _context.Notifications
            .Where(n => n.UserId == userId)
            .AsQueryable();

        if (isRead.HasValue)
        {
            query = query.Where(n => n.IsRead == isRead.Value);
        }

        if (!string.IsNullOrWhiteSpace(type))
        {
            query = query.Where(n => n.Type == type);
        }

        // Filter out expired notifications
        query = query.Where(n => !n.ExpiresAt.HasValue || n.ExpiresAt.Value > DateTime.UtcNow);

        var totalCount = await query.CountAsync();
        var notifications = await query
            .OrderByDescending(n => n.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        Response.Headers.Append("X-Total-Count", totalCount.ToString());
        Response.Headers.Append("X-Page", page.ToString());
        Response.Headers.Append("X-Page-Size", pageSize.ToString());

        return Ok(notifications);
    }

    /// <summary>
    /// Get unread notification count
    /// </summary>
    [HttpGet("me/unread-count")]
    [ProducesResponseType(typeof(UnreadCountDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<UnreadCountDto>> GetUnreadCount()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var count = await _context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .Where(n => !n.ExpiresAt.HasValue || n.ExpiresAt.Value > DateTime.UtcNow)
            .CountAsync();

        return Ok(new UnreadCountDto { Count = count });
    }

    /// <summary>
    /// Mark notification as read
    /// </summary>
    [HttpPatch("{id}/read")]
    [ProducesResponseType(typeof(Notification), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Notification>> MarkAsRead(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);

        if (notification == null)
            return NotFound();

        if (!notification.IsRead)
        {
            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        return Ok(notification);
    }

    /// <summary>
    /// Mark all notifications as read
    /// </summary>
    [HttpPatch("read-all")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> MarkAllAsRead()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var unreadNotifications = await _context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync();

        foreach (var notification in unreadNotifications)
        {
            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        return Ok(new { message = $"Marked {unreadNotifications.Count} notifications as read" });
    }

    /// <summary>
    /// Delete a notification
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteNotification(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);

        if (notification == null)
            return NotFound();

        _context.Notifications.Remove(notification);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>
    /// Get my notification preferences
    /// </summary>
    [HttpGet("preferences")]
    [ProducesResponseType(typeof(NotificationPreferences), StatusCodes.Status200OK)]
    public async Task<ActionResult<NotificationPreferences>> GetMyPreferences()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var preferences = await _notificationService.GetUserPreferencesAsync(userId);
        return Ok(preferences);
    }

    /// <summary>
    /// Update notification preferences
    /// </summary>
    [HttpPut("preferences")]
    [ProducesResponseType(typeof(NotificationPreferences), StatusCodes.Status200OK)]
    public async Task<ActionResult<NotificationPreferences>> UpdatePreferences([FromBody] UpdateNotificationPreferencesDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var preferences = await _context.NotificationPreferences
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (preferences == null)
        {
            preferences = new NotificationPreferences { UserId = userId };
            _context.NotificationPreferences.Add(preferences);
        }

        // Update channel preferences
        if (dto.EmailEnabled.HasValue) preferences.EmailEnabled = dto.EmailEnabled.Value;
        if (dto.SmsEnabled.HasValue) preferences.SmsEnabled = dto.SmsEnabled.Value;
        if (dto.PushEnabled.HasValue) preferences.PushEnabled = dto.PushEnabled.Value;

        // Job notifications
        if (dto.JobAssignedEmail.HasValue) preferences.JobAssignedEmail = dto.JobAssignedEmail.Value;
        if (dto.JobAssignedSms.HasValue) preferences.JobAssignedSms = dto.JobAssignedSms.Value;
        if (dto.JobAssignedPush.HasValue) preferences.JobAssignedPush = dto.JobAssignedPush.Value;

        if (dto.JobCompletedEmail.HasValue) preferences.JobCompletedEmail = dto.JobCompletedEmail.Value;
        if (dto.JobCompletedSms.HasValue) preferences.JobCompletedSms = dto.JobCompletedSms.Value;
        if (dto.JobCompletedPush.HasValue) preferences.JobCompletedPush = dto.JobCompletedPush.Value;

        if (dto.JobCancelledEmail.HasValue) preferences.JobCancelledEmail = dto.JobCancelledEmail.Value;
        if (dto.JobCancelledSms.HasValue) preferences.JobCancelledSms = dto.JobCancelledSms.Value;
        if (dto.JobCancelledPush.HasValue) preferences.JobCancelledPush = dto.JobCancelledPush.Value;

        if (dto.JobRescheduledEmail.HasValue) preferences.JobRescheduledEmail = dto.JobRescheduledEmail.Value;
        if (dto.JobRescheduledSms.HasValue) preferences.JobRescheduledSms = dto.JobRescheduledSms.Value;
        if (dto.JobRescheduledPush.HasValue) preferences.JobRescheduledPush = dto.JobRescheduledPush.Value;

        // Payment notifications
        if (dto.PaymentReceivedEmail.HasValue) preferences.PaymentReceivedEmail = dto.PaymentReceivedEmail.Value;
        if (dto.PaymentReceivedSms.HasValue) preferences.PaymentReceivedSms = dto.PaymentReceivedSms.Value;
        if (dto.PaymentReceivedPush.HasValue) preferences.PaymentReceivedPush = dto.PaymentReceivedPush.Value;

        if (dto.PayoutProcessedEmail.HasValue) preferences.PayoutProcessedEmail = dto.PayoutProcessedEmail.Value;
        if (dto.PayoutProcessedSms.HasValue) preferences.PayoutProcessedSms = dto.PayoutProcessedSms.Value;
        if (dto.PayoutProcessedPush.HasValue) preferences.PayoutProcessedPush = dto.PayoutProcessedPush.Value;

        // Review notifications
        if (dto.ReviewReceivedEmail.HasValue) preferences.ReviewReceivedEmail = dto.ReviewReceivedEmail.Value;
        if (dto.ReviewReceivedSms.HasValue) preferences.ReviewReceivedSms = dto.ReviewReceivedSms.Value;
        if (dto.ReviewReceivedPush.HasValue) preferences.ReviewReceivedPush = dto.ReviewReceivedPush.Value;

        // System notifications
        if (dto.SystemAlertsEmail.HasValue) preferences.SystemAlertsEmail = dto.SystemAlertsEmail.Value;
        if (dto.SystemAlertsSms.HasValue) preferences.SystemAlertsSms = dto.SystemAlertsSms.Value;
        if (dto.SystemAlertsPush.HasValue) preferences.SystemAlertsPush = dto.SystemAlertsPush.Value;

        // Promotional
        if (dto.PromotionalEmail.HasValue) preferences.PromotionalEmail = dto.PromotionalEmail.Value;
        if (dto.PromotionalSms.HasValue) preferences.PromotionalSms = dto.PromotionalSms.Value;
        if (dto.PromotionalPush.HasValue) preferences.PromotionalPush = dto.PromotionalPush.Value;

        // Quiet hours
        if (dto.EnableQuietHours.HasValue) preferences.EnableQuietHours = dto.EnableQuietHours.Value;
        if (dto.QuietHoursStart.HasValue) preferences.QuietHoursStart = dto.QuietHoursStart;
        if (dto.QuietHoursEnd.HasValue) preferences.QuietHoursEnd = dto.QuietHoursEnd;

        // Digest
        if (dto.EnableEmailDigest.HasValue) preferences.EnableEmailDigest = dto.EnableEmailDigest.Value;
        if (!string.IsNullOrEmpty(dto.DigestFrequency)) preferences.DigestFrequency = dto.DigestFrequency;

        preferences.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        await _activityLogService.LogActivityAsync(
            userId,
            "notification_preferences_updated",
            "NotificationPreferences",
            preferences.Id.ToString(),
            "Preferences",
            "User updated notification preferences"
        );

        return Ok(preferences);
    }

    #region Admin Endpoints

    /// <summary>
    /// Send notification to a specific user (Admin)
    /// </summary>
    [HttpPost("send")]
    [Authorize(Roles = Infrastructure.Authorization.Roles.Admin + "," + Infrastructure.Authorization.Roles.SuperAdmin)]
    [ProducesResponseType(typeof(Notification), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Notification>> SendNotification([FromBody] SendNotificationDto request)
    {
        var adminUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(adminUserId))
            return Unauthorized();

        // Verify target user exists
        var targetUser = await _userManager.FindByIdAsync(request.UserId);
        if (targetUser == null)
            return NotFound("Target user not found");

        var notification = new Notification
        {
            UserId = request.UserId,
            Title = request.Title,
            Message = request.Message,
            Type = request.Type,
            Category = request.Category,
            EntityType = request.EntityType,
            EntityId = request.EntityId,
            ActionUrl = request.ActionUrl,
            ActionText = request.ActionText,
            Data = request.Data != null ? JsonSerializer.Serialize(request.Data) : null,
            Priority = request.Priority ?? "normal",
            SendEmail = request.SendEmail,
            SendPush = request.SendPush,
            ExpiresAt = request.ExpiresAt
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        // TODO: Implement actual email and push notification sending
        if (request.SendEmail)
        {
            // Send email notification
            _logger.LogInformation($"Email notification queued for user {request.UserId}");
        }

        if (request.SendPush)
        {
            // Send push notification
            _logger.LogInformation($"Push notification queued for user {request.UserId}");
        }

        await _activityLogService.LogActivityAsync(
            adminUserId,
            "notification_sent",
            "Notification",
            notification.Id.ToString(),
            notification.Title,
            $"Admin sent notification to user {targetUser.Email}"
        );

        return CreatedAtAction(nameof(GetNotification), new { id = notification.Id }, notification);
    }

    /// <summary>
    /// Broadcast notification to a role or all users (Admin)
    /// </summary>
    [HttpPost("broadcast")]
    [Authorize(Roles = Infrastructure.Authorization.Roles.Admin + "," + Infrastructure.Authorization.Roles.SuperAdmin)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> BroadcastNotification([FromBody] BroadcastNotificationDto request)
    {
        var adminUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(adminUserId))
            return Unauthorized();

        List<string> targetUserIds;

        if (request.Role == "all")
        {
            // Send to all users
            targetUserIds = await _userManager.Users.Select(u => u.Id).ToListAsync();
        }
        else
        {
            // Send to specific role
            var usersInRole = await _userManager.GetUsersInRoleAsync(request.Role);
            targetUserIds = usersInRole.Select(u => u.Id).ToList();
        }

        if (targetUserIds.Count == 0)
            return BadRequest("No users found for the specified criteria");

        var notifications = targetUserIds.Select(userId => new Notification
        {
            UserId = userId,
            Title = request.Title,
            Message = request.Message,
            Type = request.Type,
            Category = request.Category,
            Priority = request.Priority ?? "normal",
            SendEmail = request.SendEmail,
            SendPush = request.SendPush,
            ExpiresAt = request.ExpiresAt
        }).ToList();

        _context.Notifications.AddRange(notifications);
        await _context.SaveChangesAsync();

        await _activityLogService.LogActivityAsync(
            adminUserId,
            "notification_broadcast",
            "Notification",
            "broadcast",
            request.Title,
            $"Admin broadcast notification to {targetUserIds.Count} users (role: {request.Role})"
        );

        return Ok(new { message = $"Notification sent to {targetUserIds.Count} users" });
    }

    /// <summary>
    /// Get notification by ID (Admin can see all, users can see their own)
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(Notification), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Notification>> GetNotification(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var notification = await _context.Notifications.FindAsync(id);
        if (notification == null)
            return NotFound();

        // Check permissions
        var user = await _userManager.FindByIdAsync(userId);
        var userRoles = await _userManager.GetRolesAsync(user!);
        var isAdmin = userRoles.Contains(Infrastructure.Authorization.Roles.Admin) || userRoles.Contains(Infrastructure.Authorization.Roles.SuperAdmin);

        if (!isAdmin && notification.UserId != userId)
            return Forbid("You can only view your own notifications");

        return Ok(notification);
    }

    /// <summary>
    /// Get notification statistics (Admin)
    /// </summary>
    [HttpGet("~/api/admin/notifications/statistics")]
    [Authorize(Roles = Infrastructure.Authorization.Roles.Admin + "," + Infrastructure.Authorization.Roles.SuperAdmin)]
    [ProducesResponseType(typeof(NotificationStatistics), StatusCodes.Status200OK)]
    public async Task<ActionResult<NotificationStatistics>> GetNotificationStatistics()
    {
        var allNotifications = await _context.Notifications.ToListAsync();
        var last24Hours = DateTime.UtcNow.AddHours(-24);

        var stats = new NotificationStatistics
        {
            TotalNotifications = allNotifications.Count,
            UnreadNotifications = allNotifications.Count(n => !n.IsRead),
            ReadNotifications = allNotifications.Count(n => n.IsRead),
            Last24Hours = allNotifications.Count(n => n.CreatedAt >= last24Hours),
            ByType = allNotifications
                .GroupBy(n => n.Type)
                .ToDictionary(g => g.Key, g => g.Count()),
            ByPriority = allNotifications
                .GroupBy(n => n.Priority)
                .ToDictionary(g => g.Key, g => g.Count()),
            EmailsSent = allNotifications.Count(n => n.EmailSent),
            PushNotificationsSent = allNotifications.Count(n => n.PushSent)
        };

        return Ok(stats);
    }

    /// <summary>
    /// Cleanup expired notifications (Admin)
    /// </summary>
    [HttpDelete("~/api/admin/notifications/cleanup")]
    [Authorize(Roles = Infrastructure.Authorization.Roles.SuperAdmin)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> CleanupExpiredNotifications()
    {
        var adminUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(adminUserId))
            return Unauthorized();

        var expiredNotifications = await _context.Notifications
            .Where(n => n.ExpiresAt.HasValue && n.ExpiresAt.Value < DateTime.UtcNow)
            .ToListAsync();

        _context.Notifications.RemoveRange(expiredNotifications);
        await _context.SaveChangesAsync();

        await _activityLogService.LogActivityAsync(
            adminUserId,
            "notifications_cleanup",
            "System",
            "notifications",
            "Cleanup",
            $"Deleted {expiredNotifications.Count} expired notifications"
        );

        return Ok(new { message = $"Deleted {expiredNotifications.Count} expired notifications" });
    }

    #endregion
}

#region DTOs

public class UnreadCountDto
{
    public int Count { get; set; }
}

public class NotificationSettingsDto
{
    public bool EmailEnabled { get; set; }
    public bool PushEnabled { get; set; }
    public bool JobNotifications { get; set; }
    public bool PaymentNotifications { get; set; }
    public bool SystemNotifications { get; set; }
}

public class SendNotificationDto
{
    public required string UserId { get; set; }
    public required string Title { get; set; }
    public required string Message { get; set; }
    public required string Type { get; set; }
    public string? Category { get; set; }
    public string? EntityType { get; set; }
    public string? EntityId { get; set; }
    public string? ActionUrl { get; set; }
    public string? ActionText { get; set; }
    public object? Data { get; set; }
    public string? Priority { get; set; }
    public bool SendEmail { get; set; }
    public bool SendPush { get; set; }
    public DateTime? ExpiresAt { get; set; }
}

public class BroadcastNotificationDto
{
    public required string Role { get; set; } // "Customer", "Driver", "Admin", "all"
    public required string Title { get; set; }
    public required string Message { get; set; }
    public required string Type { get; set; }
    public string? Category { get; set; }
    public string? Priority { get; set; }
    public bool SendEmail { get; set; }
    public bool SendPush { get; set; }
    public DateTime? ExpiresAt { get; set; }
}

public class NotificationStatistics
{
    public int TotalNotifications { get; set; }
    public int UnreadNotifications { get; set; }
    public int ReadNotifications { get; set; }
    public int Last24Hours { get; set; }
    public Dictionary<string, int> ByType { get; set; } = new();
    public Dictionary<string, int> ByPriority { get; set; } = new();
    public int EmailsSent { get; set; }
    public int PushNotificationsSent { get; set; }
}

public class UpdateNotificationPreferencesDto
{
    // Channel preferences
    public bool? EmailEnabled { get; set; }
    public bool? SmsEnabled { get; set; }
    public bool? PushEnabled { get; set; }

    // Job notifications
    public bool? JobAssignedEmail { get; set; }
    public bool? JobAssignedSms { get; set; }
    public bool? JobAssignedPush { get; set; }

    public bool? JobCompletedEmail { get; set; }
    public bool? JobCompletedSms { get; set; }
    public bool? JobCompletedPush { get; set; }

    public bool? JobCancelledEmail { get; set; }
    public bool? JobCancelledSms { get; set; }
    public bool? JobCancelledPush { get; set; }

    public bool? JobRescheduledEmail { get; set; }
    public bool? JobRescheduledSms { get; set; }
    public bool? JobRescheduledPush { get; set; }

    // Payment notifications
    public bool? PaymentReceivedEmail { get; set; }
    public bool? PaymentReceivedSms { get; set; }
    public bool? PaymentReceivedPush { get; set; }

    public bool? PayoutProcessedEmail { get; set; }
    public bool? PayoutProcessedSms { get; set; }
    public bool? PayoutProcessedPush { get; set; }

    // Review notifications
    public bool? ReviewReceivedEmail { get; set; }
    public bool? ReviewReceivedSms { get; set; }
    public bool? ReviewReceivedPush { get; set; }

    // System notifications
    public bool? SystemAlertsEmail { get; set; }
    public bool? SystemAlertsSms { get; set; }
    public bool? SystemAlertsPush { get; set; }

    // Promotional
    public bool? PromotionalEmail { get; set; }
    public bool? PromotionalSms { get; set; }
    public bool? PromotionalPush { get; set; }

    // Quiet hours
    public bool? EnableQuietHours { get; set; }
    public TimeSpan? QuietHoursStart { get; set; }
    public TimeSpan? QuietHoursEnd { get; set; }

    // Digest
    public bool? EnableEmailDigest { get; set; }
    public string? DigestFrequency { get; set; }
}

#endregion
