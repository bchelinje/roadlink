using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Validation.AspNetCore;

namespace BeC.OpenId.Connect.Features.Ping.Controllers;

/// <summary>
/// Health check and API testing endpoint
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class PingController : ControllerBase
{
    /// <summary>
    /// Check if the API is accessible and responding
    /// </summary>
    /// <returns>Status and current UTC time</returns>
    /// <response code="200">API is healthy and accessible</response>
    /// <response code="401">Unauthorized - Valid bearer token required</response>
    [HttpGet]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [ProducesResponseType(typeof(PingResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult Get()
    {
        var response = new PingResponse
        {
            Ok = true,
            Time = DateTimeOffset.UtcNow,
            Message = "API is running",
            User = User.Identity?.Name ?? "Anonymous"
        };

        return Ok(response);
    }

    /// <summary>
    /// Public ping endpoint (no authentication required)
    /// </summary>
    /// <returns>Public status message</returns>
    /// <response code="200">Server is responding</response>
    [HttpGet("public")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PingResponse), StatusCodes.Status200OK)]
    public IActionResult GetPublic()
    {
        var response = new PingResponse
        {
            Ok = true,
            Time = DateTimeOffset.UtcNow,
            Message = "Public endpoint - No authentication required"
        };

        return Ok(response);
    }
}

/// <summary>
/// Ping response model
/// </summary>
public class PingResponse
{
    /// <summary>
    /// Indicates if the service is operational
    /// </summary>
    public bool Ok { get; set; }

    /// <summary>
    /// Current UTC timestamp
    /// </summary>
    public DateTimeOffset Time { get; set; }

    /// <summary>
    /// Status message
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Authenticated user (if applicable)
    /// </summary>
    public string? User { get; set; }
}