namespace BeC.OpenId.Connect.Features.Notifications.Services.Interfaces;

/// <summary>
/// Service for sending real-time notifications via SignalR
/// </summary>
public interface IRealtimeNotificationService
{
    /// <summary>
    /// Send notification to a specific user
    /// </summary>
    Task SendToUserAsync(string userId, string type, object data);

    /// <summary>
    /// Send notification to all users with a specific role
    /// </summary>
    Task SendToRoleAsync(string role, string type, object data);

    /// <summary>
    /// Send notification to all users in a job group (customer + assigned driver)
    /// </summary>
    Task SendToJobGroupAsync(string jobId, string type, object data);

    /// <summary>
    /// Send notification to multiple users
    /// </summary>
    Task SendToUsersAsync(IEnumerable<string> userIds, string type, object data);

    /// <summary>
    /// Broadcast notification to all connected users
    /// </summary>
    Task BroadcastAsync(string type, object data);
}
