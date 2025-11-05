// Features/ActivityLogs/Services/ActivityLogService.cs

using BeC.OpenId.Connect.Data;
using BeC.OpenId.Connect.Features.ActivityLogs.Services.Interfaces;
using System.Text.Json;

namespace BeC.OpenId.Connect.Features.ActivityLogs.Services;

public class ActivityLogService : IActivityLogService
{
    private readonly ApplicationDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ActivityLogService(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task LogActivityAsync(
        string action, 
        string entityType, 
        string? entityId, 
        string? entityName, 
        string description, 
        string severity = "INFO",
        string? userId = null,
        string? userName = null,
        string? userEmail = null,
        Dictionary<string, object>? metadata = null)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        var user = httpContext?.User;
        
        // Use provided user info or extract from current user context
        var finalUserId = userId ?? user?.FindFirst("sub")?.Value ?? user?.FindFirst("id")?.Value ?? "system";
        var finalUserName = userName ?? user?.FindFirst("name")?.Value ?? "System";
        var finalUserEmail = userEmail ?? user?.FindFirst("email")?.Value ?? "system@example.com";
        
        var ipAddress = httpContext?.Connection.RemoteIpAddress?.ToString();
        var userAgent = httpContext?.Request.Headers["User-Agent"].ToString();

        var log = new ActivityLog
        {
            Id = Guid.NewGuid().ToString(),
            UserId = finalUserId,
            UserName = finalUserName,
            UserEmail = finalUserEmail,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            EntityName = entityName,
            Description = description,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            Timestamp = DateTime.UtcNow,
            Severity = severity,
            // Serialize metadata to JSON
            MetadataJson = metadata != null ? JsonSerializer.Serialize(metadata) : null
        };

        _context.ActivityLogs.Add(log);
        await _context.SaveChangesAsync();
    }
}