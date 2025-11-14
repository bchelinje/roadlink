using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Validation.AspNetCore;
using BeC.OpenId.Connect.Dto;
using BeC.OpenId.Connect.Features.Drivers.Dtos;
using BeC.OpenId.Connect.Features.ActivityLogs.Services.Interfaces;
using AuthRoles = BeC.OpenId.Connect.Infrastructure.Authorization.Roles;

namespace BeC.OpenId.Connect.Features.Documents.Controllers;

/// <summary>
/// Driver document management and verification endpoints
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
[Produces("application/json")]
public class DocumentsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IActivityLogService _activityLogService;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<DocumentsController> _logger;

    public DocumentsController(
        ApplicationDbContext context,
        IActivityLogService activityLogService,
        IWebHostEnvironment environment,
        ILogger<DocumentsController> logger)
    {
        _context = context;
        _activityLogService = activityLogService;
        _environment = environment;
        _logger = logger;
    }

    /// <summary>
    /// Get my documents (Driver)
    /// </summary>
    [HttpGet("~/api/drivers/me/documents")]
    [Authorize(Roles = AuthRoles.Driver)]
    [ProducesResponseType(typeof(List<DriverDocument>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<DriverDocument>>> GetMyDocuments()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var driver = await _context.Drivers.FirstOrDefaultAsync(d => d.UserId == userId);
        if (driver == null)
            return NotFound("Driver profile not found");

        var documents = await _context.DriverDocuments
            .Where(d => d.DriverId == driver.Id)
            .OrderByDescending(d => d.UploadedDate)
            .ToListAsync();

        return Ok(documents);
    }

    /// <summary>
    /// Upload a document (Driver)
    /// </summary>
    [HttpPost("~/api/drivers/me/documents")]
    [Authorize(Roles = AuthRoles.Driver)]
    [ProducesResponseType(typeof(DriverDocument), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<DriverDocument>> UploadDocument(
        [FromForm] string type,
        [FromForm] IFormFile file,
        [FromForm] DateTime? expiryDate = null)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var driver = await _context.Drivers.FirstOrDefaultAsync(d => d.UserId == userId);
        if (driver == null)
            return NotFound("Driver profile not found");

        // Validate file
        if (file == null || file.Length == 0)
            return BadRequest("No file uploaded");

        var allowedTypes = new[] { "drivers_license", "insurance", "vehicle_registration", "mot_certificate", "id_proof", "address_proof" };
        if (!allowedTypes.Contains(type))
            return BadRequest($"Invalid document type. Allowed types: {string.Join(", ", allowedTypes)}");

        var allowedExtensions = new[] { ".pdf", ".jpg", ".jpeg", ".png" };
        var extension = Path.GetExtension(file.FileName).ToLower();
        if (!allowedExtensions.Contains(extension))
            return BadRequest($"Invalid file type. Allowed: {string.Join(", ", allowedExtensions)}");

        // Max file size: 5MB
        if (file.Length > 5 * 1024 * 1024)
            return BadRequest("File size must be less than 5MB");

        // Generate unique filename
        var fileName = $"{driver.Id}_{type}_{Guid.NewGuid()}{extension}";
        var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "drivers", driver.Id.ToString());
        Directory.CreateDirectory(uploadsFolder);

        var filePath = Path.Combine(uploadsFolder, fileName);
        var fileUrl = $"/uploads/drivers/{driver.Id}/{fileName}";

        // Save file
        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        var document = new DriverDocument
        {
            DriverId = driver.Id,
            Type = type,
            FileName = file.FileName,
            FileUrl = fileUrl,
            ExpiryDate = expiryDate,
            Status = "pending"
        };

        _context.DriverDocuments.Add(document);
        await _context.SaveChangesAsync();

        await _activityLogService.LogAsync(
            userId,
            "document_uploaded",
            "DriverDocument",
            document.Id.ToString(),
            type,
            $"Driver uploaded {type} document"
        );

        return CreatedAtAction(nameof(GetDocument), new { id = document.Id }, document);
    }

    /// <summary>
    /// Get document by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(DriverDocument), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DriverDocument>> GetDocument(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var document = await _context.DriverDocuments
            .Include(d => d.Driver)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (document == null)
            return NotFound();

        // Check permissions
        var userRoles = User.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value);
        var isAdmin = userRoles.Contains(AuthRoles.Admin) || userRoles.Contains(AuthRoles.SuperAdmin);
        var isOwner = document.Driver.UserId == userId;

        if (!isAdmin && !isOwner)
            return Forbid("You can only view your own documents");

        return Ok(document);
    }

    /// <summary>
    /// Delete a document (Driver)
    /// </summary>
    [HttpDelete("~/api/drivers/me/documents/{id}")]
    [Authorize(Roles = AuthRoles.Driver)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteMyDocument(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var driver = await _context.Drivers.FirstOrDefaultAsync(d => d.UserId == userId);
        if (driver == null)
            return NotFound("Driver profile not found");

        var document = await _context.DriverDocuments
            .FirstOrDefaultAsync(d => d.Id == id && d.DriverId == driver.Id);

        if (document == null)
            return NotFound();

        // Cannot delete verified documents
        if (document.Status == "verified")
            return BadRequest("Cannot delete verified documents. Please contact support.");

        // Delete physical file
        var filePath = Path.Combine(_environment.WebRootPath, document.FileUrl.TrimStart('/'));
        if (System.IO.File.Exists(filePath))
        {
            System.IO.File.Delete(filePath);
        }

        _context.DriverDocuments.Remove(document);
        await _context.SaveChangesAsync();

        await _activityLogService.LogAsync(
            userId,
            "document_deleted",
            "DriverDocument",
            document.Id.ToString(),
            document.Type,
            $"Driver deleted {document.Type} document"
        );

        return NoContent();
    }

    /// <summary>
    /// Get documents pending verification (Admin)
    /// </summary>
    [HttpGet("pending")]
    [Authorize(Roles = AuthRoles.Admin + "," + AuthRoles.SuperAdmin)]
    [ProducesResponseType(typeof(List<DriverDocument>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<DriverDocument>>> GetPendingDocuments(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var query = _context.DriverDocuments
            .Include(d => d.Driver)
            .Where(d => d.Status == "pending")
            .OrderBy(d => d.UploadedDate);

        var totalCount = await query.CountAsync();
        var documents = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        Response.Headers.Append("X-Total-Count", totalCount.ToString());
        Response.Headers.Append("X-Page", page.ToString());
        Response.Headers.Append("X-Page-Size", pageSize.ToString());

        return Ok(documents);
    }

    /// <summary>
    /// Verify a document (Admin)
    /// </summary>
    [HttpPost("{id}/verify")]
    [Authorize(Roles = AuthRoles.Admin + "," + AuthRoles.SuperAdmin)]
    [ProducesResponseType(typeof(DriverDocument), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DriverDocument>> VerifyDocument(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var document = await _context.DriverDocuments
            .Include(d => d.Driver)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (document == null)
            return NotFound();

        document.Status = "verified";
        document.VerifiedBy = userId;
        document.VerifiedDate = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        await _activityLogService.LogAsync(
            userId,
            "document_verified",
            "DriverDocument",
            document.Id.ToString(),
            document.Type,
            $"Admin verified {document.Type} document for driver {document.Driver.FirstName} {document.Driver.LastName}"
        );

        return Ok(document);
    }

    /// <summary>
    /// Reject a document (Admin)
    /// </summary>
    [HttpPost("{id}/reject")]
    [Authorize(Roles = AuthRoles.Admin + "," + AuthRoles.SuperAdmin)]
    [ProducesResponseType(typeof(DriverDocument), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DriverDocument>> RejectDocument(Guid id, [FromBody] RejectDocumentDto? request = null)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var document = await _context.DriverDocuments
            .Include(d => d.Driver)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (document == null)
            return NotFound();

        document.Status = "rejected";
        document.VerifiedBy = userId;
        document.VerifiedDate = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        var reason = request?.Reason ?? "Document did not meet verification requirements";

        await _activityLogService.LogAsync(
            userId,
            "document_rejected",
            "DriverDocument",
            document.Id.ToString(),
            document.Type,
            $"Admin rejected {document.Type} document for driver {document.Driver.FirstName} {document.Driver.LastName}. Reason: {reason}"
        );

        return Ok(document);
    }

    /// <summary>
    /// Get documents expiring soon (Admin)
    /// </summary>
    [HttpGet("expiring")]
    [Authorize(Roles = AuthRoles.Admin + "," + AuthRoles.SuperAdmin)]
    [ProducesResponseType(typeof(List<DriverDocument>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<DriverDocument>>> GetExpiringDocuments(
        [FromQuery] int daysAhead = 30)
    {
        var expiryThreshold = DateTime.UtcNow.AddDays(daysAhead);

        var documents = await _context.DriverDocuments
            .Include(d => d.Driver)
            .Where(d => d.ExpiryDate.HasValue &&
                       d.ExpiryDate.Value <= expiryThreshold &&
                       d.ExpiryDate.Value >= DateTime.UtcNow &&
                       d.Status == "verified")
            .OrderBy(d => d.ExpiryDate)
            .ToListAsync();

        return Ok(documents);
    }

    /// <summary>
    /// Get all documents for a specific driver (Admin)
    /// </summary>
    [HttpGet("drivers/{driverId}")]
    [Authorize(Roles = AuthRoles.Admin + "," + AuthRoles.SuperAdmin)]
    [ProducesResponseType(typeof(List<DriverDocument>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<DriverDocument>>> GetDriverDocuments(Guid driverId)
    {
        var documents = await _context.DriverDocuments
            .Where(d => d.DriverId == driverId)
            .OrderByDescending(d => d.UploadedDate)
            .ToListAsync();

        return Ok(documents);
    }

    /// <summary>
    /// Get document statistics (Admin)
    /// </summary>
    [HttpGet("statistics")]
    [Authorize(Roles = AuthRoles.Admin + "," + AuthRoles.SuperAdmin)]
    [ProducesResponseType(typeof(DocumentStatistics), StatusCodes.Status200OK)]
    public async Task<ActionResult<DocumentStatistics>> GetDocumentStatistics()
    {
        var allDocuments = await _context.DriverDocuments.ToListAsync();

        var stats = new DocumentStatistics
        {
            TotalDocuments = allDocuments.Count,
            PendingVerification = allDocuments.Count(d => d.Status == "pending"),
            Verified = allDocuments.Count(d => d.Status == "verified"),
            Rejected = allDocuments.Count(d => d.Status == "rejected"),
            Expired = allDocuments.Count(d => d.ExpiryDate.HasValue && d.ExpiryDate.Value < DateTime.UtcNow),
            ExpiringSoon = allDocuments.Count(d => d.ExpiryDate.HasValue &&
                                                   d.ExpiryDate.Value >= DateTime.UtcNow &&
                                                   d.ExpiryDate.Value <= DateTime.UtcNow.AddDays(30)),
            DocumentsByType = allDocuments
                .GroupBy(d => d.Type)
                .ToDictionary(g => g.Key, g => g.Count())
        };

        return Ok(stats);
    }
}

#region DTOs

public class RejectDocumentDto
{
    public string? Reason { get; set; }
}

public class DocumentStatistics
{
    public int TotalDocuments { get; set; }
    public int PendingVerification { get; set; }
    public int Verified { get; set; }
    public int Rejected { get; set; }
    public int Expired { get; set; }
    public int ExpiringSoon { get; set; }
    public Dictionary<string, int> DocumentsByType { get; set; } = new();
}

#endregion
