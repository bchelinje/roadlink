using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Validation.AspNetCore;
using BeC.OpenId.Connect.Dto;
using BeC.OpenId.Connect.Features.Users.Dtos;
using BeC.OpenId.Connect.Features.DataPrivacy.Dtos;
using BeC.OpenId.Connect.Features.DataPrivacy.Models;
using BeC.OpenId.Connect.Features.DataPrivacy.Services;
using BeC.OpenId.Connect.Features.ActivityLogs.Services.Interfaces;
using AuthRoles = BeC.OpenId.Connect.Infrastructure.Authorization.Roles;

namespace BeC.OpenId.Connect.Features.DataPrivacy.Controllers;

/// <summary>
/// GDPR-compliant data privacy endpoints for user data export, deletion, and anonymization
/// Implements GDPR Article 17 (Right to Erasure) and Article 20 (Right to Data Portability)
/// </summary>
[ApiController]
[Route("api/data-privacy")]
[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
[Produces("application/json")]
public class DataPrivacyController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IDataAnonymizationService _anonymizationService;
    private readonly IActivityLogService _activityLogService;
    private readonly ILogger<DataPrivacyController> _logger;

    public DataPrivacyController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IDataAnonymizationService anonymizationService,
        IActivityLogService activityLogService,
        ILogger<DataPrivacyController> logger)
    {
        _context = context;
        _userManager = userManager;
        _anonymizationService = anonymizationService;
        _activityLogService = activityLogService;
        _logger = logger;
    }

    #region User Self-Service Endpoints

    /// <summary>
    /// Export all user data in a portable format (GDPR Article 20 - Right to Data Portability)
    /// </summary>
    [HttpGet("export")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<object>> ExportMyData()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var (success, errorMessage, data) = await _anonymizationService.ExportUserDataAsync(userId);

        if (!success)
            return NotFound(new { message = errorMessage });

        await _activityLogService.LogActivityAsync(
            userId,
            "data_export_requested",
            "User",
            userId,
            "User data exported",
            "User exported their personal data (GDPR Article 20)"
        );

        return Ok(new
        {
            message = "Your data has been exported successfully",
            exportDate = DateTime.UtcNow,
            data
        });
    }

    /// <summary>
    /// Get a summary of all data related to the current user
    /// </summary>
    [HttpGet("my-data-summary")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<ActionResult<object>> GetMyDataSummary()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var summary = await _anonymizationService.GetUserDataSummaryAsync(userId);

        return Ok(new
        {
            userId,
            dataSummary = summary,
            totalRecords = summary.Values.Sum()
        });
    }

    /// <summary>
    /// Request account deletion (GDPR Article 17 - Right to Erasure)
    /// </summary>
    [HttpPost("request-deletion")]
    [ProducesResponseType(typeof(DataDeletionRequest), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<DataDeletionRequest>> RequestAccountDeletion([FromBody] RequestDataDeletionDto request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return NotFound("User not found");

        // Check if there's already a pending request
        var existingRequest = await _context.DataDeletionRequests
            .FirstOrDefaultAsync(r => r.UserId == userId && r.Status == "pending");

        if (existingRequest != null)
            return BadRequest(new { message = "You already have a pending deletion request" });

        var userRoles = await _userManager.GetRolesAsync(user);
        var userType = userRoles.Contains(AuthRoles.Driver) ? "driver" : "customer";

        // Get data summary
        var dataSummary = await _anonymizationService.GetUserDataSummaryAsync(userId);

        var deletionRequest = new DataDeletionRequest
        {
            UserId = userId,
            UserName = user.DisplayName ?? user.UserName ?? "Unknown",
            UserEmail = user.Email ?? "unknown@example.com",
            UserType = userType,
            Reason = request.Reason,
            DeletionType = request.DeletionType,
            DataExportRequested = request.RequestDataExport,
            GracePeriodDays = request.GracePeriodDays ?? 30,
            ScheduledDeletionDate = DateTime.UtcNow.AddDays(request.GracePeriodDays ?? 30),
            RelatedJobsCount = dataSummary.GetValueOrDefault("Jobs_Customer", 0) + dataSummary.GetValueOrDefault("Jobs_Driver", 0),
            RelatedPaymentsCount = dataSummary.GetValueOrDefault("Payments", 0),
            RelatedReviewsCount = dataSummary.GetValueOrDefault("Reviews", 0),
            RelatedMessagesCount = dataSummary.GetValueOrDefault("Messages", 0),
            ConfirmationToken = Guid.NewGuid().ToString()
        };

        _context.DataDeletionRequests.Add(deletionRequest);
        await _context.SaveChangesAsync();

        await _activityLogService.LogActivityAsync(
            userId,
            "deletion_request_created",
            "DataDeletionRequest",
            deletionRequest.Id.ToString(),
            "Account deletion requested",
            $"User requested account deletion (GDPR Article 17) - {request.DeletionType}",
            "WARNING"
        );

        return CreatedAtAction(nameof(GetMyDeletionRequests), new { }, deletionRequest);
    }

    /// <summary>
    /// Get my deletion requests
    /// </summary>
    [HttpGet("my-deletion-requests")]
    [ProducesResponseType(typeof(List<DataDeletionRequest>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<DataDeletionRequest>>> GetMyDeletionRequests()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var requests = await _context.DataDeletionRequests
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        return Ok(requests);
    }

    /// <summary>
    /// Confirm account deletion request
    /// </summary>
    [HttpPost("confirm-deletion/{requestId}")]
    [ProducesResponseType(typeof(DataDeletionRequest), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DataDeletionRequest>> ConfirmDeletion(Guid requestId, [FromBody] ConfirmDeletionDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var request = await _context.DataDeletionRequests.FindAsync(requestId);
        if (request == null || request.UserId != userId)
            return NotFound("Deletion request not found");

        if (request.Status != "pending")
            return BadRequest(new { message = "This request cannot be confirmed" });

        if (request.ConfirmationToken != dto.ConfirmationToken)
            return BadRequest(new { message = "Invalid confirmation token" });

        request.UserConfirmed = true;
        request.ConfirmedAt = DateTime.UtcNow;
        request.Status = "approved";
        request.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        await _activityLogService.LogActivityAsync(
            userId,
            "deletion_request_confirmed",
            "DataDeletionRequest",
            request.Id.ToString(),
            "Deletion request confirmed",
            "User confirmed account deletion request",
            "WARNING"
        );

        return Ok(request);
    }

    /// <summary>
    /// Cancel a pending deletion request
    /// </summary>
    [HttpPost("cancel-deletion/{requestId}")]
    [ProducesResponseType(typeof(DataDeletionRequest), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DataDeletionRequest>> CancelDeletion(Guid requestId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var request = await _context.DataDeletionRequests.FindAsync(requestId);
        if (request == null || request.UserId != userId)
            return NotFound("Deletion request not found");

        if (request.Status != "pending" && request.Status != "approved")
            return BadRequest(new { message = "This request cannot be cancelled" });

        request.Status = "cancelled";
        request.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        await _activityLogService.LogActivityAsync(
            userId,
            "deletion_request_cancelled",
            "DataDeletionRequest",
            request.Id.ToString(),
            "Deletion request cancelled",
            "User cancelled their account deletion request"
        );

        return Ok(request);
    }

    #endregion

    #region Admin Endpoints

    /// <summary>
    /// Get all deletion requests (Admin only)
    /// </summary>
    [HttpGet("admin/deletion-requests")]
    [Authorize(Roles = $"{AuthRoles.Admin},{AuthRoles.SuperAdmin}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<ActionResult<object>> GetAllDeletionRequests(
        [FromQuery] string? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = _context.DataDeletionRequests.AsQueryable();

        if (!string.IsNullOrEmpty(status))
            query = query.Where(r => r.Status == status);

        var total = await query.CountAsync();
        var requests = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Ok(new
        {
            data = requests,
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
    /// Review and approve/reject a deletion request (Admin only)
    /// </summary>
    [HttpPost("admin/deletion-requests/{id}/review")]
    [Authorize(Roles = $"{AuthRoles.Admin},{AuthRoles.SuperAdmin}")]
    [ProducesResponseType(typeof(DataDeletionRequest), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DataDeletionRequest>> ReviewDeletionRequest(
        Guid id,
        [FromBody] ReviewDeletionRequestDto dto)
    {
        var adminUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(adminUserId))
            return Unauthorized();

        var request = await _context.DataDeletionRequests.FindAsync(id);
        if (request == null)
            return NotFound("Deletion request not found");

        if (dto.Decision.ToLower() == "approve")
        {
            request.Status = "approved";
            request.ScheduledDeletionDate = dto.ScheduledDeletionDate ?? DateTime.UtcNow.AddDays(7);
        }
        else if (dto.Decision.ToLower() == "reject")
        {
            request.Status = "rejected";
            request.RejectionReason = dto.RejectionReason;
        }
        else
        {
            return BadRequest(new { message = "Decision must be 'approve' or 'reject'" });
        }

        request.ReviewedBy = adminUserId;
        request.ReviewedAt = DateTime.UtcNow;
        request.ReviewNotes = dto.Notes;
        request.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        await _activityLogService.LogActivityAsync(
            adminUserId,
            $"deletion_request_{dto.Decision}",
            "DataDeletionRequest",
            request.Id.ToString(),
            $"Deletion request {dto.Decision}",
            $"Admin {dto.Decision} deletion request for user {request.UserEmail}",
            "WARNING"
        );

        return Ok(request);
    }

    /// <summary>
    /// Process (execute) an approved deletion request (Admin only)
    /// </summary>
    [HttpPost("admin/deletion-requests/{id}/process")]
    [Authorize(Roles = $"{AuthRoles.Admin},{AuthRoles.SuperAdmin}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<object>> ProcessDeletionRequest(Guid id)
    {
        var adminUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(adminUserId))
            return Unauthorized();

        var request = await _context.DataDeletionRequests.FindAsync(id);
        if (request == null)
            return NotFound("Deletion request not found");

        if (request.Status != "approved")
            return BadRequest(new { message = "Only approved requests can be processed" });

        // Export data if requested
        if (request.DataExportRequested && !request.DataExportCompleted)
        {
            var (exportSuccess, exportError, exportData) = await _anonymizationService.ExportUserDataAsync(request.UserId);
            if (exportSuccess)
            {
                // In production, save this to blob storage and provide download link
                request.DataExportCompleted = true;
                request.DataExportedAt = DateTime.UtcNow;
                // request.ExportFileUrl = "url-to-download-exported-data";
            }
        }

        // Perform anonymization or hard delete
        if (request.DeletionType == "soft")
        {
            var (success, errorMessage, affectedRecords) = await _anonymizationService.AnonymizeUserDataAsync(request.UserId);

            if (!success)
                return BadRequest(new { message = $"Anonymization failed: {errorMessage}" });

            request.Status = "completed";
            request.ProcessedBy = adminUserId;
            request.ProcessedAt = DateTime.UtcNow;
            request.CompletedAt = DateTime.UtcNow;
            request.ProcessingNotes = $"User data anonymized successfully. Affected records: {JsonSerializer.Serialize(affectedRecords)}";

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "User data anonymized successfully",
                type = "soft_delete",
                affectedRecords
            });
        }
        else if (request.DeletionType == "hard")
        {
            var (success, errorMessage) = await _anonymizationService.HardDeleteUserDataAsync(request.UserId);

            if (!success)
                return BadRequest(new { message = $"Hard deletion failed: {errorMessage}" });

            request.Status = "completed";
            request.ProcessedBy = adminUserId;
            request.ProcessedAt = DateTime.UtcNow;
            request.CompletedAt = DateTime.UtcNow;
            request.ProcessingNotes = "User data permanently deleted";

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "User data permanently deleted",
                type = "hard_delete"
            });
        }

        return BadRequest(new { message = "Invalid deletion type" });
    }

    /// <summary>
    /// Anonymize user data directly (Admin only - use with caution)
    /// </summary>
    [HttpPost("admin/users/{userId}/anonymize")]
    [Authorize(Roles = $"{AuthRoles.Admin},{AuthRoles.SuperAdmin}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<object>> AnonymizeUserData(string userId)
    {
        var adminUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(adminUserId))
            return Unauthorized();

        var (success, errorMessage, affectedRecords) = await _anonymizationService.AnonymizeUserDataAsync(userId);

        if (!success)
            return BadRequest(new { message = errorMessage });

        await _activityLogService.LogActivityAsync(
            adminUserId,
            "user_anonymized_by_admin",
            "User",
            userId,
            "User anonymized by admin",
            "Admin directly anonymized user data",
            "CRITICAL"
        );

        return Ok(new
        {
            message = "User data anonymized successfully",
            affectedRecords
        });
    }

    /// <summary>
    /// Export user data (Admin only)
    /// </summary>
    [HttpGet("admin/users/{userId}/export")]
    [Authorize(Roles = $"{AuthRoles.Admin},{AuthRoles.SuperAdmin}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<object>> ExportUserData(string userId)
    {
        var (success, errorMessage, data) = await _anonymizationService.ExportUserDataAsync(userId);

        if (!success)
            return NotFound(new { message = errorMessage });

        return Ok(new
        {
            message = "User data exported successfully",
            exportDate = DateTime.UtcNow,
            data
        });
    }

    /// <summary>
    /// Get data summary for a user (Admin only)
    /// </summary>
    [HttpGet("admin/users/{userId}/data-summary")]
    [Authorize(Roles = $"{AuthRoles.Admin},{AuthRoles.SuperAdmin}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<ActionResult<object>> GetUserDataSummary(string userId)
    {
        var summary = await _anonymizationService.GetUserDataSummaryAsync(userId);

        return Ok(new
        {
            userId,
            dataSummary = summary,
            totalRecords = summary.Values.Sum()
        });
    }

    #endregion
}

public class ConfirmDeletionDto
{
    [Required]
    public required string ConfirmationToken { get; set; }
}
