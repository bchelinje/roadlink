using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Validation.AspNetCore;
using BeC.OpenId.Connect.Infrastructure.Authorization;

namespace BeC.OpenId.Connect.Features.Demo.Controllers;

/// <summary>
/// Demo endpoints showing different authorization levels
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
[Produces("application/json")]
public class DemoController : ControllerBase
{
    /// <summary>
    /// Public endpoint - accessible to all authenticated users
    /// </summary>
    [HttpGet("public")]
    [ProducesResponseType(typeof(DemoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult Public()
    {
        return Ok(new DemoResponse
        {
            Message = "This endpoint is accessible to all authenticated users",
            UserName = User.Identity?.Name ?? "Unknown",
            Roles = User.Claims
                .Where(c => c.Type == "role" || c.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role")
                .Select(c => c.Value)
                .ToList(),
            Timestamp = DateTimeOffset.UtcNow
        });
    }

    /// <summary>
    /// Customer-only endpoint - accessible to customers
    /// </summary>
    [HttpGet("customer")]
    [Authorize(Roles = Infrastructure.Authorization.Roles.Customer)]
    [ProducesResponseType(typeof(DemoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public IActionResult CustomerOnly()
    {
        return Ok(new DemoResponse
        {
            Message = "Welcome! This is a customer-only area",
            UserName = User.Identity?.Name ?? "Unknown",
            Roles = User.Claims
                .Where(c => c.Type == "role" || c.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role")
                .Select(c => c.Value)
                .ToList(),
            Timestamp = DateTimeOffset.UtcNow
        });
    }

    /// <summary>
    /// Driver-only endpoint - accessible to drivers
    /// </summary>
    [HttpGet("driver")]
    [Authorize(Roles = Infrastructure.Authorization.Roles.Driver)]
    [ProducesResponseType(typeof(DemoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public IActionResult DriverOnly()
    {
        return Ok(new DemoResponse
        {
            Message = "Driver dashboard - manage your jobs here",
            UserName = User.Identity?.Name ?? "Unknown",
            Roles = User.Claims
                .Where(c => c.Type == "role" || c.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role")
                .Select(c => c.Value)
                .ToList(),
            Timestamp = DateTimeOffset.UtcNow
        });
    }

    /// <summary>
    /// Admin-only endpoint - accessible to admins and super admins
    /// </summary>
    [HttpGet("admin")]
    [Authorize(Roles = Infrastructure.Authorization.Roles.Admin + "," + Infrastructure.Authorization.Roles.SuperAdmin)]
    [ProducesResponseType(typeof(DemoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public IActionResult AdminOnly()
    {
        return Ok(new DemoResponse
        {
            Message = "Admin panel - manage system settings",
            UserName = User.Identity?.Name ?? "Unknown",
            Roles = User.Claims
                .Where(c => c.Type == "role" || c.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role")
                .Select(c => c.Value)
                .ToList(),
            Timestamp = DateTimeOffset.UtcNow
        });
    }

    /// <summary>
    /// SuperAdmin-only endpoint - accessible only to super admins
    /// </summary>
    [HttpGet("superadmin")]
    [Authorize(Roles = Infrastructure.Authorization.Roles.SuperAdmin)]
    [ProducesResponseType(typeof(DemoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public IActionResult SuperAdminOnly()
    {
        return Ok(new DemoResponse
        {
            Message = "SuperAdmin area - full system control",
            UserName = User.Identity?.Name ?? "Unknown",
            Roles = User.Claims
                .Where(c => c.Type == "role" || c.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role")
                .Select(c => c.Value)
                .ToList(),
            Timestamp = DateTimeOffset.UtcNow
        });
    }

    /// <summary>
    /// Policy-based endpoint - using authorization policy
    /// </summary>
    [HttpGet("driver-or-admin")]
    [Authorize(Policy = Policies.RequireDriverOrAdmin)]
    [ProducesResponseType(typeof(DemoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public IActionResult DriverOrAdmin()
    {
        return Ok(new DemoResponse
        {
            Message = "Accessible to drivers and admins using policy-based authorization",
            UserName = User.Identity?.Name ?? "Unknown",
            Roles = User.Claims
                .Where(c => c.Type == "role" || c.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role")
                .Select(c => c.Value)
                .ToList(),
            Timestamp = DateTimeOffset.UtcNow
        });
    }

    /// <summary>
    /// Get all claims for the current user
    /// </summary>
    [HttpGet("claims")]
    [ProducesResponseType(typeof(ClaimsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult GetClaims()
    {
        var claims = User.Claims.Select(c => new ClaimInfo
        {
            Type = c.Type,
            Value = c.Value,
            Issuer = c.Issuer
        }).ToList();

        return Ok(new ClaimsResponse
        {
            UserName = User.Identity?.Name ?? "Unknown",
            IsAuthenticated = User.Identity?.IsAuthenticated ?? false,
            AuthenticationType = User.Identity?.AuthenticationType ?? "Unknown",
            Claims = claims,
            Timestamp = DateTimeOffset.UtcNow
        });
    }
}

#region Response DTOs

public record DemoResponse
{
    public string Message { get; init; } = string.Empty;
    public string UserName { get; init; } = string.Empty;
    public List<string> Roles { get; init; } = new();
    public DateTimeOffset Timestamp { get; init; }
}

public record ClaimsResponse
{
    public string UserName { get; init; } = string.Empty;
    public bool IsAuthenticated { get; init; }
    public string AuthenticationType { get; init; } = string.Empty;
    public List<ClaimInfo> Claims { get; init; } = new();
    public DateTimeOffset Timestamp { get; init; }
}

public record ClaimInfo
{
    public string Type { get; init; } = string.Empty;
    public string Value { get; init; } = string.Empty;
    public string Issuer { get; init; } = string.Empty;
}

#endregion