using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BeC.OpenId.Connect.Dto;
using BeC.OpenId.Connect.Features.Location.Dtos;
using BeC.OpenId.Connect.Features.Users.Dtos;
using BeC.OpenId.Connect.Infrastructure.Authorization;
using BeC.OpenId.Connect.Infrastructure.Maps;
using BeC.OpenId.Connect.Features.Notifications.Services.Interfaces;

namespace BeC.OpenId.Connect.Features.Location.Controllers;

/// <summary>
/// API endpoints for driver location tracking and ETA calculations
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LocationController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IGoogleMapsService _mapsService;
    private readonly IRealtimeNotificationService _notificationService;
    private readonly ILogger<LocationController> _logger;

    public LocationController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IGoogleMapsService mapsService,
        IRealtimeNotificationService notificationService,
        ILogger<LocationController> logger)
    {
        _context = context;
        _userManager = userManager;
        _mapsService = mapsService;
        _notificationService = notificationService;
        _logger = logger;
    }

    /// <summary>
    /// Update driver's current location (Driver only)
    /// </summary>
    /// <param name="request">Location update request</param>
    /// <returns>Updated location</returns>
    [HttpPost("drivers/me/location")]
    [Authorize(Policy = Policies.RequireDriverRole)]
    [ProducesResponseType(typeof(DriverLocation), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DriverLocation>> UpdateDriverLocation([FromBody] UpdateLocationRequest request)
    {
        try
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized(new { message = "User not found" });
            }

            // Get driver profile
            var driver = await _context.Drivers
                .FirstOrDefaultAsync(d => d.UserId == user.Id);

            if (driver == null)
            {
                return NotFound(new { message = "Driver profile not found" });
            }

            // Validate coordinates
            if (request.Latitude < -90 || request.Latitude > 90 ||
                request.Longitude < -180 || request.Longitude > 180)
            {
                return BadRequest(new { message = "Invalid coordinates" });
            }

            // Reverse geocode to get address
            string? address = null;
            try
            {
                address = await _mapsService.ReverseGeocodeAsync(request.Latitude, request.Longitude);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to reverse geocode location");
            }

            // Create or update location record
            var location = new DriverLocation
            {
                DriverId = driver.Id,
                Latitude = request.Latitude,
                Longitude = request.Longitude,
                Accuracy = request.Accuracy,
                Speed = request.Speed,
                Heading = request.Heading,
                CurrentJobId = request.CurrentJobId,
                Address = address,
                Timestamp = DateTime.UtcNow
            };

            _context.DriverLocations.Add(location);
            await _context.SaveChangesAsync();

            // If driver is on a job, notify customer of location update
            if (request.CurrentJobId.HasValue)
            {
                var job = await _context.Jobs
                    .Include(j => j.Customer)
                    .FirstOrDefaultAsync(j => j.Id == request.CurrentJobId.Value);

                if (job != null)
                {
                    await _notificationService.SendToUserAsync(
                        job.Customer.UserId,
                        "driver_location_updated",
                        new
                        {
                            jobId = job.Id,
                            driverName = $"{driver.FirstName} {driver.LastName}",
                            latitude = location.Latitude,
                            longitude = location.Longitude,
                            timestamp = location.Timestamp
                        });
                }
            }

            _logger.LogInformation("Updated location for driver {DriverId} at {Lat},{Lng}",
                driver.Id, request.Latitude, request.Longitude);

            return Ok(location);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating driver location");
            return StatusCode(500, new { message = "Error updating location", error = ex.Message });
        }
    }

    /// <summary>
    /// Get driver's current location for a specific job (Customer only)
    /// </summary>
    /// <param name="jobId">Job ID</param>
    /// <returns>Driver's current location</returns>
    [HttpGet("jobs/{jobId}/driver-location")]
    [ProducesResponseType(typeof(DriverLocationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DriverLocationResponse>> GetDriverLocationForJob(Guid jobId)
    {
        try
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized(new { message = "User not found" });
            }

            // Get job and verify authorization
            var job = await _context.Jobs
                .Include(j => j.Customer)
                .Include(j => j.AssignedDriver)
                .FirstOrDefaultAsync(j => j.Id == jobId);

            if (job == null)
            {
                return NotFound(new { message = "Job not found" });
            }

            // Check if user is customer or driver or admin
            var isCustomer = job.Customer.UserId == user.Id;
            var isDriver = job.AssignedDriver?.UserId == user.Id;
            var isAdmin = User.IsInRole(Infrastructure.Authorization.Roles.Admin) || User.IsInRole(Infrastructure.Authorization.Roles.SuperAdmin);

            if (!isCustomer && !isDriver && !isAdmin)
            {
                return Forbid();
            }

            if (job.AssignedDriverId == null)
            {
                return NotFound(new { message = "No driver assigned to this job" });
            }

            // Get most recent location
            var location = await _context.DriverLocations
                .Where(l => l.DriverId == job.AssignedDriverId.Value)
                .OrderByDescending(l => l.Timestamp)
                .FirstOrDefaultAsync();

            if (location == null)
            {
                return NotFound(new { message = "Driver location not available" });
            }

            var ageInSeconds = (int)(DateTime.UtcNow - location.Timestamp).TotalSeconds;

            var response = new DriverLocationResponse
            {
                DriverId = job.AssignedDriver!.Id,
                DriverName = $"{job.AssignedDriver.FirstName} {job.AssignedDriver.LastName}",
                Latitude = location.Latitude,
                Longitude = location.Longitude,
                Speed = location.Speed,
                Heading = location.Heading,
                Address = location.Address,
                Timestamp = location.Timestamp,
                AgeInSeconds = ageInSeconds
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting driver location for job {JobId}", jobId);
            return StatusCode(500, new { message = "Error getting driver location", error = ex.Message });
        }
    }

    /// <summary>
    /// Calculate ETA for driver to reach job location
    /// </summary>
    /// <param name="jobId">Job ID</param>
    /// <returns>ETA calculation with estimated arrival time</returns>
    [HttpGet("jobs/{jobId}/eta")]
    [ProducesResponseType(typeof(EtaCalculationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EtaCalculationResult>> CalculateEta(Guid jobId)
    {
        try
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized(new { message = "User not found" });
            }

            // Get job and verify authorization
            var job = await _context.Jobs
                .Include(j => j.Customer)
                .Include(j => j.AssignedDriver)
                .FirstOrDefaultAsync(j => j.Id == jobId);

            if (job == null)
            {
                return NotFound(new { message = "Job not found" });
            }

            // Check authorization
            var isCustomer = job.Customer.UserId == user.Id;
            var isDriver = job.AssignedDriver?.UserId == user.Id;
            var isAdmin = User.IsInRole(Infrastructure.Authorization.Roles.Admin) || User.IsInRole(Infrastructure.Authorization.Roles.SuperAdmin);

            if (!isCustomer && !isDriver && !isAdmin)
            {
                return Forbid();
            }

            if (job.AssignedDriverId == null)
            {
                return NotFound(new { message = "No driver assigned to this job" });
            }

            // Get driver's current location
            var driverLocation = await _context.DriverLocations
                .Where(l => l.DriverId == job.AssignedDriverId.Value)
                .OrderByDescending(l => l.Timestamp)
                .FirstOrDefaultAsync();

            if (driverLocation == null)
            {
                return NotFound(new { message = "Driver location not available" });
            }

            // Check if location is too old (more than 5 minutes)
            if ((DateTime.UtcNow - driverLocation.Timestamp).TotalMinutes > 5)
            {
                return BadRequest(new { message = "Driver location is outdated. Last updated " +
                    $"{(int)(DateTime.UtcNow - driverLocation.Timestamp).TotalMinutes} minutes ago." });
            }

            // Get destination coordinates (pickup address)
            var destinationGeocode = await _mapsService.GeocodeAddressAsync(job.PickupAddress);

            if (destinationGeocode == null)
            {
                return BadRequest(new { message = "Unable to geocode destination address" });
            }

            // Calculate route distance and duration
            var route = await _mapsService.CalculateDistanceAsync(
                driverLocation.Latitude,
                driverLocation.Longitude,
                destinationGeocode.Latitude,
                destinationGeocode.Longitude);

            if (route == null)
            {
                return BadRequest(new { message = "Unable to calculate route" });
            }

            var eta = DateTime.UtcNow.AddMinutes(route.DurationInMinutes);

            var result = new EtaCalculationResult
            {
                JobId = job.Id,
                DriverId = job.AssignedDriver!.Id,
                DriverName = $"{job.AssignedDriver.FirstName} {job.AssignedDriver.LastName}",
                CurrentLatitude = driverLocation.Latitude,
                CurrentLongitude = driverLocation.Longitude,
                DestinationLatitude = destinationGeocode.Latitude,
                DestinationLongitude = destinationGeocode.Longitude,
                DestinationAddress = job.PickupAddress,
                DistanceInMiles = route.DistanceInMiles,
                DurationInMinutes = route.DurationInMinutes,
                EstimatedArrivalTime = eta,
                DurationText = route.DurationText,
                DistanceText = route.DistanceText
            };

            _logger.LogInformation("Calculated ETA for job {JobId}: {Duration} minutes",
                jobId, route.DurationInMinutes);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating ETA for job {JobId}", jobId);
            return StatusCode(500, new { message = "Error calculating ETA", error = ex.Message });
        }
    }

    /// <summary>
    /// Get driver's location history (Admin only)
    /// </summary>
    /// <param name="driverId">Driver ID</param>
    /// <param name="startDate">Start date for history</param>
    /// <param name="endDate">End date for history</param>
    /// <returns>List of driver locations</returns>
    [HttpGet("drivers/{driverId}/history")]
    [Authorize(Policy = Policies.RequireAdminOrSuperAdmin)]
    [ProducesResponseType(typeof(List<DriverLocation>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<DriverLocation>>> GetDriverLocationHistory(
        Guid driverId,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        try
        {
            var driver = await _context.Drivers.FindAsync(driverId);
            if (driver == null)
            {
                return NotFound(new { message = "Driver not found" });
            }

            var query = _context.DriverLocations.Where(l => l.DriverId == driverId);

            if (startDate.HasValue)
            {
                query = query.Where(l => l.Timestamp >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(l => l.Timestamp <= endDate.Value);
            }

            var history = await query
                .OrderByDescending(l => l.Timestamp)
                .Take(1000) // Limit to prevent excessive data
                .ToListAsync();

            return Ok(history);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting location history for driver {DriverId}", driverId);
            return StatusCode(500, new { message = "Error getting location history", error = ex.Message });
        }
    }

    /// <summary>
    /// Get all active drivers with their last known locations (Admin only)
    /// </summary>
    /// <returns>List of active drivers with locations</returns>
    [HttpGet("drivers/active")]
    [Authorize(Policy = Policies.RequireAdminOrSuperAdmin)]
    [ProducesResponseType(typeof(List<DriverLocationResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<DriverLocationResponse>>> GetActiveDriverLocations()
    {
        try
        {
            // Get all drivers with recent locations (within last 30 minutes)
            var recentCutoff = DateTime.UtcNow.AddMinutes(-30);

            var activeDrivers = await _context.DriverLocations
                .Where(l => l.Timestamp >= recentCutoff)
                .GroupBy(l => l.DriverId)
                .Select(g => g.OrderByDescending(l => l.Timestamp).FirstOrDefault())
                .ToListAsync();

            var driverIds = activeDrivers.Select(l => l!.DriverId).ToList();

            var drivers = await _context.Drivers
                .Where(d => driverIds.Contains(d.Id))
                .ToDictionaryAsync(d => d.Id);

            var response = activeDrivers
                .Where(l => l != null && drivers.ContainsKey(l.DriverId))
                .Select(l =>
                {
                    var driver = drivers[l!.DriverId];
                    return new DriverLocationResponse
                    {
                        DriverId = driver.Id,
                        DriverName = $"{driver.FirstName} {driver.LastName}",
                        Latitude = l.Latitude,
                        Longitude = l.Longitude,
                        Speed = l.Speed,
                        Heading = l.Heading,
                        Address = l.Address,
                        Timestamp = l.Timestamp,
                        AgeInSeconds = (int)(DateTime.UtcNow - l.Timestamp).TotalSeconds
                    };
                })
                .ToList();

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active driver locations");
            return StatusCode(500, new { message = "Error getting active drivers", error = ex.Message });
        }
    }
}
