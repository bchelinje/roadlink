using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Validation.AspNetCore;
using BeC.OpenId.Connect.Dto;
using BeC.OpenId.Connect.Features.Complaints.Dtos;
using BeC.OpenId.Connect.Features.Complaints.Models;
using BeC.OpenId.Connect.Features.Users.Dtos;
using BeC.OpenId.Connect.Features.ActivityLogs.Services.Interfaces;
using AuthRoles = BeC.OpenId.Connect.Infrastructure.Authorization.Roles;

namespace BeC.OpenId.Connect.Features.Complaints.Controllers;

/// <summary>
/// Complaint management for formal disputes and issues
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
[Produces("application/json")]
public class ComplaintsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IActivityLogService _activityLogService;
    private readonly ILogger<ComplaintsController> _logger;

    public ComplaintsController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IActivityLogService activityLogService,
        ILogger<ComplaintsController> logger)
    {
        _context = context;
        _userManager = userManager;
        _activityLogService = activityLogService;
        _logger = logger;
    }

    /// <summary>
    /// File a new complaint
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(Complaint), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Complaint>> CreateComplaint([FromBody] CreateComplaintDto request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return NotFound("User not found");

        var userRoles = await _userManager.GetRolesAsync(user);
        var complainantType = userRoles.Contains(AuthRoles.Driver) ? "driver" : "customer";

        // Generate unique complaint number
        var complaintNumber = $"CMP-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";

        var complaint = new Complaint
        {
            ComplaintNumber = complaintNumber,
            ComplainantId = userId,
            ComplainantName = user.DisplayName ?? user.UserName ?? "Unknown",
            ComplainantType = complainantType,
            ComplainantEmail = user.Email,
            SubjectId = request.SubjectId,
            SubjectType = request.SubjectType,
            Title = request.Title,
            Description = request.Description,
            Category = request.Category,
            Severity = request.Severity,
            JobId = request.JobId,
            PaymentId = request.PaymentId,
            IncidentDate = request.IncidentDate,
            IncidentLocation = request.IncidentLocation,
            Evidence = request.Evidence != null ? JsonSerializer.Serialize(request.Evidence) : null,
            Witnesses = request.Witnesses != null ? JsonSerializer.Serialize(request.Witnesses) : null,
            LastActivityAt = DateTime.UtcNow
        };

        // Get subject name if available
        if (!string.IsNullOrEmpty(request.SubjectId))
        {
            var subject = await _userManager.FindByIdAsync(request.SubjectId);
            if (subject != null)
            {
                complaint.SubjectName = subject.DisplayName ?? subject.UserName ?? "Unknown";
            }
        }

        _context.Complaints.Add(complaint);
        await _context.SaveChangesAsync();

        await _activityLogService.LogActivityAsync(
            userId,
            "complaint_filed",
            "Complaint",
            complaint.Id.ToString(),
            $"Complaint: {complaint.Title}",
            $"Filed complaint {complaintNumber} - {complaint.Category}"
        );

        return CreatedAtAction(nameof(GetComplaint), new { id = complaint.Id }, complaint);
    }

    /// <summary>
    /// Get a complaint by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(Complaint), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Complaint>> GetComplaint(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return NotFound("User not found");

        var userRoles = await _userManager.GetRolesAsync(user);
        var isAdmin = userRoles.Contains(AuthRoles.Admin) || userRoles.Contains(AuthRoles.SuperAdmin);

        var complaint = await _context.Complaints.FindAsync(id);
        if (complaint == null)
            return NotFound();

        // Users can only view complaints they're involved in, admins can see all
        if (!isAdmin && complaint.ComplainantId != userId && complaint.SubjectId != userId)
            return Forbid();

        return Ok(complaint);
    }

    /// <summary>
    /// Get all complaints (filtered)
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<ActionResult<object>> GetComplaints(
        [FromQuery] string? status = null,
        [FromQuery] string? severity = null,
        [FromQuery] string? category = null,
        [FromQuery] bool? isEscalated = null,
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

        var query = _context.Complaints.AsQueryable();

        // Non-admin users can only see their own complaints or complaints about them
        if (!isAdmin)
        {
            query = query.Where(c => c.ComplainantId == userId || c.SubjectId == userId);
        }

        if (!string.IsNullOrEmpty(status))
            query = query.Where(c => c.Status == status);

        if (!string.IsNullOrEmpty(severity))
            query = query.Where(c => c.Severity == severity);

        if (!string.IsNullOrEmpty(category))
            query = query.Where(c => c.Category == category);

        if (isEscalated.HasValue)
            query = query.Where(c => c.IsEscalated == isEscalated.Value);

        var total = await query.CountAsync();
        var complaints = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Ok(new
        {
            data = complaints,
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
    /// Update a complaint (Admin only)
    /// </summary>
    [HttpPatch("{id}")]
    [Authorize(Roles = $"{AuthRoles.Admin},{AuthRoles.SuperAdmin}")]
    [ProducesResponseType(typeof(Complaint), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Complaint>> UpdateComplaint(Guid id, [FromBody] UpdateComplaintDto request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var complaint = await _context.Complaints.FindAsync(id);
        if (complaint == null)
            return NotFound();

        if (request.Status != null)
        {
            complaint.Status = request.Status;
            if (request.Status == "resolved")
            {
                complaint.ResolvedAt = DateTime.UtcNow;
                complaint.ResolvedBy = userId;
            }
        }

        if (request.Severity != null)
            complaint.Severity = request.Severity;

        if (request.AssignedToId != null)
        {
            complaint.AssignedToId = request.AssignedToId;
            var assignedUser = await _userManager.FindByIdAsync(request.AssignedToId);
            complaint.AssignedToName = assignedUser?.DisplayName ?? assignedUser?.UserName ?? "Unknown";
            complaint.AssignedAt = DateTime.UtcNow;
        }

        if (request.InvestigationNotes != null)
            complaint.InvestigationNotes = request.InvestigationNotes;

        if (request.Resolution != null)
            complaint.Resolution = request.Resolution;

        if (request.ResolutionType != null)
            complaint.ResolutionType = request.ResolutionType;

        if (request.ActionsTaken != null)
            complaint.ActionsTaken = JsonSerializer.Serialize(request.ActionsTaken);

        if (request.IsEscalated.HasValue && request.IsEscalated.Value && !complaint.IsEscalated)
        {
            complaint.IsEscalated = true;
            complaint.EscalatedAt = DateTime.UtcNow;
            complaint.EscalationReason = request.EscalationReason;
        }

        complaint.UpdatedAt = DateTime.UtcNow;
        complaint.LastActivityAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        await _activityLogService.LogActivityAsync(
            userId,
            "complaint_updated",
            "Complaint",
            complaint.Id.ToString(),
            $"Complaint: {complaint.Title}",
            $"Updated complaint {complaint.ComplaintNumber}"
        );

        return Ok(complaint);
    }

    /// <summary>
    /// Resolve a complaint (Admin only)
    /// </summary>
    [HttpPost("{id}/resolve")]
    [Authorize(Roles = $"{AuthRoles.Admin},{AuthRoles.SuperAdmin}")]
    [ProducesResponseType(typeof(Complaint), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Complaint>> ResolveComplaint(
        Guid id,
        [FromBody] ResolveComplaintDto request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var complaint = await _context.Complaints.FindAsync(id);
        if (complaint == null)
            return NotFound();

        complaint.Status = "resolved";
        complaint.Resolution = request.Resolution;
        complaint.ResolutionType = request.ResolutionType;
        complaint.ActionsTaken = request.ActionsTaken != null ? JsonSerializer.Serialize(request.ActionsTaken) : null;
        complaint.ResolvedAt = DateTime.UtcNow;
        complaint.ResolvedBy = userId;
        complaint.UpdatedAt = DateTime.UtcNow;
        complaint.LastActivityAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        await _activityLogService.LogActivityAsync(
            userId,
            "complaint_resolved",
            "Complaint",
            complaint.Id.ToString(),
            $"Complaint: {complaint.Title}",
            $"Resolved complaint {complaint.ComplaintNumber}"
        );

        return Ok(complaint);
    }

    /// <summary>
    /// Escalate a complaint (Admin only)
    /// </summary>
    [HttpPost("{id}/escalate")]
    [Authorize(Roles = $"{AuthRoles.Admin},{AuthRoles.SuperAdmin}")]
    [ProducesResponseType(typeof(Complaint), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Complaint>> EscalateComplaint(
        Guid id,
        [FromBody] EscalateComplaintDto request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var complaint = await _context.Complaints.FindAsync(id);
        if (complaint == null)
            return NotFound();

        complaint.IsEscalated = true;
        complaint.EscalatedAt = DateTime.UtcNow;
        complaint.EscalationReason = request.Reason;
        complaint.Status = "escalated";
        complaint.UpdatedAt = DateTime.UtcNow;
        complaint.LastActivityAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        await _activityLogService.LogActivityAsync(
            userId,
            "complaint_escalated",
            "Complaint",
            complaint.Id.ToString(),
            $"Complaint: {complaint.Title}",
            $"Escalated complaint {complaint.ComplaintNumber}"
        );

        return Ok(complaint);
    }

    /// <summary>
    /// Get complaint statistics (Admin only)
    /// </summary>
    [HttpGet("statistics")]
    [Authorize(Roles = $"{AuthRoles.Admin},{AuthRoles.SuperAdmin}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<ActionResult<object>> GetComplaintStatistics()
    {
        var totalComplaints = await _context.Complaints.CountAsync();
        var submittedComplaints = await _context.Complaints.CountAsync(c => c.Status == "submitted");
        var underReviewComplaints = await _context.Complaints.CountAsync(c => c.Status == "under_review");
        var investigatingComplaints = await _context.Complaints.CountAsync(c => c.Status == "investigating");
        var resolvedComplaints = await _context.Complaints.CountAsync(c => c.Status == "resolved");
        var escalatedComplaints = await _context.Complaints.CountAsync(c => c.IsEscalated);

        var complaintsByCategory = await _context.Complaints
            .GroupBy(c => c.Category)
            .Select(g => new { category = g.Key, count = g.Count() })
            .ToListAsync();

        var complaintsBySeverity = await _context.Complaints
            .GroupBy(c => c.Severity)
            .Select(g => new { severity = g.Key, count = g.Count() })
            .ToListAsync();

        var averageResolutionTime = await _context.Complaints
            .Where(c => c.ResolvedAt.HasValue)
            .Select(c => EF.Functions.DateDiffMinute(c.CreatedAt, c.ResolvedAt!.Value))
            .AverageAsync();

        return Ok(new
        {
            totalComplaints,
            submittedComplaints,
            underReviewComplaints,
            investigatingComplaints,
            resolvedComplaints,
            escalatedComplaints,
            complaintsByCategory,
            complaintsBySeverity,
            averageResolutionTimeMinutes = averageResolutionTime != null ? Math.Round((double)averageResolutionTime, 2) : 0
        });
    }
}

public class ResolveComplaintDto
{
    [Required]
    public required string Resolution { get; set; }

    [Required]
    public required string ResolutionType { get; set; }

    public List<string>? ActionsTaken { get; set; }
}

public class EscalateComplaintDto
{
    [Required]
    public required string Reason { get; set; }
}
