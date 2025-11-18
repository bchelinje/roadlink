using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Validation.AspNetCore;
using BeC.OpenId.Connect.Dto;
using BeC.OpenId.Connect.Features.Support.Dtos;
using BeC.OpenId.Connect.Features.Support.Models;
using BeC.OpenId.Connect.Features.Users.Dtos;
using BeC.OpenId.Connect.Features.ActivityLogs.Services.Interfaces;
using AuthRoles = BeC.OpenId.Connect.Infrastructure.Authorization.Roles;

namespace BeC.OpenId.Connect.Features.Support.Controllers;

/// <summary>
/// Support ticket management for customer support
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
[Produces("application/json")]
public class SupportController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IActivityLogService _activityLogService;
    private readonly ILogger<SupportController> _logger;

    public SupportController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IActivityLogService activityLogService,
        ILogger<SupportController> logger)
    {
        _context = context;
        _userManager = userManager;
        _activityLogService = activityLogService;
        _logger = logger;
    }

    /// <summary>
    /// Create a new support ticket
    /// </summary>
    [HttpPost("tickets")]
    [ProducesResponseType(typeof(SupportTicket), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SupportTicket>> CreateTicket([FromBody] CreateTicketDto request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return NotFound("User not found");

        // Generate unique ticket number
        var ticketNumber = $"TKT-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";

        var ticket = new SupportTicket
        {
            TicketNumber = ticketNumber,
            UserId = userId,
            UserName = user.DisplayName ?? user.UserName ?? "Unknown",
            UserEmail = user.Email ?? "no-email@example.com",
            Subject = request.Subject,
            Description = request.Description,
            Category = request.Category,
            Priority = request.Priority,
            JobId = request.JobId,
            DriverId = request.DriverId,
            PaymentId = request.PaymentId,
            Attachments = request.Attachments != null ? JsonSerializer.Serialize(request.Attachments) : null,
            Tags = request.Tags != null ? JsonSerializer.Serialize(request.Tags) : null,
            LastActivityAt = DateTime.UtcNow
        };

        _context.SupportTickets.Add(ticket);
        await _context.SaveChangesAsync();

        await _activityLogService.LogActivityAsync(
            userId,
            "support_ticket_created",
            "SupportTicket",
            ticket.Id.ToString(),
            $"Ticket: {ticket.Subject}",
            $"Created support ticket {ticketNumber}"
        );

        return CreatedAtAction(nameof(GetTicket), new { id = ticket.Id }, ticket);
    }

    /// <summary>
    /// Get a support ticket by ID
    /// </summary>
    [HttpGet("tickets/{id}")]
    [ProducesResponseType(typeof(SupportTicket), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SupportTicket>> GetTicket(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return NotFound("User not found");

        var userRoles = await _userManager.GetRolesAsync(user);
        var isAdmin = userRoles.Contains(AuthRoles.Admin) || userRoles.Contains(AuthRoles.SuperAdmin);

        var ticket = await _context.SupportTickets.FindAsync(id);
        if (ticket == null)
            return NotFound();

        // Users can only view their own tickets unless they're admin
        if (!isAdmin && ticket.UserId != userId)
            return Forbid();

        return Ok(ticket);
    }

    /// <summary>
    /// Get all support tickets (with filtering)
    /// </summary>
    [HttpGet("tickets")]
    [ProducesResponseType(typeof(List<SupportTicket>), StatusCodes.Status200OK)]
    public async Task<ActionResult<object>> GetTickets(
        [FromQuery] string? status = null,
        [FromQuery] string? priority = null,
        [FromQuery] string? category = null,
        [FromQuery] string? assignedTo = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return NotFound("User not found");

        var userRoles = await _userManager.GetRolesAsync(user);
        var isAdmin = userRoles.Contains(AuthRoles.Admin) || userRoles.Contains(AuthRoles.SuperAdmin);

        var query = _context.SupportTickets.AsQueryable();

        // Non-admin users can only see their own tickets
        if (!isAdmin)
        {
            query = query.Where(t => t.UserId == userId);
        }

        if (!string.IsNullOrEmpty(status))
            query = query.Where(t => t.Status == status);

        if (!string.IsNullOrEmpty(priority))
            query = query.Where(t => t.Priority == priority);

        if (!string.IsNullOrEmpty(category))
            query = query.Where(t => t.Category == category);

        if (!string.IsNullOrEmpty(assignedTo) && isAdmin)
            query = query.Where(t => t.AssignedToId == assignedTo);

        var total = await query.CountAsync();
        var tickets = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Ok(new
        {
            data = tickets,
            pagination = new
            {
                page,
                pageSize,
                total,
                totalPages = (int)Math.Ceiling(total / (double)pageSize)
            }
        });
    }

    /// <summary>
    /// Update a support ticket (Admin only)
    /// </summary>
    [HttpPatch("tickets/{id}")]
    [Authorize(Roles = $"{AuthRoles.Admin},{AuthRoles.SuperAdmin}")]
    [ProducesResponseType(typeof(SupportTicket), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SupportTicket>> UpdateTicket(Guid id, [FromBody] UpdateTicketDto request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var ticket = await _context.SupportTickets.FindAsync(id);
        if (ticket == null)
            return NotFound();

        if (request.Status != null)
        {
            ticket.Status = request.Status;
            if (request.Status == "resolved")
            {
                ticket.ResolvedAt = DateTime.UtcNow;
                ticket.ResolvedBy = userId;
            }
            else if (request.Status == "closed")
            {
                ticket.ClosedAt = DateTime.UtcNow;
                ticket.ClosedBy = userId;
            }
        }

        if (request.Priority != null)
            ticket.Priority = request.Priority;

        if (request.AssignedToId != null)
        {
            ticket.AssignedToId = request.AssignedToId;
            var assignedUser = await _userManager.FindByIdAsync(request.AssignedToId);
            ticket.AssignedToName = assignedUser?.DisplayName ?? assignedUser?.UserName ?? "Unknown";
            ticket.AssignedAt = DateTime.UtcNow;
        }

        if (request.Resolution != null)
            ticket.Resolution = request.Resolution;

        if (request.InternalNotes != null)
            ticket.InternalNotes = request.InternalNotes;

        ticket.UpdatedAt = DateTime.UtcNow;
        ticket.LastActivityAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        await _activityLogService.LogActivityAsync(
            userId,
            "support_ticket_updated",
            "SupportTicket",
            ticket.Id.ToString(),
            $"Ticket: {ticket.Subject}",
            $"Updated support ticket {ticket.TicketNumber}"
        );

        return Ok(ticket);
    }

    /// <summary>
    /// Add a message to a support ticket
    /// </summary>
    [HttpPost("tickets/{ticketId}/messages")]
    [ProducesResponseType(typeof(TicketMessage), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TicketMessage>> AddTicketMessage(Guid ticketId, [FromBody] AddTicketMessageDto request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return NotFound("User not found");

        var ticket = await _context.SupportTickets.FindAsync(ticketId);
        if (ticket == null)
            return NotFound("Ticket not found");

        var userRoles = await _userManager.GetRolesAsync(user);
        var isAdmin = userRoles.Contains(AuthRoles.Admin) || userRoles.Contains(AuthRoles.SuperAdmin);

        // Verify access - user must own ticket or be admin
        if (!isAdmin && ticket.UserId != userId)
            return Forbid();

        // Only admins can create internal messages
        if (request.IsInternal && !isAdmin)
            return BadRequest("Only administrators can create internal messages");

        var senderType = isAdmin ? "support_agent" : "customer";

        var message = new TicketMessage
        {
            TicketId = ticketId,
            SenderId = userId,
            SenderName = user.DisplayName ?? user.UserName ?? "Unknown",
            SenderType = senderType,
            Message = request.Message,
            Attachments = request.Attachments != null ? JsonSerializer.Serialize(request.Attachments) : null,
            IsInternal = request.IsInternal
        };

        _context.TicketMessages.Add(message);

        // Update ticket activity and first response time
        ticket.LastActivityAt = DateTime.UtcNow;
        ticket.UpdatedAt = DateTime.UtcNow;

        if (isAdmin && ticket.FirstResponseAt == null)
        {
            ticket.FirstResponseAt = DateTime.UtcNow;
            ticket.FirstResponseTimeMinutes = (int)(DateTime.UtcNow - ticket.CreatedAt).TotalMinutes;
        }

        await _context.SaveChangesAsync();

        await _activityLogService.LogActivityAsync(
            userId,
            "ticket_message_added",
            "TicketMessage",
            message.Id.ToString(),
            $"Ticket: {ticket.TicketNumber}",
            $"Added message to support ticket"
        );

        return CreatedAtAction(nameof(GetTicketMessages), new { ticketId = ticketId }, message);
    }

    /// <summary>
    /// Get all messages for a support ticket
    /// </summary>
    [HttpGet("tickets/{ticketId}/messages")]
    [ProducesResponseType(typeof(List<TicketMessage>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<TicketMessage>>> GetTicketMessages(Guid ticketId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return NotFound("User not found");

        var ticket = await _context.SupportTickets.FindAsync(ticketId);
        if (ticket == null)
            return NotFound("Ticket not found");

        var userRoles = await _userManager.GetRolesAsync(user);
        var isAdmin = userRoles.Contains(AuthRoles.Admin) || userRoles.Contains(AuthRoles.SuperAdmin);

        // Verify access
        if (!isAdmin && ticket.UserId != userId)
            return Forbid();

        var query = _context.TicketMessages
            .Where(m => m.TicketId == ticketId);

        // Hide internal messages from non-admin users
        if (!isAdmin)
        {
            query = query.Where(m => !m.IsInternal);
        }

        var messages = await query
            .OrderBy(m => m.CreatedAt)
            .ToListAsync();

        return Ok(messages);
    }

    /// <summary>
    /// Get support ticket statistics (Admin only)
    /// </summary>
    [HttpGet("tickets/statistics")]
    [Authorize(Roles = $"{AuthRoles.Admin},{AuthRoles.SuperAdmin}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<ActionResult<object>> GetTicketStatistics()
    {
        var totalTickets = await _context.SupportTickets.CountAsync();
        var openTickets = await _context.SupportTickets.CountAsync(t => t.Status == "open");
        var inProgressTickets = await _context.SupportTickets.CountAsync(t => t.Status == "in_progress");
        var resolvedTickets = await _context.SupportTickets.CountAsync(t => t.Status == "resolved");
        var closedTickets = await _context.SupportTickets.CountAsync(t => t.Status == "closed");

        var averageResolutionTime = await _context.SupportTickets
            .Where(t => t.ResolutionTimeMinutes.HasValue)
            .AverageAsync(t => (double?)t.ResolutionTimeMinutes) ?? 0;

        var averageFirstResponseTime = await _context.SupportTickets
            .Where(t => t.FirstResponseTimeMinutes.HasValue)
            .AverageAsync(t => (double?)t.FirstResponseTimeMinutes) ?? 0;

        var ticketsByCategory = await _context.SupportTickets
            .GroupBy(t => t.Category)
            .Select(g => new { category = g.Key, count = g.Count() })
            .ToListAsync();

        var ticketsByPriority = await _context.SupportTickets
            .GroupBy(t => t.Priority)
            .Select(g => new { priority = g.Key, count = g.Count() })
            .ToListAsync();

        var satisfactionRating = await _context.SupportTickets
            .Where(t => t.CustomerSatisfactionRating.HasValue)
            .AverageAsync(t => (double?)t.CustomerSatisfactionRating) ?? 0;

        return Ok(new
        {
            totalTickets,
            openTickets,
            inProgressTickets,
            resolvedTickets,
            closedTickets,
            averageResolutionTimeMinutes = Math.Round(averageResolutionTime, 2),
            averageFirstResponseTimeMinutes = Math.Round(averageFirstResponseTime, 2),
            averageSatisfactionRating = Math.Round(satisfactionRating, 2),
            ticketsByCategory,
            ticketsByPriority
        });
    }

    /// <summary>
    /// Close a support ticket
    /// </summary>
    [HttpPost("tickets/{id}/close")]
    [ProducesResponseType(typeof(SupportTicket), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SupportTicket>> CloseTicket(Guid id, [FromBody] RateTicketDto? rating = null)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return NotFound("User not found");

        var ticket = await _context.SupportTickets.FindAsync(id);
        if (ticket == null)
            return NotFound();

        var userRoles = await _userManager.GetRolesAsync(user);
        var isAdmin = userRoles.Contains(AuthRoles.Admin) || userRoles.Contains(AuthRoles.SuperAdmin);

        // Users can close their own tickets, admins can close any
        if (!isAdmin && ticket.UserId != userId)
            return Forbid();

        ticket.Status = "closed";
        ticket.ClosedAt = DateTime.UtcNow;
        ticket.ClosedBy = userId;
        ticket.UpdatedAt = DateTime.UtcNow;

        if (rating != null)
        {
            ticket.CustomerSatisfactionRating = rating.Rating;
            ticket.CustomerFeedback = rating.Feedback;
        }

        if (ticket.ResolvedAt.HasValue)
        {
            ticket.ResolutionTimeMinutes = (int)(DateTime.UtcNow - ticket.CreatedAt).TotalMinutes;
        }

        await _context.SaveChangesAsync();

        await _activityLogService.LogActivityAsync(
            userId,
            "support_ticket_closed",
            "SupportTicket",
            ticket.Id.ToString(),
            $"Ticket: {ticket.Subject}",
            $"Closed support ticket {ticket.TicketNumber}"
        );

        return Ok(ticket);
    }
}

public class RateTicketDto
{
    [Range(1, 5)]
    public int Rating { get; set; }

    public string? Feedback { get; set; }
}
