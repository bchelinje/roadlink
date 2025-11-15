using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Validation.AspNetCore;
using BeC.OpenId.Connect.Features.Vehicles.Models;
using BeC.OpenId.Connect.Features.Vehicles.Services.Interfaces;
using BeC.OpenId.Connect.Features.Vehicles.ViewModels;
using BeC.OpenId.Connect.Infrastructure.Authorization;
using AuthRoles = BeC.OpenId.Connect.Infrastructure.Authorization.Roles;

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
    private readonly IVehicleService _vehicleService;
    private readonly ILogger<VehiclesController> _logger;

    public VehiclesController(
        IVehicleService vehicleService,
        ILogger<VehiclesController> logger)
    {
        _vehicleService = vehicleService;
        _logger = logger;
    }

    /// <summary>
    /// Get all vehicles with pagination and filtering (Admin only)
    /// </summary>
    [HttpGet]
    [Authorize(Roles = AuthRoles.Admin + "," + AuthRoles.SuperAdmin)]
    [ProducesResponseType(typeof(VehicleListViewModel), StatusCodes.Status200OK)]
    public async Task<ActionResult<VehicleListViewModel>> GetAllVehicles(
        [FromQuery] string? status = null,
        [FromQuery] Guid? driverId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            var result = await _vehicleService.GetAllVehiclesAsync(status, driverId, page, pageSize);

            Response.Headers.Append("X-Total-Count", result.TotalCount.ToString());
            Response.Headers.Append("X-Page", result.Page.ToString());
            Response.Headers.Append("X-Page-Size", result.PageSize.ToString());

            return this.Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all vehicles");
            return this.StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
        }
    }

    /// <summary>
    /// Get vehicle by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(VehicleViewModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VehicleViewModel>> GetVehicle(Guid id)
    {
        try
        {
            var result = await _vehicleService.GetVehicleByIdAsync(id);

            return result is not null
                ? this.Ok(result)
                : this.NotFound();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting vehicle {VehicleId}", id);
            return this.StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
        }
    }

    /// <summary>
    /// Get my vehicles (Driver)
    /// </summary>
    [HttpGet("~/api/drivers/me/vehicles")]
    [Authorize(Roles = AuthRoles.Driver)]
    [ProducesResponseType(typeof(List<VehicleViewModel>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<List<VehicleViewModel>>> GetMyVehicles()
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return this.Unauthorized();

            var result = await _vehicleService.GetVehiclesByDriverUserIdAsync(userId);

            return this.Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting my vehicles");
            return this.StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
        }
    }

    /// <summary>
    /// Create a new vehicle
    /// </summary>
    [HttpPost]
    [Authorize(Roles = AuthRoles.Driver + "," + AuthRoles.Admin + "," + AuthRoles.SuperAdmin)]
    [ProducesResponseType(typeof(VehicleViewModel), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<VehicleViewModel>> CreateVehicle([FromBody] CreateVehicleModel request)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return this.Unauthorized();

            var userRoles = User.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value);

            var result = await _vehicleService.CreateVehicleAsync(request, userId, userRoles);

            return this.CreatedAtAction(nameof(GetVehicle), new { id = result.Id }, result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return this.Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return this.BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating vehicle");
            return this.StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
        }
    }

    /// <summary>
    /// Add my vehicle (Driver shorthand endpoint)
    /// </summary>
    [HttpPost("~/api/drivers/me/vehicles")]
    [Authorize(Roles = AuthRoles.Driver)]
    [ProducesResponseType(typeof(VehicleViewModel), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<VehicleViewModel>> AddMyVehicle([FromBody] CreateVehicleModel request)
    {
        // Ensure DriverId is not set (will be determined automatically)
        request.DriverId = null;
        return await CreateVehicle(request);
    }

    /// <summary>
    /// Update a vehicle
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = AuthRoles.Driver + "," + AuthRoles.Admin + "," + AuthRoles.SuperAdmin)]
    [ProducesResponseType(typeof(VehicleViewModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VehicleViewModel>> UpdateVehicle(Guid id, [FromBody] UpdateVehicleModel request)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return this.Unauthorized();

            var userRoles = User.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value);

            var (success, vehicle, errorMessage) = await _vehicleService.UpdateVehicleAsync(id, request, userId, userRoles);

            if (!success)
            {
                return errorMessage == "Vehicle not found"
                    ? this.NotFound()
                    : this.Forbid();
            }

            return this.Ok(vehicle);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating vehicle {VehicleId}", id);
            return this.StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
        }
    }

    /// <summary>
    /// Update vehicle status
    /// </summary>
    [HttpPatch("{id}/status")]
    [Authorize(Roles = AuthRoles.Driver + "," + AuthRoles.Admin + "," + AuthRoles.SuperAdmin)]
    [ProducesResponseType(typeof(VehicleViewModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VehicleViewModel>> UpdateVehicleStatus(Guid id, [FromBody] UpdateVehicleStatusModel request)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return this.Unauthorized();

            var userRoles = User.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value);

            var (success, vehicle, errorMessage) = await _vehicleService.UpdateVehicleStatusAsync(id, request, userId, userRoles);

            if (!success)
            {
                return errorMessage == "Vehicle not found"
                    ? this.NotFound()
                    : this.Forbid();
            }

            return this.Ok(vehicle);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating vehicle status {VehicleId}", id);
            return this.StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
        }
    }

    /// <summary>
    /// Log vehicle maintenance
    /// </summary>
    [HttpPost("{id}/maintenance")]
    [Authorize(Roles = AuthRoles.Driver + "," + AuthRoles.Admin + "," + AuthRoles.SuperAdmin)]
    [ProducesResponseType(typeof(VehicleViewModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VehicleViewModel>> LogMaintenance(Guid id, [FromBody] LogMaintenanceModel request)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return this.Unauthorized();

            var userRoles = User.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value);

            var (success, vehicle, errorMessage) = await _vehicleService.LogMaintenanceAsync(id, request, userId, userRoles);

            if (!success)
            {
                return errorMessage == "Vehicle not found"
                    ? this.NotFound()
                    : this.Forbid();
            }

            return this.Ok(vehicle);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging maintenance for vehicle {VehicleId}", id);
            return this.StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
        }
    }

    /// <summary>
    /// Get vehicle maintenance history
    /// </summary>
    [HttpGet("{id}/maintenance-history")]
    [ProducesResponseType(typeof(List<MaintenanceHistoryViewModel>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<MaintenanceHistoryViewModel>>> GetMaintenanceHistory(Guid id)
    {
        try
        {
            var vehicle = await _vehicleService.GetVehicleByIdAsync(id);
            if (vehicle is null)
                return this.NotFound();

            var result = await _vehicleService.GetMaintenanceHistoryAsync(id);

            return this.Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting maintenance history for vehicle {VehicleId}", id);
            return this.StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
        }
    }

    /// <summary>
    /// Delete a vehicle (soft delete)
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = AuthRoles.Driver + "," + AuthRoles.Admin + "," + AuthRoles.SuperAdmin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteVehicle(Guid id)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return this.Unauthorized();

            var userRoles = User.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value);

            var (success, errorMessage) = await _vehicleService.DeleteVehicleAsync(id, userId, userRoles);

            if (!success)
            {
                return errorMessage == "Vehicle not found"
                    ? this.NotFound()
                    : this.Forbid();
            }

            return this.NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting vehicle {VehicleId}", id);
            return this.StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
        }
    }
}
