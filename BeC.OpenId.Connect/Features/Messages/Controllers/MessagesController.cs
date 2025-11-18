using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Validation.AspNetCore;
using BeC.OpenId.Connect.Dto;
using BeC.OpenId.Connect.Features.Messages.Dtos;
using BeC.OpenId.Connect.Features.Messages.Models;
using BeC.OpenId.Connect.Features.Users.Dtos;
using BeC.OpenId.Connect.Features.ActivityLogs.Services.Interfaces;
using AuthRoles = BeC.OpenId.Connect.Infrastructure.Authorization.Roles;

namespace BeC.OpenId.Connect.Features.Messages.Controllers;

/// <summary>
/// In-app messaging between customers and drivers
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
[Produces("application/json")]
public class MessagesController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IActivityLogService _activityLogService;
    private readonly ILogger<MessagesController> _logger;

    public MessagesController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IActivityLogService activityLogService,
        ILogger<MessagesController> logger)
    {
        _context = context;
        _userManager = userManager;
        _activityLogService = activityLogService;
        _logger = logger;
    }

    /// <summary>
    /// Send a message to another user
    /// </summary>
    [HttpPost("send")]
    [ProducesResponseType(typeof(ChatMessage), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ChatMessage>> SendMessage([FromBody] SendMessageDto request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return NotFound("User not found");

        var recipient = await _userManager.FindByIdAsync(request.RecipientId);
        if (recipient == null)
            return NotFound("Recipient not found");

        var userRoles = await _userManager.GetRolesAsync(user);
        var recipientRoles = await _userManager.GetRolesAsync(recipient);

        var senderType = userRoles.Contains(AuthRoles.Driver) ? "driver" :
                         userRoles.Contains(AuthRoles.Admin) ? "admin" : "customer";
        var recipientType = recipientRoles.Contains(AuthRoles.Driver) ? "driver" :
                           recipientRoles.Contains(AuthRoles.Admin) ? "admin" : "customer";

        // Find or create conversation
        var conversation = await _context.Conversations
            .FirstOrDefaultAsync(c =>
                (c.User1Id == userId && c.User2Id == request.RecipientId) ||
                (c.User1Id == request.RecipientId && c.User2Id == userId));

        if (conversation == null)
        {
            conversation = new Conversation
            {
                User1Id = userId,
                User1Name = user.DisplayName ?? user.UserName ?? "Unknown",
                User1Type = senderType,
                User2Id = request.RecipientId,
                User2Name = recipient.DisplayName ?? recipient.UserName ?? "Unknown",
                User2Type = recipientType,
                JobId = request.JobId
            };
            _context.Conversations.Add(conversation);
            await _context.SaveChangesAsync();
        }

        var message = new ChatMessage
        {
            ConversationId = conversation.Id,
            SenderId = userId,
            SenderName = user.DisplayName ?? user.UserName ?? "Unknown",
            SenderType = senderType,
            RecipientId = request.RecipientId,
            RecipientName = recipient.DisplayName ?? recipient.UserName ?? "Unknown",
            RecipientType = recipientType,
            Message = request.Message,
            MessageType = request.MessageType,
            JobId = request.JobId,
            Attachment = request.Attachment != null ? JsonSerializer.Serialize(request.Attachment) : null,
            LocationData = request.LocationData != null ? JsonSerializer.Serialize(request.LocationData) : null
        };

        _context.ChatMessages.Add(message);

        // Update conversation
        conversation.LastMessage = request.Message.Length > 100 ? request.Message.Substring(0, 100) + "..." : request.Message;
        conversation.LastMessageAt = DateTime.UtcNow;
        conversation.LastMessageSenderId = userId;
        conversation.UpdatedAt = DateTime.UtcNow;

        // Increment unread count for recipient
        if (conversation.User1Id == request.RecipientId)
            conversation.User1UnreadCount++;
        else
            conversation.User2UnreadCount++;

        await _context.SaveChangesAsync();

        await _activityLogService.LogActivityAsync(
            userId,
            "message_sent",
            "ChatMessage",
            message.Id.ToString(),
            $"To: {recipient.DisplayName ?? recipient.UserName}",
            $"Sent message"
        );

        return CreatedAtAction(nameof(GetMessage), new { id = message.Id }, message);
    }

    /// <summary>
    /// Get a specific message
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ChatMessage), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ChatMessage>> GetMessage(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var message = await _context.ChatMessages.FindAsync(id);
        if (message == null)
            return NotFound();

        // Verify user is part of conversation
        if (message.SenderId != userId && message.RecipientId != userId)
            return Forbid();

        // Mark as read if recipient is viewing
        if (message.RecipientId == userId && !message.IsRead)
        {
            message.IsRead = true;
            message.ReadAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        return Ok(message);
    }

    /// <summary>
    /// Get all conversations for the current user
    /// </summary>
    [HttpGet("conversations")]
    [ProducesResponseType(typeof(List<Conversation>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<Conversation>>> GetConversations(
        [FromQuery] string? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var query = _context.Conversations
            .Where(c => c.User1Id == userId || c.User2Id == userId);

        if (!string.IsNullOrEmpty(status))
            query = query.Where(c => c.Status == status);

        var conversations = await query
            .OrderByDescending(c => c.LastMessageAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Ok(conversations);
    }

    /// <summary>
    /// Get messages for a specific conversation
    /// </summary>
    [HttpGet("conversations/{conversationId}/messages")]
    [ProducesResponseType(typeof(List<ChatMessage>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<ChatMessage>>> GetConversationMessages(
        Guid conversationId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var conversation = await _context.Conversations.FindAsync(conversationId);
        if (conversation == null)
            return NotFound("Conversation not found");

        // Verify user is part of conversation
        if (conversation.User1Id != userId && conversation.User2Id != userId)
            return Forbid();

        var messages = await _context.ChatMessages
            .Where(m => m.ConversationId == conversationId && !m.IsDeleted)
            .OrderByDescending(m => m.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        // Mark messages as read
        var unreadMessages = messages.Where(m => m.RecipientId == userId && !m.IsRead).ToList();
        foreach (var message in unreadMessages)
        {
            message.IsRead = true;
            message.ReadAt = DateTime.UtcNow;
        }

        // Update unread count in conversation
        if (unreadMessages.Any())
        {
            if (conversation.User1Id == userId)
                conversation.User1UnreadCount = Math.Max(0, conversation.User1UnreadCount - unreadMessages.Count);
            else
                conversation.User2UnreadCount = Math.Max(0, conversation.User2UnreadCount - unreadMessages.Count);

            await _context.SaveChangesAsync();
        }

        // Return in chronological order (oldest first)
        messages.Reverse();

        return Ok(messages);
    }

    /// <summary>
    /// Mark conversation messages as read
    /// </summary>
    [HttpPost("conversations/{conversationId}/mark-read")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> MarkConversationAsRead(Guid conversationId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var conversation = await _context.Conversations.FindAsync(conversationId);
        if (conversation == null)
            return NotFound("Conversation not found");

        // Verify user is part of conversation
        if (conversation.User1Id != userId && conversation.User2Id != userId)
            return Forbid();

        var unreadMessages = await _context.ChatMessages
            .Where(m => m.ConversationId == conversationId && m.RecipientId == userId && !m.IsRead)
            .ToListAsync();

        foreach (var message in unreadMessages)
        {
            message.IsRead = true;
            message.ReadAt = DateTime.UtcNow;
        }

        // Reset unread count
        if (conversation.User1Id == userId)
            conversation.User1UnreadCount = 0;
        else
            conversation.User2UnreadCount = 0;

        await _context.SaveChangesAsync();

        return Ok(new { markedRead = unreadMessages.Count });
    }

    /// <summary>
    /// Get unread message count for current user
    /// </summary>
    [HttpGet("unread-count")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<ActionResult<object>> GetUnreadCount()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var totalUnread = await _context.ChatMessages
            .CountAsync(m => m.RecipientId == userId && !m.IsRead && !m.IsDeleted);

        var conversationsWithUnread = await _context.Conversations
            .Where(c => c.User1Id == userId || c.User2Id == userId)
            .Where(c => (c.User1Id == userId && c.User1UnreadCount > 0) ||
                       (c.User2Id == userId && c.User2UnreadCount > 0))
            .CountAsync();

        return Ok(new
        {
            totalUnreadMessages = totalUnread,
            conversationsWithUnread
        });
    }

    /// <summary>
    /// Archive a conversation
    /// </summary>
    [HttpPost("conversations/{conversationId}/archive")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> ArchiveConversation(Guid conversationId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var conversation = await _context.Conversations.FindAsync(conversationId);
        if (conversation == null)
            return NotFound("Conversation not found");

        // Verify user is part of conversation
        if (conversation.User1Id != userId && conversation.User2Id != userId)
            return Forbid();

        conversation.Status = "archived";
        conversation.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        await _activityLogService.LogActivityAsync(
            userId,
            "conversation_archived",
            "Conversation",
            conversation.Id.ToString(),
            $"Conversation archived",
            $"Archived conversation"
        );

        return Ok(new { message = "Conversation archived successfully" });
    }

    /// <summary>
    /// Delete a message (soft delete)
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteMessage(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var message = await _context.ChatMessages.FindAsync(id);
        if (message == null)
            return NotFound();

        // Only sender can delete their message
        if (message.SenderId != userId)
            return Forbid();

        message.IsDeleted = true;
        message.DeletedAt = DateTime.UtcNow;
        message.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(new { message = "Message deleted successfully" });
    }

    /// <summary>
    /// Get messages for a specific job (Admin, Customer, or assigned Driver)
    /// </summary>
    [HttpGet("job/{jobId}")]
    [ProducesResponseType(typeof(List<ChatMessage>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ChatMessage>>> GetJobMessages(Guid jobId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return NotFound("User not found");

        var userRoles = await _userManager.GetRolesAsync(user);
        var isAdmin = userRoles.Contains(AuthRoles.Admin) || userRoles.Contains(AuthRoles.SuperAdmin);

        // Verify access to job
        var job = await _context.Jobs.FindAsync(jobId);
        if (job == null)
            return NotFound("Job not found");

        var hasAccess = isAdmin || job.CustomerId == userId;

        if (!hasAccess)
        {
            var driver = await _context.Drivers.FirstOrDefaultAsync(d => d.Id == job.DriverId);
            if (driver?.UserId == userId)
                hasAccess = true;
        }

        if (!hasAccess)
            return Forbid();

        var messages = await _context.ChatMessages
            .Where(m => m.JobId == jobId && !m.IsDeleted)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync();

        return Ok(messages);
    }
}
