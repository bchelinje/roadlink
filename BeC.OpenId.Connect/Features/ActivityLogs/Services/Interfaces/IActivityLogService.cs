namespace BeC.OpenId.Connect.Features.ActivityLogs.Services.Interfaces;

public interface IActivityLogService
{
    /// <summary>
    /// Log an activity with optional metadata
    /// </summary>
    /// <param name="action">The action performed (e.g., "USER_CREATED")</param>
    /// <param name="entityType">The type of entity (e.g., "USER")</param>
    /// <param name="entityId">The ID of the entity</param>
    /// <param name="entityName">The name of the entity</param>
    /// <param name="description">Human-readable description</param>
    /// <param name="severity">Severity level: INFO, WARNING, ERROR, CRITICAL</param>
    /// <param name="userId">Optional: Override the user ID (if not using current user)</param>
    /// <param name="userName">Optional: Override the user name</param>
    /// <param name="userEmail">Optional: Override the user email</param>
    /// <param name="metadata">Optional: Additional structured data</param>
    Task LogActivityAsync(
        string action, 
        string entityType, 
        string? entityId,
        string? entityName, 
        string description, 
        string severity = "INFO",
        string? userId = null,
        string? userName = null,
        string? userEmail = null,
        Dictionary<string, object>? metadata = null);
}