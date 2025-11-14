using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Validation.AspNetCore;
using BeC.OpenId.Connect.Dto;
using BeC.OpenId.Connect.Features.Notifications.Dtos;
using BeC.OpenId.Connect.Features.Users.Dtos;
using BeC.OpenId.Connect.Features.ActivityLogs.Services.Interfaces;
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
    private readonly ILogger<NotificationsController> _logger;

    public NotificationsController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IActivityLogService activityLogService,
        ILogger<NotificationsController> logger)
    {
        _context = context;
        _userManager = userManager;
        _activityLogService = activityLogService;
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
    /// Update notification preferences (placeholder for future implementation)
    /// </summary>
    [HttpPatch("settings")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateNotificationSettings([FromBody] NotificationSettingsDto settings)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        // TODO: Implement user notification preferences table
        // For now, just acknowledge the request

        await _activityLogService.LogActivityAsync(
            "notification_settings_updated",
            "User",
            userId,
            "Notification Settings",
            "User updated notification preferences"
        ,
            userId: userId
        );

        return Ok(new { message = "Notification settings updated (feature coming soon)" });
    }

    #region Admin Endpoints

    /// <summary>
    /// Send notification to a specific user (Admin)
    /// </summary>
    [HttpPost("send")]
    [Authorize(Roles = AuthRoles.Admin + "," + AuthRoles.SuperAdmin)]
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
    [Authorize(Roles = AuthRoles.Admin + "," + AuthRoles.SuperAdmin)]
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
        var isAdmin = userRoles.Contains(AuthRoles.Admin) || userRoles.Contains(AuthRoles.SuperAdmin);

        if (!isAdmin && notification.UserId != userId)
            return Forbid("You can only view your own notifications");

        return Ok(notification);
    }

    /// <summary>
    /// Get notification statistics (Admin)
    /// </summary>
    [HttpGet("~/api/admin/notifications/statistics")]
    [Authorize(Roles = AuthRoles.Admin + "," + AuthRoles.SuperAdmin)]
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
    [Authorize(Roles = AuthRoles.SuperAdmin)]
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
            $"Deleted {expiredNotifications.Count} expired notifications",
            userId: userId
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

#endregion
