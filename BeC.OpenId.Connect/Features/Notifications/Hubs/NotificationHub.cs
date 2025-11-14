using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace BeC.OpenId.Connect.Features.Notifications.Hubs;

/// <summary>
/// SignalR Hub for real-time notifications
/// </summary>
[Authorize]
public class NotificationHub : Hub
{
    private readonly ILogger<NotificationHub> _logger;

    public NotificationHub(ILogger<NotificationHub> logger)
    {
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!string.IsNullOrEmpty(userId))
        {
            // Add user to their personal group
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");

            // Add to role-based groups
            var roles = Context.User?.Claims
                .Where(c => c.Type == ClaimTypes.Role)
                .Select(c => c.Value)
                .ToList() ?? new List<string>();

            foreach (var role in roles)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"role_{role}");
            }

            _logger.LogInformation("User {UserId} connected to NotificationHub with connection {ConnectionId}",
                userId, Context.ConnectionId);
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!string.IsNullOrEmpty(userId))
        {
            _logger.LogInformation("User {UserId} disconnected from NotificationHub", userId);
        }

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Join a specific group (e.g., for a job)
    /// </summary>
    public async Task JoinJobGroup(string jobId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"job_{jobId}");
        _logger.LogInformation("Connection {ConnectionId} joined job group {JobId}",
            Context.ConnectionId, jobId);
    }

    /// <summary>
    /// Leave a specific group
    /// </summary>
    public async Task LeaveJobGroup(string jobId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"job_{jobId}");
        _logger.LogInformation("Connection {ConnectionId} left job group {JobId}",
            Context.ConnectionId, jobId);
    }

    /// <summary>
    /// Mark notification as read (client calls this)
    /// </summary>
    public async Task MarkAsRead(Guid notificationId)
    {
        var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        _logger.LogInformation("User {UserId} marked notification {NotificationId} as read",
            userId, notificationId);

        // The actual database update should be done through the API endpoint
        // This is just for client-side acknowledgment
        await Task.CompletedTask;
    }
}
