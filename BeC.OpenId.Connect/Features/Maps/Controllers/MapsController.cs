using Microsoft.AspNetCore.Mvc;
using BeC.OpenId.Connect.Infrastructure.Maps;

namespace BeC.OpenId.Connect.Features.Maps.Controllers;

/// <summary>
/// API endpoints for Google Maps services
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class MapsController : ControllerBase
{
    private readonly IGoogleMapsService _mapsService;
    private readonly ILogger<MapsController> _logger;

    public MapsController(
        IGoogleMapsService mapsService,
        ILogger<MapsController> logger)
    {
        _mapsService = mapsService;
        _logger = logger;
    }

    /// <summary>
    /// Autocomplete address suggestions
    /// </summary>
    /// <param name="input">Search text</param>
    /// <param name="sessionToken">Optional session token for billing optimization</param>
    /// <returns>List of address suggestions</returns>
    [HttpGet("autocomplete")]
    [ProducesResponseType(typeof(List<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<List<string>>> AutocompleteAddress(
        [FromQuery] string input,
        [FromQuery] string? sessionToken = null)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return BadRequest(new { message = "Input is required" });
            }

            var suggestions = await _mapsService.AutocompleteAddressAsync(input, sessionToken);
            return Ok(suggestions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error autocompleting address: {Input}", input);
            return StatusCode(500, new { message = "Error getting address suggestions", error = ex.Message });
        }
    }

    /// <summary>
    /// Calculate distance and duration between two addresses
    /// </summary>
    /// <param name="origin">Starting address</param>
    /// <param name="destination">Destination address</param>
    /// <returns>Distance and duration information</returns>
    [HttpGet("distance")]
    [ProducesResponseType(typeof(DistanceResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<DistanceResult>> CalculateDistance(
        [FromQuery] string origin,
        [FromQuery] string destination)
    {
        try
        {
            _logger.LogInformation("Calculating distance from '{Origin}' to '{Destination}'", origin, destination);

            if (string.IsNullOrWhiteSpace(origin) || string.IsNullOrWhiteSpace(destination))
            {
                _logger.LogWarning("Missing origin or destination");
                return BadRequest(new { message = "Origin and destination are required" });
            }

            var result = await _mapsService.CalculateDistanceAsync(origin, destination);

            if (result == null)
            {
                _logger.LogWarning("Distance calculation returned null for {Origin} -> {Destination}", origin, destination);
                return BadRequest(new {
                    message = "Could not calculate distance. Please check your addresses and ensure they are valid locations.",
                    details = "Google Maps API may have rejected the request. Check that the addresses are complete and valid."
                });
            }

            _logger.LogInformation("Distance calculated successfully: {Distance} miles", result.DistanceInMiles);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating distance between {Origin} and {Destination}", origin, destination);
            return StatusCode(500, new {
                message = "Error calculating distance",
                error = ex.Message,
                details = "Check server logs for more details. Ensure Google Maps API key is valid and has Distance Matrix API enabled."
            });
        }
    }

    /// <summary>
    /// Geocode an address to get coordinates
    /// </summary>
    /// <param name="address">Address to geocode</param>
    /// <returns>Geocode result with latitude and longitude</returns>
    [HttpGet("geocode")]
    [ProducesResponseType(typeof(GeocodeResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<GeocodeResult>> GeocodeAddress([FromQuery] string address)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(address))
            {
                return BadRequest(new { message = "Address is required" });
            }

            var result = await _mapsService.GeocodeAddressAsync(address);

            if (result == null)
            {
                return BadRequest(new { message = "Could not geocode address" });
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error geocoding address: {Address}", address);
            return StatusCode(500, new { message = "Error geocoding address", error = ex.Message });
        }
    }

    /// <summary>
    /// Reverse geocode coordinates to get an address
    /// </summary>
    /// <param name="latitude">Latitude</param>
    /// <param name="longitude">Longitude</param>
    /// <returns>Formatted address</returns>
    [HttpGet("reverse-geocode")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<string>> ReverseGeocode(
        [FromQuery] double latitude,
        [FromQuery] double longitude)
    {
        try
        {
            var address = await _mapsService.ReverseGeocodeAsync(latitude, longitude);

            if (string.IsNullOrEmpty(address))
            {
                return BadRequest(new { message = "Could not reverse geocode coordinates" });
            }

            return Ok(new { address });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reverse geocoding: {Lat}, {Lng}", latitude, longitude);
            return StatusCode(500, new { message = "Error reverse geocoding", error = ex.Message });
        }
    }

    /// <summary>
    /// Get optimized route with waypoints
    /// </summary>
    /// <param name="origin">Starting point</param>
    /// <param name="destination">End point</param>
    /// <param name="waypoints">Optional waypoints to visit</param>
    /// <returns>Optimized route information</returns>
    [HttpPost("route")]
    [ProducesResponseType(typeof(RouteResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<RouteResult>> GetOptimizedRoute(
        [FromBody] RouteRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Origin) || string.IsNullOrWhiteSpace(request.Destination))
            {
                return BadRequest(new { message = "Origin and destination are required" });
            }

            var result = await _mapsService.GetOptimizedRouteAsync(
                request.Origin,
                request.Destination,
                request.Waypoints);

            if (result == null)
            {
                return BadRequest(new { message = "Could not calculate route" });
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating route");
            return StatusCode(500, new { message = "Error calculating route", error = ex.Message });
        }
    }
}

#region DTOs

public class RouteRequest
{
    public required string Origin { get; set; }
    public required string Destination { get; set; }
    public List<string>? Waypoints { get; set; }
}

#endregion
