using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Validation.AspNetCore;
using BeC.OpenId.Connect.Dto;
using BeC.OpenId.Connect.Features.Drivers.Dtos;
using BeC.OpenId.Connect.Features.Customers.Dtos;
using BeC.OpenId.Connect.Features.ActivityLogs.Services.Interfaces;
using BeC.Common.Data.Repositories.Interfaces;
using AuthRoles = BeC.OpenId.Connect.Infrastructure.Authorization.Roles;

namespace BeC.OpenId.Connect.Features.Admin.Controllers;

/// <summary>
/// Admin vetting and approval endpoints for drivers, customers, vehicles, and documents
/// Supports bulk vetting for high-volume onboarding (e.g., 10,000+ drivers)
/// </summary>
[ApiController]
[Route("api/admin/[controller]")]
[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
[Authorize(Roles = $"{AuthRoles.Admin},{AuthRoles.SuperAdmin}")]
[Produces("application/json")]
public class VettingController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IRepository _repository;
    private readonly IActivityLogService _activityLogService;
    private readonly ILogger<VettingController> _logger;

    public VettingController(
        ApplicationDbContext context,
        IRepository repository,
        IActivityLogService activityLogService,
        ILogger<VettingController> logger)
    {
        _context = context;
        _repository = repository;
        _activityLogService = activityLogService;
        _logger = logger;
    }

    #region Pending Registrations

    /// <summary>
    /// Get all pending driver registrations for vetting
    /// Supports pagination for handling thousands of applications
    /// </summary>
    [HttpGet("drivers/pending")]
    [ProducesResponseType(typeof(List<object>), StatusCodes.Status200OK)]
    public async Task<ActionResult> GetPendingDrivers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? sortBy = "CreatedAt",
        [FromQuery] bool descending = true)
    {
        var query = _context.Drivers
            .Include(d => d.Documents)
            .Include(d => d.Vehicles)
            .Where(d => d.ApprovalStatus == "pending");

        // Dynamic sorting
        query = sortBy?.ToLower() switch
        {
            "firstname" => descending ? query.OrderByDescending(d => d.FirstName) : query.OrderBy(d => d.FirstName),
            "lastname" => descending ? query.OrderByDescending(d => d.LastName) : query.OrderBy(d => d.LastName),
            "email" => descending ? query.OrderByDescending(d => d.Email) : query.OrderBy(d => d.Email),
            "joinedate" => descending ? query.OrderByDescending(d => d.JoinedDate) : query.OrderBy(d => d.JoinedDate),
            _ => descending ? query.OrderByDescending(d => d.CreatedAt) : query.OrderBy(d => d.CreatedAt)
        };

        var totalCount = await query.CountAsync();
        var drivers = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(d => new
            {
                d.Id,
                d.UserId,
                d.FirstName,
                d.LastName,
                d.Email,
                d.Phone,
                d.LicenseNumber,
                d.LicenseExpiry,
                d.DrivingLicenseType,
                d.NationalInsuranceNumber,
                d.ApprovalStatus,
                d.BackgroundCheckCompleted,
                d.RightToWorkVerified,
                d.ProofOfAddressVerified,
                d.JoinedDate,
                d.CreatedAt,
                DocumentCount = d.Documents.Count,
                VehicleCount = d.Vehicles.Count,
                Documents = d.Documents.Select(doc => new
                {
                    doc.Id,
                    doc.Type,
                    doc.Status,
                    doc.UploadedDate,
                    doc.ExpiryDate
                }),
                Vehicles = d.Vehicles.Select(v => new
                {
                    v.Id,
                    v.Type,
                    v.Make,
                    v.Model,
                    v.RegistrationNumber,
                    v.ApprovalStatus
                })
            })
            .ToListAsync();

        Response.Headers.Append("X-Total-Count", totalCount.ToString());
        Response.Headers.Append("X-Page", page.ToString());
        Response.Headers.Append("X-Page-Size", pageSize.ToString());
        Response.Headers.Append("X-Total-Pages", Math.Ceiling((double)totalCount / pageSize).ToString());

        return Ok(drivers);
    }

    /// <summary>
    /// Get all pending customer registrations
    /// </summary>
    [HttpGet("customers/pending")]
    [ProducesResponseType(typeof(List<Customer>), StatusCodes.Status200OK)]
    public async Task<ActionResult> GetPendingCustomers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var query = _context.Customers
            .Where(c => c.ApprovalStatus == "pending")
            .OrderByDescending(c => c.CreatedAt);

        var totalCount = await query.CountAsync();
        var customers = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        Response.Headers.Append("X-Total-Count", totalCount.ToString());
        Response.Headers.Append("X-Page", page.ToString());
        Response.Headers.Append("X-Page-Size", pageSize.ToString());

        return Ok(customers);
    }

    /// <summary>
    /// Get all pending vehicle registrations
    /// </summary>
    [HttpGet("vehicles/pending")]
    [ProducesResponseType(typeof(List<object>), StatusCodes.Status200OK)]
    public async Task<ActionResult> GetPendingVehicles(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var query = _context.Vehicles
            .Include(v => v.Driver)
            .Where(v => v.ApprovalStatus == "pending")
            .OrderByDescending(v => v.CreatedAt);

        var totalCount = await query.CountAsync();
        var vehicles = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(v => new
            {
                v.Id,
                v.Type,
                v.Make,
                v.Model,
                v.Year,
                v.RegistrationNumber,
                v.MOTExpiryDate,
                v.InsuranceExpiry,
                v.RoadTaxExpiryDate,
                v.HireAndRewardInsurance,
                v.GoodsInTransitInsurance,
                v.PublicLiabilityInsurance,
                v.ApprovalStatus,
                v.CreatedAt,
                Driver = new
                {
                    v.Driver.Id,
                    v.Driver.FirstName,
                    v.Driver.LastName,
                    v.Driver.Email,
                    v.Driver.ApprovalStatus
                },
                Documents = _context.DriverDocuments
                    .Where(d => d.VehicleId == v.Id)
                    .Select(d => new { d.Id, d.Type, d.Status })
                    .ToList()
            })
            .ToListAsync();

        Response.Headers.Append("X-Total-Count", totalCount.ToString());
        Response.Headers.Append("X-Page", page.ToString());
        Response.Headers.Append("X-Page-Size", pageSize.ToString());

        return Ok(vehicles);
    }

    /// <summary>
    /// Get all pending documents for verification
    /// </summary>
    [HttpGet("documents/pending")]
    [ProducesResponseType(typeof(List<object>), StatusCodes.Status200OK)]
    public async Task<ActionResult> GetPendingDocuments(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? documentType = null)
    {
        var query = _context.DriverDocuments
            .Include(d => d.Driver)
            .Where(d => d.Status == "pending");

        if (!string.IsNullOrWhiteSpace(documentType))
        {
            query = query.Where(d => d.Type == documentType);
        }

        query = query.OrderByDescending(d => d.CreatedAt);

        var totalCount = await query.CountAsync();
        var documents = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(d => new
            {
                d.Id,
                d.Type,
                d.FileName,
                d.FileUrl,
                d.FileSize,
                d.MimeType,
                d.DocumentNumber,
                d.IssueDate,
                d.ExpiryDate,
                d.IssuingAuthority,
                d.Status,
                d.UploadedDate,
                d.VehicleId,
                Driver = new
                {
                    d.Driver.Id,
                    d.Driver.FirstName,
                    d.Driver.LastName,
                    d.Driver.Email,
                    d.Driver.ApprovalStatus
                }
            })
            .ToListAsync();

        Response.Headers.Append("X-Total-Count", totalCount.ToString());
        Response.Headers.Append("X-Page", page.ToString());
        Response.Headers.Append("X-Page-Size", pageSize.ToString());

        return Ok(documents);
    }

    #endregion

    #region Driver Approval

    /// <summary>
    /// Approve a driver registration
    /// </summary>
    [HttpPost("drivers/{id}/approve")]
    [ProducesResponseType(typeof(Driver), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> ApproveDriver(Guid id, [FromBody] ApprovalRequest? request = null)
    {
        var adminUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(adminUserId))
            return Unauthorized();

        var driver = await _repository.GetEntity<Driver>(d => d.Id == id);
        if (driver == null)
            return NotFound(new { message = "Driver not found" });

        if (driver.ApprovalStatus == "approved")
            return BadRequest(new { message = "Driver is already approved" });

        driver.ApprovalStatus = "approved";
        driver.Status = "active";
        driver.ApprovedBy = adminUserId;
        driver.ApprovedAt = DateTime.UtcNow;
        driver.UpdatedAt = DateTime.UtcNow;

        if (!string.IsNullOrWhiteSpace(request?.Notes))
        {
            driver.AdminNotes = (driver.AdminNotes ?? "") + $"\n[APPROVAL] {DateTime.UtcNow:yyyy-MM-dd HH:mm} - {request.Notes}";
        }

        await _repository.UpdateEntity(driver);

        await _activityLogService.LogActivityAsync(
            adminUserId,
            "driver_approved",
            "Driver",
            driver.Id.ToString(),
            $"{driver.FirstName} {driver.LastName}",
            $"Admin approved driver registration for {driver.Email}"
        );

        _logger.LogInformation("Driver approved: {DriverId} by admin {AdminId}", id, adminUserId);

        // TODO: Send approval email to driver
        // TODO: Send notification to driver

        return Ok(driver);
    }

    /// <summary>
    /// Reject a driver registration
    /// </summary>
    [HttpPost("drivers/{id}/reject")]
    [ProducesResponseType(typeof(Driver), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> RejectDriver(Guid id, [FromBody] RejectionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            return BadRequest(new { message = "Rejection reason is required" });
        }

        var adminUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(adminUserId))
            return Unauthorized();

        var driver = await _repository.GetEntity<Driver>(d => d.Id == id);
        if (driver == null)
            return NotFound(new { message = "Driver not found" });

        driver.ApprovalStatus = "rejected";
        driver.Status = "suspended";
        driver.RejectionReason = request.Reason;
        driver.UpdatedAt = DateTime.UtcNow;

        if (!string.IsNullOrWhiteSpace(request.Notes))
        {
            driver.AdminNotes = (driver.AdminNotes ?? "") + $"\n[REJECTION] {DateTime.UtcNow:yyyy-MM-dd HH:mm} - {request.Notes}";
        }

        await _repository.UpdateEntity(driver);

        await _activityLogService.LogActivityAsync(
            adminUserId,
            "driver_rejected",
            "Driver",
            driver.Id.ToString(),
            $"{driver.FirstName} {driver.LastName}",
            $"Admin rejected driver registration for {driver.Email}: {request.Reason}"
        );

        _logger.LogInformation("Driver rejected: {DriverId} by admin {AdminId}. Reason: {Reason}",
            id, adminUserId, request.Reason);

        // TODO: Send rejection email to driver with reason
        // TODO: Send notification to driver

        return Ok(driver);
    }

    /// <summary>
    /// Suspend an approved driver
    /// </summary>
    [HttpPost("drivers/{id}/suspend")]
    [ProducesResponseType(typeof(Driver), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> SuspendDriver(Guid id, [FromBody] SuspensionRequest request)
    {
        var adminUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(adminUserId))
            return Unauthorized();

        var driver = await _repository.GetEntity<Driver>(d => d.Id == id);
        if (driver == null)
            return NotFound(new { message = "Driver not found" });

        driver.ApprovalStatus = "suspended";
        driver.Status = "suspended";
        driver.UpdatedAt = DateTime.UtcNow;
        driver.AdminNotes = (driver.AdminNotes ?? "") + $"\n[SUSPENSION] {DateTime.UtcNow:yyyy-MM-dd HH:mm} - {request.Reason}";

        await _repository.UpdateEntity(driver);

        await _activityLogService.LogActivityAsync(
            adminUserId,
            "driver_suspended",
            "Driver",
            driver.Id.ToString(),
            $"{driver.FirstName} {driver.LastName}",
            $"Admin suspended driver {driver.Email}: {request.Reason}"
        );

        _logger.LogInformation("Driver suspended: {DriverId} by admin {AdminId}. Reason: {Reason}",
            id, adminUserId, request.Reason);

        return Ok(driver);
    }

    #endregion

    #region Customer Approval

    /// <summary>
    /// Approve a customer registration
    /// </summary>
    [HttpPost("customers/{id}/approve")]
    [ProducesResponseType(typeof(Customer), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> ApproveCustomer(Guid id, [FromBody] ApprovalRequest? request = null)
    {
        var adminUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(adminUserId))
            return Unauthorized();

        var customer = await _repository.GetEntity<Customer>(c => c.Id == id);
        if (customer == null)
            return NotFound(new { message = "Customer not found" });

        if (customer.ApprovalStatus == "approved")
            return BadRequest(new { message = "Customer is already approved" });

        customer.ApprovalStatus = "approved";
        customer.Status = "active";
        customer.ApprovedBy = adminUserId;
        customer.ApprovedAt = DateTime.UtcNow;
        customer.UpdatedAt = DateTime.UtcNow;

        if (!string.IsNullOrWhiteSpace(request?.Notes))
        {
            customer.AdminNotes = (customer.AdminNotes ?? "") + $"\n[APPROVAL] {DateTime.UtcNow:yyyy-MM-dd HH:mm} - {request.Notes}";
        }

        await _repository.UpdateEntity(customer);

        await _activityLogService.LogActivityAsync(
            adminUserId,
            "customer_approved",
            "Customer",
            customer.Id.ToString(),
            $"{customer.FirstName} {customer.LastName}",
            $"Admin approved customer registration for {customer.Email}"
        );

        _logger.LogInformation("Customer approved: {CustomerId} by admin {AdminId}", id, adminUserId);

        return Ok(customer);
    }

    /// <summary>
    /// Reject a customer registration
    /// </summary>
    [HttpPost("customers/{id}/reject")]
    [ProducesResponseType(typeof(Customer), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> RejectCustomer(Guid id, [FromBody] RejectionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            return BadRequest(new { message = "Rejection reason is required" });
        }

        var adminUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(adminUserId))
            return Unauthorized();

        var customer = await _repository.GetEntity<Customer>(c => c.Id == id);
        if (customer == null)
            return NotFound(new { message = "Customer not found" });

        customer.ApprovalStatus = "rejected";
        customer.Status = "deactivated";
        customer.RejectionReason = request.Reason;
        customer.UpdatedAt = DateTime.UtcNow;

        if (!string.IsNullOrWhiteSpace(request.Notes))
        {
            customer.AdminNotes = (customer.AdminNotes ?? "") + $"\n[REJECTION] {DateTime.UtcNow:yyyy-MM-dd HH:mm} - {request.Notes}";
        }

        await _repository.UpdateEntity(customer);

        await _activityLogService.LogActivityAsync(
            adminUserId,
            "customer_rejected",
            "Customer",
            customer.Id.ToString(),
            $"{customer.FirstName} {customer.LastName}",
            $"Admin rejected customer registration for {customer.Email}: {request.Reason}"
        );

        return Ok(customer);
    }

    #endregion

    #region Vehicle Approval

    /// <summary>
    /// Approve a vehicle registration
    /// </summary>
    [HttpPost("vehicles/{id}/approve")]
    [ProducesResponseType(typeof(Vehicle), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> ApproveVehicle(Guid id, [FromBody] ApprovalRequest? request = null)
    {
        var adminUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(adminUserId))
            return Unauthorized();

        var vehicle = await _repository.GetEntity<Vehicle>(v => v.Id == id);
        if (vehicle == null)
            return NotFound(new { message = "Vehicle not found" });

        if (vehicle.ApprovalStatus == "approved")
            return BadRequest(new { message = "Vehicle is already approved" });

        vehicle.ApprovalStatus = "approved";
        vehicle.Status = "active";
        vehicle.IsActive = true;
        vehicle.ApprovedBy = adminUserId;
        vehicle.ApprovedAt = DateTime.UtcNow;
        vehicle.UpdatedAt = DateTime.UtcNow;

        if (!string.IsNullOrWhiteSpace(request?.Notes))
        {
            vehicle.AdminNotes = (vehicle.AdminNotes ?? "") + $"\n[APPROVAL] {DateTime.UtcNow:yyyy-MM-dd HH:mm} - {request.Notes}";
        }

        await _repository.UpdateEntity(vehicle);

        await _activityLogService.LogActivityAsync(
            adminUserId,
            "vehicle_approved",
            "Vehicle",
            vehicle.Id.ToString(),
            vehicle.RegistrationNumber,
            $"Admin approved vehicle {vehicle.Make} {vehicle.Model} ({vehicle.RegistrationNumber})"
        );

        _logger.LogInformation("Vehicle approved: {VehicleId} by admin {AdminId}", id, adminUserId);

        return Ok(vehicle);
    }

    /// <summary>
    /// Reject a vehicle registration
    /// </summary>
    [HttpPost("vehicles/{id}/reject")]
    [ProducesResponseType(typeof(Vehicle), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> RejectVehicle(Guid id, [FromBody] RejectionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            return BadRequest(new { message = "Rejection reason is required" });
        }

        var adminUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(adminUserId))
            return Unauthorized();

        var vehicle = await _repository.GetEntity<Vehicle>(v => v.Id == id);
        if (vehicle == null)
            return NotFound(new { message = "Vehicle not found" });

        vehicle.ApprovalStatus = "rejected";
        vehicle.Status = "inactive";
        vehicle.IsActive = false;
        vehicle.RejectionReason = request.Reason;
        vehicle.UpdatedAt = DateTime.UtcNow;

        if (!string.IsNullOrWhiteSpace(request.Notes))
        {
            vehicle.AdminNotes = (vehicle.AdminNotes ?? "") + $"\n[REJECTION] {DateTime.UtcNow:yyyy-MM-dd HH:mm} - {request.Notes}";
        }

        await _repository.UpdateEntity(vehicle);

        await _activityLogService.LogActivityAsync(
            adminUserId,
            "vehicle_rejected",
            "Vehicle",
            vehicle.Id.ToString(),
            vehicle.RegistrationNumber,
            $"Admin rejected vehicle {vehicle.RegistrationNumber}: {request.Reason}"
        );

        return Ok(vehicle);
    }

    #endregion

    #region Document Verification

    /// <summary>
    /// Verify/approve a document
    /// </summary>
    [HttpPost("documents/{id}/verify")]
    [ProducesResponseType(typeof(DriverDocument), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> VerifyDocument(Guid id, [FromBody] DocumentVerificationRequest? request = null)
    {
        var adminUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(adminUserId))
            return Unauthorized();

        var document = await _repository.GetEntity<DriverDocument>(d => d.Id == id);
        if (document == null)
            return NotFound(new { message = "Document not found" });

        document.Status = "verified";
        document.VerifiedBy = adminUserId;
        document.VerifiedDate = DateTime.UtcNow;
        document.UpdatedAt = DateTime.UtcNow;

        if (!string.IsNullOrWhiteSpace(request?.Notes))
        {
            document.Notes = (document.Notes ?? "") + $"\n[VERIFIED] {DateTime.UtcNow:yyyy-MM-dd HH:mm} - {request.Notes}";
        }

        await _repository.UpdateEntity(document);

        await _activityLogService.LogActivityAsync(
            adminUserId,
            "document_verified",
            "DriverDocument",
            document.Id.ToString(),
            document.Type,
            $"Admin verified {document.Type} document for driver {document.DriverId}"
        );

        _logger.LogInformation("Document verified: {DocumentId} ({Type}) by admin {AdminId}",
            id, document.Type, adminUserId);

        return Ok(document);
    }

    /// <summary>
    /// Reject a document
    /// </summary>
    [HttpPost("documents/{id}/reject")]
    [ProducesResponseType(typeof(DriverDocument), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> RejectDocument(Guid id, [FromBody] RejectionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            return BadRequest(new { message = "Rejection reason is required" });
        }

        var adminUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(adminUserId))
            return Unauthorized();

        var document = await _repository.GetEntity<DriverDocument>(d => d.Id == id);
        if (document == null)
            return NotFound(new { message = "Document not found" });

        document.Status = "rejected";
        document.RejectionReason = request.Reason;
        document.UpdatedAt = DateTime.UtcNow;

        if (!string.IsNullOrWhiteSpace(request.Notes))
        {
            document.Notes = (document.Notes ?? "") + $"\n[REJECTED] {DateTime.UtcNow:yyyy-MM-dd HH:mm} - {request.Notes}";
        }

        await _repository.UpdateEntity(document);

        await _activityLogService.LogActivityAsync(
            adminUserId,
            "document_rejected",
            "DriverDocument",
            document.Id.ToString(),
            document.Type,
            $"Admin rejected {document.Type} document: {request.Reason}"
        );

        return Ok(document);
    }

    #endregion

    #region Bulk Operations

    /// <summary>
    /// Bulk approve multiple drivers
    /// Useful for high-volume onboarding
    /// </summary>
    [HttpPost("drivers/bulk-approve")]
    [ProducesResponseType(typeof(BulkOperationResult), StatusCodes.Status200OK)]
    public async Task<ActionResult> BulkApproveDrivers([FromBody] BulkApprovalRequest request)
    {
        var adminUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(adminUserId))
            return Unauthorized();

        var result = new BulkOperationResult();

        foreach (var driverId in request.Ids)
        {
            try
            {
                var driver = await _repository.GetEntity<Driver>(d => d.Id == driverId);
                if (driver == null)
                {
                    result.Failed.Add(new { Id = driverId, Reason = "Driver not found" });
                    continue;
                }

                if (driver.ApprovalStatus == "approved")
                {
                    result.Skipped.Add(new { Id = driverId, Reason = "Already approved" });
                    continue;
                }

                driver.ApprovalStatus = "approved";
                driver.Status = "active";
                driver.ApprovedBy = adminUserId;
                driver.ApprovedAt = DateTime.UtcNow;
                driver.UpdatedAt = DateTime.UtcNow;

                await _repository.UpdateEntity(driver);

                await _activityLogService.LogActivityAsync(
                    adminUserId,
                    "driver_bulk_approved",
                    "Driver",
                    driver.Id.ToString(),
                    $"{driver.FirstName} {driver.LastName}",
                    $"Bulk approved driver registration"
                );

                result.Successful.Add(driverId);
            }
            catch (Exception ex)
            {
                result.Failed.Add(new { Id = driverId, Reason = ex.Message });
                _logger.LogError(ex, "Error bulk approving driver {DriverId}", driverId);
            }
        }

        _logger.LogInformation("Bulk driver approval completed: {Success} successful, {Failed} failed, {Skipped} skipped",
            result.Successful.Count, result.Failed.Count, result.Skipped.Count);

        return Ok(result);
    }

    #endregion
}

#region DTOs

public class ApprovalRequest
{
    public string? Notes { get; set; }
}

public class RejectionRequest
{
    [Required]
    public required string Reason { get; set; }
    public string? Notes { get; set; }
}

public class SuspensionRequest
{
    [Required]
    public required string Reason { get; set; }
}

public class DocumentVerificationRequest
{
    public string? Notes { get; set; }
}

public class BulkApprovalRequest
{
    [Required]
    public required List<Guid> Ids { get; set; }
}

public class BulkOperationResult
{
    public List<Guid> Successful { get; set; } = new();
    public List<object> Failed { get; set; } = new();
    public List<object> Skipped { get; set; } = new();
}

#endregion
