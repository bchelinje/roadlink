using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BeC.OpenId.Connect.Features.Location.Models;
using BeC.OpenId.Connect.Features.Location.Services.Interfaces;
using BeC.OpenId.Connect.Features.Location.ViewModels;
using BeC.OpenId.Connect.Infrastructure.Authorization;
using AuthRoles = BeC.OpenId.Connect.Infrastructure.Authorization.Roles;

namespace BeC.OpenId.Connect.Features.Location.Controllers;

/// <summary>
/// API endpoints for driver location tracking and ETA calculations
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LocationController : ControllerBase
{
    private readonly ILocationService _locationService;
    private readonly ILogger<LocationController> _logger;

    public LocationController(
        ILocationService locationService,
        ILogger<LocationController> logger)
    {
        _locationService = locationService;
        _logger = logger;
    }

    /// <summary>
    /// Update driver's current location (Driver only)
    /// </summary>
    [HttpPost("drivers/me/location")]
    [Authorize(Policy = Policies.RequireDriverRole)]
    [ProducesResponseType(typeof(LocationViewModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LocationViewModel>> UpdateDriverLocation([FromBody] UpdateLocationModel request)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return this.Unauthorized();

            var result = await _locationService.UpdateDriverLocationAsync(request, userId);

            return this.Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return this.NotFound(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return this.BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating driver location");
            return this.StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error updating location", error = ex.Message });
        }
    }

    /// <summary>
    /// Get driver's current location for a specific job (Customer/Driver/Admin)
    /// </summary>
    [HttpGet("jobs/{jobId}/driver-location")]
    [ProducesResponseType(typeof(DriverLocationViewModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DriverLocationViewModel>> GetDriverLocationForJob(Guid jobId)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return this.Unauthorized();

            var userRoles = User.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value);

            var (success, location, errorMessage) = await _locationService.GetDriverLocationForJobAsync(jobId, userId, userRoles);

            if (!success)
            {
                return errorMessage == "Forbidden"
                    ? this.Forbid()
                    : this.NotFound(new { message = errorMessage });
            }

            return this.Ok(location);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting driver location for job {JobId}", jobId);
            return this.StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error getting driver location", error = ex.Message });
        }
    }

    /// <summary>
    /// Calculate ETA for driver to reach job location (Customer/Driver/Admin)
    /// </summary>
    [HttpGet("jobs/{jobId}/eta")]
    [ProducesResponseType(typeof(EtaViewModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EtaViewModel>> CalculateEta(Guid jobId)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return this.Unauthorized();

            var userRoles = User.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value);

            var (success, eta, errorMessage) = await _locationService.CalculateEtaAsync(jobId, userId, userRoles);

            if (!success)
            {
                return errorMessage == "Forbidden"
                    ? this.Forbid()
                    : errorMessage?.Contains("outdated") == true || errorMessage?.Contains("geocode") == true || errorMessage?.Contains("calculate route") == true
                        ? this.BadRequest(new { message = errorMessage })
                        : this.NotFound(new { message = errorMessage });
            }

            return this.Ok(eta);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating ETA for job {JobId}", jobId);
            return this.StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error calculating ETA", error = ex.Message });
        }
    }

    /// <summary>
    /// Get driver's location history (Admin only)
    /// </summary>
    [HttpGet("drivers/{driverId}/history")]
    [Authorize(Policy = Policies.RequireAdminOrSuperAdmin)]
    [ProducesResponseType(typeof(List<LocationViewModel>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<LocationViewModel>>> GetDriverLocationHistory(
        Guid driverId,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        try
        {
            var result = await _locationService.GetDriverLocationHistoryAsync(driverId, startDate, endDate);

            return this.Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return this.NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting location history for driver {DriverId}", driverId);
            return this.StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error getting location history", error = ex.Message });
        }
    }

    /// <summary>
    /// Get all active drivers with their last known locations (Admin only)
    /// </summary>
    [HttpGet("drivers/active")]
    [Authorize(Policy = Policies.RequireAdminOrSuperAdmin)]
    [ProducesResponseType(typeof(List<DriverLocationViewModel>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<DriverLocationViewModel>>> GetActiveDriverLocations()
    {
        try
        {
            var result = await _locationService.GetActiveDriverLocationsAsync();

            return this.Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active driver locations");
            return this.StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error getting active drivers", error = ex.Message });
        }
    }
}
