using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BeC.OpenId.Connect.Dto;
using BeC.OpenId.Connect.Features.Messages.Dtos;
using System.Security.Claims;
using BeC.OpenId.Connect.Features.ActivityLogs.Services.Interfaces;
using OpenIddict.Validation.AspNetCore;

namespace BeC.OpenId.Connect.Features.Messages.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
public class MessagesController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<MessagesController> _logger;
    private readonly IActivityLogService _activityLogService;

    public MessagesController(
        ApplicationDbContext context,
        ILogger<MessagesController> logger,
        IActivityLogService activityLogService)
    {
        _context = context;
        _logger = logger;
        _activityLogService = activityLogService;
    }

    /// <summary>
    /// Send a message in a job conversation
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(MessageDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MessageDto>> SendMessage([FromBody] SendMessageDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        // Get the job
        var job = await _context.Jobs.FirstOrDefaultAsync(j => j.Id == dto.JobId);
        if (job == null)
            return NotFound("Job not found");

        // Determine sender and receiver
        string senderType;
        string receiverId;
        string receiverType;
        string? senderName = null;
        string? receiverName = null;

        var driver = await _context.Drivers.FirstOrDefaultAsync(d => d.UserId == userId);
        bool isDriver = driver != null && job.DriverId == driver.Id;
        bool isCustomer = job.CustomerId == userId;

        if (isDriver)
        {
            senderType = "driver";
            receiverId = job.CustomerId;
            receiverType = "customer";
            senderName = $"{driver!.FirstName} {driver.LastName}";
            receiverName = job.CustomerName;
        }
        else if (isCustomer)
        {
            senderType = "customer";
            if (job.DriverId == null)
                return BadRequest("Job does not have an assigned driver yet");

            receiverId = _context.Drivers
                .Where(d => d.Id == job.DriverId)
                .Select(d => d.UserId)
                .FirstOrDefault() ?? "";

            if (string.IsNullOrEmpty(receiverId))
                return BadRequest("Could not find driver user ID");

            receiverType = "driver";
            senderName = job.CustomerName;
            receiverName = job.DriverName;
        }
        else
        {
            return Forbid("You are not authorized to send messages for this job");
        }

        // Create the message
        var message = new Message
        {
            JobId = dto.JobId,
            SenderId = userId,
            SenderName = senderName,
            SenderType = senderType,
            ReceiverId = receiverId,
            ReceiverName = receiverName,
            ReceiverType = receiverType,
            Content = dto.Content,
            MessageType = dto.MessageType ?? "text",
            Attachments = dto.Attachments,
            Status = "sent",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Messages.Add(message);
        await _context.SaveChangesAsync();

        await _activityLogService.LogActivityAsync(
            action: "message.sent",
            entityType: "Message",
            entityId: message.Id.ToString(),
            entityName: $"Message for Job {job.JobNumber}",
            description: $"{senderType} sent a message for job {job.JobNumber}",
            severity: "INFO",
            metadata: new Dictionary<string, object>
            {
                { "JobId", job.Id },
                { "JobNumber", job.JobNumber },
                { "SenderType", senderType },
                { "ReceiverType", receiverType }
            }
        );

        // TODO: Send push notification to receiver

        return CreatedAtAction(nameof(GetMessage), new { id = message.Id }, MapToDto(message));
    }

    /// <summary>
    /// Get a specific message
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(MessageDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MessageDto>> GetMessage(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var message = await _context.Messages.FirstOrDefaultAsync(m => m.Id == id);
        if (message == null)
            return NotFound();

        // Authorization: only sender or receiver can view
        if (message.SenderId != userId && message.ReceiverId != userId &&
            !User.IsInRole("Admin") && !User.IsInRole("SuperAdmin"))
            return Forbid();

        // Mark as read if the user is the receiver and it's not already read
        if (message.ReceiverId == userId && !message.IsRead)
        {
            message.IsRead = true;
            message.ReadAt = DateTime.UtcNow;
            message.Status = "read";
            message.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        return Ok(MapToDto(message));
    }

    /// <summary>
    /// Get all messages for a job (conversation)
    /// </summary>
    [HttpGet("job/{jobId}")]
    [ProducesResponseType(typeof(ConversationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ConversationDto>> GetJobConversation(
        Guid jobId,
        [FromQuery] int? limit = 50,
        [FromQuery] DateTime? before = null)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var job = await _context.Jobs.FirstOrDefaultAsync(j => j.Id == jobId);
        if (job == null)
            return NotFound("Job not found");

        // Authorization: only customer or assigned driver can view
        var driver = await _context.Drivers.FirstOrDefaultAsync(d => d.UserId == userId);
        bool isDriver = driver != null && job.DriverId == driver.Id;
        bool isCustomer = job.CustomerId == userId;

        if (!isDriver && !isCustomer && !User.IsInRole("Admin") && !User.IsInRole("SuperAdmin"))
            return Forbid();

        // Get messages
        var query = _context.Messages.Where(m => m.JobId == jobId);

        if (before.HasValue)
            query = query.Where(m => m.CreatedAt < before.Value);

        var messages = await query
            .OrderByDescending(m => m.CreatedAt)
            .Take(limit ?? 50)
            .ToListAsync();

        // Mark unread messages as read
        var unreadMessages = messages.Where(m => m.ReceiverId == userId && !m.IsRead).ToList();
        foreach (var msg in unreadMessages)
        {
            msg.IsRead = true;
            msg.ReadAt = DateTime.UtcNow;
            msg.Status = "read";
            msg.UpdatedAt = DateTime.UtcNow;
        }

        if (unreadMessages.Any())
            await _context.SaveChangesAsync();

        // Reverse to chronological order
        messages.Reverse();

        var unreadCount = await _context.Messages
            .CountAsync(m => m.JobId == jobId && m.ReceiverId == userId && !m.IsRead);

        return Ok(new ConversationDto
        {
            JobId = jobId,
            JobNumber = job.JobNumber,
            Messages = messages.Select(MapToDto).ToList(),
            TotalMessages = await _context.Messages.CountAsync(m => m.JobId == jobId),
            UnreadCount = unreadCount
        });
    }

    /// <summary>
    /// Get all conversations for the current user
    /// </summary>
    [HttpGet("conversations")]
    [ProducesResponseType(typeof(List<ConversationSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ConversationSummaryDto>>> GetUserConversations()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        // Get all jobs where user is customer or driver
        var driver = await _context.Drivers.FirstOrDefaultAsync(d => d.UserId == userId);

        var jobIds = await _context.Jobs
            .Where(j => j.CustomerId == userId || (driver != null && j.DriverId == driver.Id))
            .Select(j => j.Id)
            .ToListAsync();

        // Get conversations (jobs with messages)
        var conversations = await _context.Messages
            .Where(m => jobIds.Contains(m.JobId))
            .GroupBy(m => m.JobId)
            .Select(g => new
            {
                JobId = g.Key,
                LastMessage = g.OrderByDescending(m => m.CreatedAt).FirstOrDefault(),
                UnreadCount = g.Count(m => m.ReceiverId == userId && !m.IsRead),
                TotalMessages = g.Count()
            })
            .ToListAsync();

        var result = new List<ConversationSummaryDto>();

        foreach (var conv in conversations)
        {
            var job = await _context.Jobs.FirstOrDefaultAsync(j => j.Id == conv.JobId);
            if (job == null) continue;

            result.Add(new ConversationSummaryDto
            {
                JobId = conv.JobId,
                JobNumber = job.JobNumber,
                JobStatus = job.Status,
                OtherPartyName = driver != null && job.DriverId == driver.Id
                    ? job.CustomerName
                    : job.DriverName ?? "Unassigned",
                LastMessage = conv.LastMessage != null ? MapToDto(conv.LastMessage) : null,
                UnreadCount = conv.UnreadCount,
                TotalMessages = conv.TotalMessages,
                LastMessageAt = conv.LastMessage?.CreatedAt
            });
        }

        return Ok(result.OrderByDescending(c => c.LastMessageAt).ToList());
    }

    /// <summary>
    /// Mark message(s) as read
    /// </summary>
    [HttpPost("{id}/mark-read")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> MarkAsRead(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var message = await _context.Messages.FirstOrDefaultAsync(m => m.Id == id);
        if (message == null)
            return NotFound();

        if (message.ReceiverId != userId)
            return Forbid();

        if (!message.IsRead)
        {
            message.IsRead = true;
            message.ReadAt = DateTime.UtcNow;
            message.Status = "read";
            message.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        return Ok();
    }

    /// <summary>
    /// Mark all messages in a conversation as read
    /// </summary>
    [HttpPost("job/{jobId}/mark-all-read")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult> MarkAllAsRead(Guid jobId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var unreadMessages = await _context.Messages
            .Where(m => m.JobId == jobId && m.ReceiverId == userId && !m.IsRead)
            .ToListAsync();

        foreach (var message in unreadMessages)
        {
            message.IsRead = true;
            message.ReadAt = DateTime.UtcNow;
            message.Status = "read";
            message.UpdatedAt = DateTime.UtcNow;
        }

        if (unreadMessages.Any())
            await _context.SaveChangesAsync();

        return Ok(new { MarkedRead = unreadMessages.Count });
    }

    private static MessageDto MapToDto(Message message)
    {
        return new MessageDto
        {
            Id = message.Id,
            JobId = message.JobId,
            SenderId = message.SenderId,
            SenderName = message.SenderName,
            SenderType = message.SenderType,
            ReceiverId = message.ReceiverId,
            ReceiverName = message.ReceiverName,
            ReceiverType = message.ReceiverType,
            Content = message.Content,
            MessageType = message.MessageType,
            Attachments = message.Attachments,
            IsRead = message.IsRead,
            ReadAt = message.ReadAt,
            Status = message.Status,
            IsSystemMessage = message.IsSystemMessage,
            CreatedAt = message.CreatedAt
        };
    }
}

#region DTOs

public class SendMessageDto
{
    public Guid JobId { get; set; }
    public required string Content { get; set; }
    public string? MessageType { get; set; }
    public string? Attachments { get; set; }
}

public class MessageDto
{
    public Guid Id { get; set; }
    public Guid JobId { get; set; }
    public string SenderId { get; set; } = "";
    public string? SenderName { get; set; }
    public string SenderType { get; set; } = "";
    public string ReceiverId { get; set; } = "";
    public string? ReceiverName { get; set; }
    public string ReceiverType { get; set; } = "";
    public string Content { get; set; } = "";
    public string MessageType { get; set; } = "";
    public string? Attachments { get; set; }
    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }
    public string Status { get; set; } = "";
    public bool IsSystemMessage { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ConversationDto
{
    public Guid JobId { get; set; }
    public string JobNumber { get; set; } = "";
    public List<MessageDto> Messages { get; set; } = new();
    public int TotalMessages { get; set; }
    public int UnreadCount { get; set; }
}

public class ConversationSummaryDto
{
    public Guid JobId { get; set; }
    public string JobNumber { get; set; } = "";
    public string JobStatus { get; set; } = "";
    public string OtherPartyName { get; set; } = "";
    public MessageDto? LastMessage { get; set; }
    public int UnreadCount { get; set; }
    public int TotalMessages { get; set; }
    public DateTime? LastMessageAt { get; set; }
}

#endregion
