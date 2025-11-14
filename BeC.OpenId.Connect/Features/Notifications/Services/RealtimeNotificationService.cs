using Microsoft.AspNetCore.SignalR;
using BeC.OpenId.Connect.Features.Notifications.Hubs;
using BeC.OpenId.Connect.Features.Notifications.Services.Interfaces;

namespace BeC.OpenId.Connect.Features.Notifications.Services;

/// <summary>
/// Implementation of real-time notification service using SignalR
/// </summary>
public class RealtimeNotificationService : IRealtimeNotificationService
{
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly ILogger<RealtimeNotificationService> _logger;

    public RealtimeNotificationService(
        IHubContext<NotificationHub> hubContext,
        ILogger<RealtimeNotificationService> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task SendToUserAsync(string userId, string type, object data)
    {
        try
        {
            await _hubContext.Clients
                .Group($"user_{userId}")
                .SendAsync("ReceiveNotification", new
                {
                    type,
                    data,
                    timestamp = DateTime.UtcNow
                });

            _logger.LogInformation("Sent real-time notification of type {Type} to user {UserId}", type, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending real-time notification to user {UserId}", userId);
        }
    }

    public async Task SendToRoleAsync(string role, string type, object data)
    {
        try
        {
            await _hubContext.Clients
                .Group($"role_{role}")
                .SendAsync("ReceiveNotification", new
                {
                    type,
                    data,
                    timestamp = DateTime.UtcNow
                });

            _logger.LogInformation("Sent real-time notification of type {Type} to role {Role}", type, role);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending real-time notification to role {Role}", role);
        }
    }

    public async Task SendToJobGroupAsync(string jobId, string type, object data)
    {
        try
        {
            await _hubContext.Clients
                .Group($"job_{jobId}")
                .SendAsync("ReceiveNotification", new
                {
                    type,
                    data,
                    timestamp = DateTime.UtcNow
                });

            _logger.LogInformation("Sent real-time notification of type {Type} to job group {JobId}", type, jobId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending real-time notification to job group {JobId}", jobId);
        }
    }

    public async Task SendToUsersAsync(IEnumerable<string> userIds, string type, object data)
    {
        try
        {
            var groups = userIds.Select(id => $"user_{id}");

            await _hubContext.Clients
                .Groups(groups)
                .SendAsync("ReceiveNotification", new
                {
                    type,
                    data,
                    timestamp = DateTime.UtcNow
                });

            _logger.LogInformation("Sent real-time notification of type {Type} to {Count} users", type, userIds.Count());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending real-time notification to multiple users");
        }
    }

    public async Task BroadcastAsync(string type, object data)
    {
        try
        {
            await _hubContext.Clients.All
                .SendAsync("ReceiveNotification", new
                {
                    type,
                    data,
                    timestamp = DateTime.UtcNow
                });

            _logger.LogInformation("Broadcast real-time notification of type {Type} to all users", type);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting real-time notification");
        }
    }
}
