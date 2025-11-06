// Controllers/ActivityLogsController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;
using BeC.OpenId.Connect.Dto;
using OpenIddict.Validation.AspNetCore;

namespace BeC.OpenId.Connect.Features.ActivityLogs.Controllers
{
    [ApiController]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [Route("api/[controller]")]
    public class ActivityLogsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ActivityLogsController> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ActivityLogsController(
            ApplicationDbContext context,
            ILogger<ActivityLogsController> logger,
            IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// Get activity logs with filtering and pagination
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<ActionResult<ActivityLogResponse>> GetLogs(
            [FromQuery] string? userId = null,
            [FromQuery] string? action = null,
            [FromQuery] string? entityType = null,
            [FromQuery] string? severity = null,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] string? searchTerm = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var query = _context.ActivityLogs.AsQueryable();

                // Apply filters
                if (!string.IsNullOrEmpty(userId))
                    query = query.Where(l => l.UserId == userId);

                if (!string.IsNullOrEmpty(action))
                    query = query.Where(l => l.Action == action);

                if (!string.IsNullOrEmpty(entityType))
                    query = query.Where(l => l.EntityType == entityType);

                if (!string.IsNullOrEmpty(severity))
                    query = query.Where(l => l.Severity == severity);

                if (startDate.HasValue)
                    query = query.Where(l => l.Timestamp >= startDate.Value);

                if (endDate.HasValue)
                    query = query.Where(l => l.Timestamp <= endDate.Value);

                if (!string.IsNullOrEmpty(searchTerm))
                {
                    query = query.Where(l =>
                        l.UserName.Contains(searchTerm) ||
                        l.UserEmail.Contains(searchTerm) ||
                        l.Description.Contains(searchTerm) ||
                        (l.EntityName != null && l.EntityName.Contains(searchTerm)));
                }

                // Get total count
                var totalCount = await query.CountAsync();

                // Apply pagination
                var logs = await query
                    .OrderByDescending(l => l.Timestamp)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var response = new ActivityLogResponse
                {
                    Logs = logs,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving activity logs");
                return StatusCode(500, "An error occurred while retrieving activity logs");
            }
        }

