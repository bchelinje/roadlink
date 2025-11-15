using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using BeC.Common.Data.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Validation.AspNetCore;
using BeC.OpenId.Connect.Dto;
using BeC.OpenId.Connect.Features.Drivers.Dtos;
using BeC.OpenId.Connect.Features.ActivityLogs.Services.Interfaces;
using BeC.OpenId.Connect.Infrastructure.Authorization;
using BeC.Common.Data.Repositories.Interfaces;

namespace BeC.OpenId.Connect.Features.Vehicles.Controllers;

/// <summary>
/// Vehicle management endpoints
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
[Produces("application/json")]
public class VehiclesController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IRepository _repository;
    private readonly IActivityLogService _activityLogService;
    private readonly ILogger<VehiclesController> _logger;

    public VehiclesController(
        ApplicationDbContext context,
        IRepository repository,
        IActivityLogService activityLogService,
        ILogger<VehiclesController> logger)
    {
        _context = context;
        _repository = repository;
        _activityLogService = activityLogService;
        _logger = logger;
    }

    /// <summary>
    /// Get all vehicles (Admin only)
    /// </summary>
    [HttpGet]
    [Authorize(Roles = Infrastructure.Authorization.Roles.Admin + "," + Infrastructure.Authorization.Roles.SuperAdmin)]
    [ProducesResponseType(typeof(List<Vehicle>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<Vehicle>>> GetAllVehicles(
        [FromQuery] string? status = null,
        [FromQuery] Guid? driverId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        // Build predicate based on filters
        System.Linq.Expressions.Expression<Func<Vehicle, bool>>? predicate = null;

        if (!string.IsNullOrWhiteSpace(status) && driverId.HasValue)
        {
            predicate = v => v.Status == status && v.DriverId == driverId.Value;
        }
        else if (!string.IsNullOrWhiteSpace(status))
        {
            predicate = v => v.Status == status;
        }
        else if (driverId.HasValue)
        {
            predicate = v => v.DriverId == driverId.Value;
        }

        // Using Repository: GetEntitiesPaged with filter
        var result = await _repository.GetEntitiesPaged<Vehicle>(
            pageNumber: page,
            pageSize: pageSize,
            predicate: predicate,
            orderBy: q => q.OrderByDescending(v => v.CreatedAt),
            includeProperties: "Driver"
        );

        Response.Headers.Append("X-Total-Count", result.TotalCount.ToString());
        Response.Headers.Append("X-Page", page.ToString());
        Response.Headers.Append("X-Page-Size", pageSize.ToString());

        return Ok(result.Items);
    }

    /// <summary>
    /// Get vehicle by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(Vehicle), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Vehicle>> GetVehicle(Guid id)
    {
        // Using Repository: GetEntity with include
        var vehicle = await _repository.GetEntity<Vehicle>(
            predicate: v => v.Id == id,
            includeProperties: "Driver"
        );

        if (vehicle == null)
            return NotFound();

        return Ok(vehicle);
    }

    /// <summary>
    /// Get my vehicles (Driver)
    /// </summary>
    [HttpGet("~/api/drivers/me/vehicles")]
    [Authorize(Roles = Infrastructure.Authorization.Roles.Driver)]
    [ProducesResponseType(typeof(List<Vehicle>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<Vehicle>>> GetMyVehicles()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        // Using Repository: GetEntity
        var driver = await _repository.GetEntity<Driver>(d => d.UserId == userId);
        if (driver == null)
            return NotFound("Driver profile not found");

        // Using Repository: GetEntities with filter and ordering
        var vehicles = await _repository.GetEntities<Vehicle>(
            predicate: v => v.DriverId == driver.Id,
            orderBy: q => q.OrderByDescending(v => v.CreatedAt)
        );

        return Ok(vehicles);
    }

    /// <summary>
    /// Create a new vehicle
    /// </summary>
    [HttpPost]
    [Authorize(Roles = Infrastructure.Authorization.Roles.Driver + "," + Infrastructure.Authorization.Roles.Admin + "," + Infrastructure.Authorization.Roles.SuperAdmin)]
    [ProducesResponseType(typeof(Vehicle), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Vehicle>> CreateVehicle([FromBody] CreateVehicleDto request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        // Determine the driver ID
        Guid targetDriverId;

        if (request.DriverId.HasValue)
        {
            // Admin creating vehicle for a specific driver
            var userRoles = User.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value);
            if (!userRoles.Contains(Infrastructure.Authorization.Roles.Admin) && !userRoles.Contains(Infrastructure.Authorization.Roles.SuperAdmin))
                return Forbid("Only admins can create vehicles for other drivers");

            targetDriverId = request.DriverId.Value;
        }
        else
        {
            // Driver creating their own vehicle
            // Using Repository: GetEntity
            var driver = await _repository.GetEntity<Driver>(d => d.UserId == userId);
            if (driver == null)
                return NotFound("Driver profile not found");

            targetDriverId = driver.Id;
        }

        // Verify driver exists
        // Using Repository: GetEntity
        var targetDriver = await _repository.GetEntity<Driver>(d => d.Id == targetDriverId);
        if (targetDriver == null)
            return NotFound("Driver not found");

        // Check for duplicate registration number
        // Using Repository: Exists
        var exists = await _repository.Exists<Vehicle>(v => v.RegistrationNumber == request.RegistrationNumber);
        if (exists)
            return BadRequest("A vehicle with this registration number already exists");

        var vehicle = new Vehicle
        {
            DriverId = targetDriverId,
            Type = request.Type,
            Make = request.Make,
            Model = request.Model,
            Year = request.Year,
            RegistrationNumber = request.RegistrationNumber,
            VinNumber = request.VinNumber,
            CargoCapacity = request.CargoCapacity,
            MaxPayloadWeight = request.MaxPayloadWeight,
            MaxGrossWeight = request.MaxGrossWeight,
            CargoLength = request.CargoLength,
            CargoWidth = request.CargoWidth,
            CargoHeight = request.CargoHeight,
            Features = request.Features != null ? JsonSerializer.Serialize(request.Features) : null,
            HasInsurance = request.HasInsurance,
            InsuranceExpiry = request.InsuranceExpiry,
            Status = "active",
            LastInspectionDate = request.LastInspectionDate,
            NextInspectionDue = request.NextInspectionDue,
            Mileage = request.Mileage,
            Photos = request.Photos != null ? JsonSerializer.Serialize(request.Photos) : null,
            IsActive = true
        };

        // Using Repository: InsertEntity
        await _repository.InsertEntity(vehicle);

        await _activityLogService.LogActivityAsync(
            userId,
            "vehicle_created",
            "Vehicle",
            vehicle.Id.ToString(),
            $"{vehicle.Make} {vehicle.Model} ({vehicle.RegistrationNumber})",
            $"Created vehicle for driver {targetDriver.FirstName} {targetDriver.LastName}"
        );

        return CreatedAtAction(nameof(GetVehicle), new { id = vehicle.Id }, vehicle);
    }

    /// <summary>
    /// Add my vehicle (Driver shorthand endpoint)
    /// </summary>
    [HttpPost("~/api/drivers/me/vehicles")]
    [Authorize(Roles = Infrastructure.Authorization.Roles.Driver)]
    [ProducesResponseType(typeof(Vehicle), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Vehicle>> AddMyVehicle([FromBody] CreateVehicleDto request)
    {
        // Ensure DriverId is not set (will be determined automatically)
        request.DriverId = null;
        return await CreateVehicle(request);
    }

    /// <summary>
    /// Update a vehicle
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = Infrastructure.Authorization.Roles.Driver + "," + Infrastructure.Authorization.Roles.Admin + "," + Infrastructure.Authorization.Roles.SuperAdmin)]
    [ProducesResponseType(typeof(Vehicle), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Vehicle>> UpdateVehicle(Guid id, [FromBody] UpdateVehicleDto request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        // Using Repository: GetEntity with include
        var vehicle = await _repository.GetEntity<Vehicle>(
            predicate: v => v.Id == id,
            includeProperties: "Driver"
        );

        if (vehicle == null)
            return NotFound();

        // Check permissions
        var userRoles = User.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value);
        var isAdmin = userRoles.Contains(Infrastructure.Authorization.Roles.Admin) || userRoles.Contains(Infrastructure.Authorization.Roles.SuperAdmin);
        var isOwner = vehicle.Driver.UserId == userId;

        if (!isAdmin && !isOwner)
            return Forbid("You can only update your own vehicles");

        // Update fields
        vehicle.Type = request.Type ?? vehicle.Type;
        vehicle.Make = request.Make ?? vehicle.Make;
        vehicle.Model = request.Model ?? vehicle.Model;
        vehicle.Year = request.Year ?? vehicle.Year;
        vehicle.RegistrationNumber = request.RegistrationNumber ?? vehicle.RegistrationNumber;
        vehicle.VinNumber = request.VinNumber ?? vehicle.VinNumber;
        vehicle.CargoCapacity = request.CargoCapacity ?? vehicle.CargoCapacity;
        vehicle.MaxPayloadWeight = request.MaxPayloadWeight ?? vehicle.MaxPayloadWeight;
        vehicle.MaxGrossWeight = request.MaxGrossWeight ?? vehicle.MaxGrossWeight;
        vehicle.CargoLength = request.CargoLength ?? vehicle.CargoLength;
        vehicle.CargoWidth = request.CargoWidth ?? vehicle.CargoWidth;
        vehicle.CargoHeight = request.CargoHeight ?? vehicle.CargoHeight;
        vehicle.HasInsurance = request.HasInsurance ?? vehicle.HasInsurance;
        vehicle.InsuranceExpiry = request.InsuranceExpiry ?? vehicle.InsuranceExpiry;
        vehicle.LastInspectionDate = request.LastInspectionDate ?? vehicle.LastInspectionDate;
        vehicle.NextInspectionDue = request.NextInspectionDue ?? vehicle.NextInspectionDue;
        vehicle.Mileage = request.Mileage ?? vehicle.Mileage;

        if (request.Features != null)
        {
            vehicle.Features = JsonSerializer.Serialize(request.Features);
        }

        if (request.Photos != null)
        {
            vehicle.Photos = JsonSerializer.Serialize(request.Photos);
        }

        vehicle.UpdatedAt = DateTime.UtcNow;

        // Using Repository: UpdateEntity
        await _repository.UpdateEntity(vehicle);

        await _activityLogService.LogActivityAsync(
            userId,
            "vehicle_updated",
            "Vehicle",
            vehicle.Id.ToString(),
            $"{vehicle.Make} {vehicle.Model} ({vehicle.RegistrationNumber})",
            "Vehicle updated"
        );

        return Ok(vehicle);
    }

    /// <summary>
    /// Update vehicle status
    /// </summary>
    [HttpPatch("{id}/status")]
    [Authorize(Roles = Infrastructure.Authorization.Roles.Driver + "," + Infrastructure.Authorization.Roles.Admin + "," + Infrastructure.Authorization.Roles.SuperAdmin)]
    [ProducesResponseType(typeof(Vehicle), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Vehicle>> UpdateVehicleStatus(Guid id, [FromBody] UpdateVehicleStatusDto request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        // Using Repository: GetEntity with include
        var vehicle = await _repository.GetEntity<Vehicle>(
            predicate: v => v.Id == id,
            includeProperties: "Driver"
        );

        if (vehicle == null)
            return NotFound();

        // Check permissions
        var userRoles = User.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value);
        var isAdmin = userRoles.Contains(Infrastructure.Authorization.Roles.Admin) || userRoles.Contains(Infrastructure.Authorization.Roles.SuperAdmin);
        var isOwner = vehicle.Driver.UserId == userId;

        if (!isAdmin && !isOwner)
            return Forbid("You can only update your own vehicles");

        var oldStatus = vehicle.Status;
        vehicle.Status = request.Status;
        vehicle.UpdatedAt = DateTime.UtcNow;

        // Using Repository: UpdateEntity
        await _repository.UpdateEntity(vehicle);

        await _activityLogService.LogActivityAsync(
            userId,
            "vehicle_status_changed",
            "Vehicle",
            vehicle.Id.ToString(),
            $"{vehicle.Make} {vehicle.Model}",
            $"Status changed from {oldStatus} to {request.Status}"
        );

        return Ok(vehicle);
    }

    /// <summary>
    /// Log vehicle maintenance
    /// </summary>
    [HttpPost("{id}/maintenance")]
    [Authorize(Roles = Infrastructure.Authorization.Roles.Driver + "," + Infrastructure.Authorization.Roles.Admin + "," + Infrastructure.Authorization.Roles.SuperAdmin)]
    [ProducesResponseType(typeof(Vehicle), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Vehicle>> LogMaintenance(Guid id, [FromBody] LogMaintenanceDto request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        // Using Repository: GetEntity with include
        var vehicle = await _repository.GetEntity<Vehicle>(
            predicate: v => v.Id == id,
            includeProperties: "Driver"
        );

        if (vehicle == null)
            return NotFound();

        // Check permissions
        var userRoles = User.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value);
        var isAdmin = userRoles.Contains(Infrastructure.Authorization.Roles.Admin) || userRoles.Contains(Infrastructure.Authorization.Roles.SuperAdmin);
        var isOwner = vehicle.Driver.UserId == userId;

        if (!isAdmin && !isOwner)
            return Forbid("You can only log maintenance for your own vehicles");

        vehicle.LastInspectionDate = request.MaintenanceDate ?? DateTime.UtcNow;
        vehicle.NextInspectionDue = request.NextInspectionDue;
        vehicle.Mileage = request.Mileage ?? vehicle.Mileage;
        vehicle.UpdatedAt = DateTime.UtcNow;

        // Using Repository: UpdateEntity
        await _repository.UpdateEntity(vehicle);

        await _activityLogService.LogActivityAsync(
            userId,
            "vehicle_maintenance",
            "Vehicle",
            vehicle.Id.ToString(),
            $"{vehicle.Make} {vehicle.Model}",
            $"Maintenance logged: {request.Description ?? "Routine maintenance"}"
        );

        return Ok(vehicle);
    }

    /// <summary>
    /// Get vehicle maintenance history
    /// </summary>
    [HttpGet("{id}/maintenance-history")]
    [ProducesResponseType(typeof(List<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<object>>> GetMaintenanceHistory(Guid id)
    {
        var vehicle = await _context.Vehicles.FindAsync(id);
        if (vehicle == null)
            return NotFound();

        // Get activity logs related to this vehicle's maintenance
        var maintenanceLogs = await _context.ActivityLogs
            .Where(a => a.EntityType == "Vehicle" &&
                       a.EntityId == id.ToString() &&
                       a.Action == "vehicle_maintenance")
            .OrderByDescending(a => a.Timestamp)
            .Select(a => new
            {
                a.Timestamp,
                a.Description,
                a.UserName
            })
            .ToListAsync();

        return Ok(maintenanceLogs);
    }

    /// <summary>
    /// Delete a vehicle
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = Infrastructure.Authorization.Roles.Driver + "," + Infrastructure.Authorization.Roles.Admin + "," + Infrastructure.Authorization.Roles.SuperAdmin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteVehicle(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        // Using Repository: GetEntity with include
        var vehicle = await _repository.GetEntity<Vehicle>(
            predicate: v => v.Id == id,
            includeProperties: "Driver"
        );

        if (vehicle == null)
            return NotFound();

        // Check permissions
        var userRoles = User.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value);
        var isAdmin = userRoles.Contains(Infrastructure.Authorization.Roles.Admin) || userRoles.Contains(Infrastructure.Authorization.Roles.SuperAdmin);
        var isOwner = vehicle.Driver.UserId == userId;

        if (!isAdmin && !isOwner)
            return Forbid("You can only delete your own vehicles");

        // Soft delete
        vehicle.IsActive = false;
        vehicle.Status = "retired";
        vehicle.UpdatedAt = DateTime.UtcNow;

        // Using Repository: UpdateEntity
        await _repository.UpdateEntity(vehicle);

        await _activityLogService.LogActivityAsync(
            userId,
            "vehicle_deleted",
            "Vehicle",
            vehicle.Id.ToString(),
            $"{vehicle.Make} {vehicle.Model}",
            "Vehicle deleted (soft delete)"
        );

        return NoContent();
    }
}

#region DTOs

public class CreateVehicleDto
{
    public Guid? DriverId { get; set; } // Only for admin use
    public required string Type { get; set; }
    public required string Make { get; set; }
    public required string Model { get; set; }
    public required int Year { get; set; }
    public required string RegistrationNumber { get; set; }
    public string? VinNumber { get; set; }
    public required int CargoCapacity { get; set; }
    public required decimal MaxPayloadWeight { get; set; }
    public required decimal MaxGrossWeight { get; set; }
    public decimal? CargoLength { get; set; }
    public decimal? CargoWidth { get; set; }
    public decimal? CargoHeight { get; set; }
    public List<string>? Features { get; set; }
    public required bool HasInsurance { get; set; }
    public DateTime? InsuranceExpiry { get; set; }
    public DateTime? LastInspectionDate { get; set; }
    public DateTime? NextInspectionDue { get; set; }
    public int? Mileage { get; set; }
    public List<string>? Photos { get; set; }
}

public class UpdateVehicleDto
{
    public string? Type { get; set; }
    public string? Make { get; set; }
    public string? Model { get; set; }
    public int? Year { get; set; }
    public string? RegistrationNumber { get; set; }
    public string? VinNumber { get; set; }
    public int? CargoCapacity { get; set; }
    public decimal? MaxPayloadWeight { get; set; }
    public decimal? MaxGrossWeight { get; set; }
    public decimal? CargoLength { get; set; }
    public decimal? CargoWidth { get; set; }
    public decimal? CargoHeight { get; set; }
    public List<string>? Features { get; set; }
    public bool? HasInsurance { get; set; }
    public DateTime? InsuranceExpiry { get; set; }
    public DateTime? LastInspectionDate { get; set; }
    public DateTime? NextInspectionDue { get; set; }
    public int? Mileage { get; set; }
    public List<string>? Photos { get; set; }
}

public class UpdateVehicleStatusDto
{
    public required string Status { get; set; } // active, inactive, maintenance, retired
}

public class LogMaintenanceDto
{
    public DateTime? MaintenanceDate { get; set; }
    public DateTime? NextInspectionDue { get; set; }
    public int? Mileage { get; set; }
    public string? Description { get; set; }
}

#endregion
