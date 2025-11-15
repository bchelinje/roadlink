using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Validation.AspNetCore;
using BeC.OpenId.Connect.Features.Documents.Models;
using BeC.OpenId.Connect.Features.Documents.Services.Interfaces;
using BeC.OpenId.Connect.Features.Documents.ViewModels;
using BeC.OpenId.Connect.Infrastructure.Authorization;
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
    private readonly IDocumentService _documentService;
    private readonly ILogger<DocumentsController> _logger;

    public DocumentsController(
        IDocumentService documentService,
        ILogger<DocumentsController> logger)
    {
        _documentService = documentService;
        _logger = logger;
    }

    /// <summary>
    /// Get my documents (Driver)
    /// </summary>
    [HttpGet("~/api/drivers/me/documents")]
    [Authorize(Roles = AuthRoles.Driver)]
    [ProducesResponseType(typeof(List<DocumentViewModel>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<List<DocumentViewModel>>> GetMyDocuments()
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return this.Unauthorized();

            var result = await _documentService.GetDriverDocumentsByUserIdAsync(userId);

            return this.Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting my documents");
            return this.StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
        }
    }

    /// <summary>
    /// Upload a document (Driver)
    /// </summary>
    [HttpPost("~/api/drivers/me/documents")]
    [Authorize(Roles = AuthRoles.Driver)]
    [ProducesResponseType(typeof(DocumentViewModel), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<DocumentViewModel>> UploadDocument(
        [FromForm] string type,
        [FromForm] IFormFile file,
        [FromForm] DateTime? expiryDate = null)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return this.Unauthorized();

            var model = new UploadDocumentModel
            {
                Type = type,
                File = file,
                ExpiryDate = expiryDate
            };

            var result = await _documentService.UploadDocumentAsync(model, userId);

            return this.CreatedAtAction(nameof(GetDocument), new { id = result.Id }, result);
        }
        catch (InvalidOperationException ex)
        {
            return this.NotFound(ex.Message);
        }
        catch (ArgumentException ex)
        {
            return this.BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading document");
            return this.StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
        }
    }

    /// <summary>
    /// Get document by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(DocumentViewModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<DocumentViewModel>> GetDocument(Guid id)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return this.Unauthorized();

            var userRoles = User.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value);

            var canAccess = await _documentService.CanUserAccessDocumentAsync(id, userId, userRoles);
            if (!canAccess)
                return this.Forbid();

            var result = await _documentService.GetDocumentByIdAsync(id);

            return result is not null
                ? this.Ok(result)
                : this.NotFound();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting document {DocumentId}", id);
            return this.StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
        }
    }

    /// <summary>
    /// Delete a document (Driver)
    /// </summary>
    [HttpDelete("~/api/drivers/me/documents/{id}")]
    [Authorize(Roles = AuthRoles.Driver)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeleteMyDocument(Guid id)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return this.Unauthorized();

            var (success, errorMessage) = await _documentService.DeleteDocumentAsync(id, userId);

            if (!success)
            {
                return errorMessage?.Contains("verified")  == true
                    ? this.BadRequest(errorMessage)
                    : this.NotFound(errorMessage);
            }

            return this.NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting document {DocumentId}", id);
            return this.StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
        }
    }

    /// <summary>
    /// Get documents pending verification (Admin)
    /// </summary>
    [HttpGet("pending")]
    [Authorize(Roles = AuthRoles.Admin + "," + AuthRoles.SuperAdmin)]
    [ProducesResponseType(typeof(DocumentListViewModel), StatusCodes.Status200OK)]
    public async Task<ActionResult<DocumentListViewModel>> GetPendingDocuments(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        try
        {
            var result = await _documentService.GetPendingDocumentsAsync(page, pageSize);

            Response.Headers.Append("X-Total-Count", result.TotalCount.ToString());
            Response.Headers.Append("X-Page", result.Page.ToString());
            Response.Headers.Append("X-Page-Size", result.PageSize.ToString());

            return this.Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting pending documents");
            return this.StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
        }
    }

    /// <summary>
    /// Verify a document (Admin)
    /// </summary>
    [HttpPost("{id}/verify")]
    [Authorize(Roles = AuthRoles.Admin + "," + AuthRoles.SuperAdmin)]
    [ProducesResponseType(typeof(DocumentViewModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DocumentViewModel>> VerifyDocument(Guid id)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return this.Unauthorized();

            var (success, document, errorMessage) = await _documentService.VerifyDocumentAsync(id, userId);

            if (!success)
                return this.NotFound(errorMessage);

            return this.Ok(document);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying document {DocumentId}", id);
            return this.StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
        }
    }

    /// <summary>
    /// Reject a document (Admin)
    /// </summary>
    [HttpPost("{id}/reject")]
    [Authorize(Roles = AuthRoles.Admin + "," + AuthRoles.SuperAdmin)]
    [ProducesResponseType(typeof(DocumentViewModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DocumentViewModel>> RejectDocument(Guid id, [FromBody] RejectDocumentModel? request = null)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return this.Unauthorized();

            var (success, document, errorMessage) = await _documentService.RejectDocumentAsync(id, userId, request);

            if (!success)
                return this.NotFound(errorMessage);

            return this.Ok(document);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting document {DocumentId}", id);
            return this.StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
        }
    }

    /// <summary>
    /// Get documents expiring soon (Admin)
    /// </summary>
    [HttpGet("expiring")]
    [Authorize(Roles = AuthRoles.Admin + "," + AuthRoles.SuperAdmin)]
    [ProducesResponseType(typeof(List<DocumentViewModel>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<DocumentViewModel>>> GetExpiringDocuments(
        [FromQuery] int daysAhead = 30)
    {
        try
        {
            var result = await _documentService.GetExpiringDocumentsAsync(daysAhead);

            return this.Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting expiring documents");
            return this.StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
        }
    }

    /// <summary>
    /// Get all documents for a specific driver (Admin)
    /// </summary>
    [HttpGet("drivers/{driverId}")]
    [Authorize(Roles = AuthRoles.Admin + "," + AuthRoles.SuperAdmin)]
    [ProducesResponseType(typeof(List<DocumentViewModel>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<DocumentViewModel>>> GetDriverDocuments(Guid driverId)
    {
        try
        {
            var result = await _documentService.GetDriverDocumentsByDriverIdAsync(driverId);

            return this.Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting driver documents for {DriverId}", driverId);
            return this.StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
        }
    }

    /// <summary>
    /// Get document statistics (Admin)
    /// </summary>
    [HttpGet("statistics")]
    [Authorize(Roles = AuthRoles.Admin + "," + AuthRoles.SuperAdmin)]
    [ProducesResponseType(typeof(DocumentStatisticsViewModel), StatusCodes.Status200OK)]
    public async Task<ActionResult<DocumentStatisticsViewModel>> GetDocumentStatistics()
    {
        try
        {
            var result = await _documentService.GetDocumentStatisticsAsync();

            return this.Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting document statistics");
            return this.StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
        }
    }
}