        /// <summary>
        /// Get a single activity log by ID
        /// </summary>
        [HttpGet("{id}")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<ActionResult<ActivityLog>> GetLogById(string id)
        {
            var log = await _context.ActivityLogs.FindAsync(id);

            if (log == null)
                return NotFound();

            return Ok(log);
        }

        /// <summary>
        /// Get recent activity logs (last 24 hours)
        /// </summary>
        [HttpGet("recent")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<ActionResult<List<ActivityLog>>> GetRecentLogs([FromQuery] int limit = 20)
        {
            var yesterday = DateTime.UtcNow.AddDays(-1);

            var logs = await _context.ActivityLogs
                .Where(l => l.Timestamp >= yesterday)
                .OrderByDescending(l => l.Timestamp)
                .Take(limit)
                .ToListAsync();

            return Ok(logs);
        }

        /// <summary>
        /// Export activity logs to CSV
        /// </summary>
        [HttpGet("export")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> ExportLogs(
            [FromQuery] string? userId = null,
            [FromQuery] string? action = null,
            [FromQuery] string? entityType = null,
            [FromQuery] string? severity = null,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] string? searchTerm = null)
        {
            try
            {
                var query = _context.ActivityLogs.AsQueryable();

                // Apply same filters as GetLogs
                if (!string.IsNullOrEmpty(userId))
                    query = query.Where(l => l.UserId == userId);

                if (!string.IsNullOrEmpty(action))
                    query = query.Where(l => l.Action == action);

                if (!string.IsNullOrEmpty(entityType))
                    query = query.Where(l => l.EntityType == entityType);

                if (!string.IsNullOrEmpty(severity))
                    query = query.Where(l => l.Severity == severity);

                if (startDate.HasValue)
                    query = query.Where(l => l.Timestamp >= startDate.Value);

                if (endDate.HasValue)
                    query = query.Where(l => l.Timestamp <= endDate.Value);

                if (!string.IsNullOrEmpty(searchTerm))
                {
                    query = query.Where(l =>
                        l.UserName.Contains(searchTerm) ||
                        l.UserEmail.Contains(searchTerm) ||
                        l.Description.Contains(searchTerm));
                }

                var logs = await query
                    .OrderByDescending(l => l.Timestamp)
                    .ToListAsync();

                // Generate CSV
                var csv = new StringBuilder();
                csv.AppendLine("Timestamp,User,Email,Action,Entity Type,Entity Name,Description,Severity,IP Address");

                foreach (var log in logs)
                {
                    csv.AppendLine($"{log.Timestamp:yyyy-MM-dd HH:mm:ss}," +
                                 $"\"{log.UserName}\"," +
                                 $"\"{log.UserEmail}\"," +
                                 $"\"{log.Action}\"," +
                                 $"\"{log.EntityType}\"," +
                                 $"\"{log.EntityName ?? ""}\"," +
                                 $"\"{log.Description}\"," +
                                 $"\"{log.Severity}\"," +
                                 $"\"{log.IpAddress ?? ""}\"");
                }

                var bytes = Encoding.UTF8.GetBytes(csv.ToString());
                var fileName = $"activity-logs-{DateTime.UtcNow:yyyyMMdd-HHmmss}.csv";

                return File(bytes, "text/csv", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting activity logs");
                return StatusCode(500, "An error occurred while exporting activity logs");
            }
        }

        /// <summary>
        /// Create a new activity log
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<ActivityLog>> CreateLog([FromBody] CreateActivityLogRequest request)
        {
            try
            {
                var userId = User.FindFirst("sub")?.Value ?? User.FindFirst("id")?.Value;
                var userName = User.FindFirst("name")?.Value ?? "Unknown";
                var userEmail = User.FindFirst("email")?.Value ?? "unknown@example.com";
                
                var httpContext = _httpContextAccessor.HttpContext;
                var ipAddress = httpContext?.Connection.RemoteIpAddress?.ToString();
                var userAgent = httpContext?.Request.Headers["User-Agent"].ToString();

                var log = new ActivityLog
                {
                    Id = Guid.NewGuid().ToString(),
                    UserId = userId ?? "system",
                    UserName = userName,
                    UserEmail = userEmail,
                    Action = request.Action,
                    EntityType = request.EntityType,
                    EntityId = request.EntityId,
                    EntityName = request.EntityName,
                    Description = request.Description,
                    IpAddress = ipAddress,
                    UserAgent = userAgent,
                    Metadata = request.Metadata,
                    Timestamp = DateTime.UtcNow,
                    Severity = request.Severity ?? "INFO"
                };

                _context.ActivityLogs.Add(log);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetLogById), new { id = log.Id }, log);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating activity log");
                return StatusCode(500, "An error occurred while creating activity log");
            }
        }

        /// <summary>
        /// Delete old activity logs (cleanup)
        /// </summary>
        [HttpDelete("cleanup")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<ActionResult<DeleteLogsResponse>> DeleteOldLogs([FromQuery] DateTime beforeDate)
        {
            try
            {
                var logsToDelete = await _context.ActivityLogs
                    .Where(l => l.Timestamp < beforeDate)
                    .ToListAsync();

                _context.ActivityLogs.RemoveRange(logsToDelete);
                await _context.SaveChangesAsync();

                // Log this action
                await LogActivity(
                    "DATA_CLEANUP",
                    "SYSTEM",
                    null,
                    null,
                    $"Deleted {logsToDelete.Count} activity logs older than {beforeDate:yyyy-MM-dd}",
                    "WARNING");

                return Ok(new DeleteLogsResponse { DeletedCount = logsToDelete.Count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting old activity logs");
                return StatusCode(500, "An error occurred while deleting old logs");
            }
        }

        /// <summary>
        /// Get activity statistics
        /// </summary>
        [HttpGet("statistics")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<ActionResult<ActivityStatistics>> GetStatistics([FromQuery] int days = 30)
        {
            try
            {
                var startDate = DateTime.UtcNow.AddDays(-days);

                var logs = await _context.ActivityLogs
                    .Where(l => l.Timestamp >= startDate)
                    .ToListAsync();

                var statistics = new ActivityStatistics
                {
                    TotalLogs = logs.Count,
                    LogsByAction = logs.GroupBy(l => l.Action)
                        .ToDictionary(g => g.Key, g => g.Count()),
                    LogsByUser = logs.GroupBy(l => l.UserName)
                        .ToDictionary(g => g.Key, g => g.Count()),
                    LogsBySeverity = logs.GroupBy(l => l.Severity)
                        .ToDictionary(g => g.Key, g => g.Count()),
                    LogsByDay = logs.GroupBy(l => l.Timestamp.Date)
                        .Select(g => new LogsByDayItem
                        {
                            Date = g.Key.ToString("yyyy-MM-dd"),
                            Count = g.Count()
                        })
                        .OrderBy(x => x.Date)
                        .ToList()
                };

                return Ok(statistics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving activity statistics");
                return StatusCode(500, "An error occurred while retrieving statistics");
            }
        }

        // Helper method to log activities
        private async Task LogActivity(
            string action,
            string entityType,
            string? entityId,
            string? entityName,
            string description,
            string severity = "INFO")
        {
            var userId = User.FindFirst("sub")?.Value ?? User.FindFirst("id")?.Value ?? "system";
            var userName = User.FindFirst("name")?.Value ?? "System";
            var userEmail = User.FindFirst("email")?.Value ?? "system@example.com";
            
            var httpContext = _httpContextAccessor.HttpContext;
            var ipAddress = httpContext?.Connection.RemoteIpAddress?.ToString();
            var userAgent = httpContext?.Request.Headers["User-Agent"].ToString();

            var log = new ActivityLog
            {
                Id = Guid.NewGuid().ToString(),
                UserId = userId,
                UserName = userName,
                UserEmail = userEmail,
                Action = action,
                EntityType = entityType,
                EntityId = entityId,
                EntityName = entityName,
                Description = description,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                Timestamp = DateTime.UtcNow,
                Severity = severity
            };

            _context.ActivityLogs.Add(log);
            await _context.SaveChangesAsync();
        }
    }

    // DTOs
    public class CreateActivityLogRequest
    {
        public string Action { get; set; } = string.Empty;
        public string EntityType { get; set; } = string.Empty;
        public string? EntityId { get; set; }
        public string? EntityName { get; set; }
        public string Description { get; set; } = string.Empty;
        public Dictionary<string, object>? Metadata { get; set; }
        public string? Severity { get; set; }
    }

    public class ActivityLogResponse
    {
        public List<ActivityLog> Logs { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }

    public class DeleteLogsResponse
    {
        public int DeletedCount { get; set; }
    }

    public class ActivityStatistics
    {
        public int TotalLogs { get; set; }
        public Dictionary<string, int> LogsByAction { get; set; } = new();
        public Dictionary<string, int> LogsByUser { get; set; } = new();
        public Dictionary<string, int> LogsBySeverity { get; set; } = new();
        public List<LogsByDayItem> LogsByDay { get; set; } = new();
    }

    public class LogsByDayItem
    {
        public string Date { get; set; } = string.Empty;
        public int Count { get; set; }
    }
}