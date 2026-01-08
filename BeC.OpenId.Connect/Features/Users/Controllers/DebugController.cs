using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BeC.OpenId.Connect.Features.Users.Controllers;

/// <summary>
/// Debug controller to help troubleshoot authentication and authorization issues
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DebugController : ControllerBase
{
    /// <summary>
    /// Get current user's claims from JWT token
    /// </summary>
    [HttpGet("whoami")]
    [ProducesResponseType(typeof(UserClaimsDebugInfo), StatusCodes.Status200OK)]
    public IActionResult WhoAmI()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var email = User.FindFirstValue(ClaimTypes.Email);
        var name = User.FindFirstValue(ClaimTypes.Name);
        var roles = User.Claims
            .Where(c => c.Type == ClaimTypes.Role || c.Type == "role")
            .Select(c => c.Value)
            .ToList();

        var allClaims = User.Claims
            .Select(c => new { c.Type, c.Value })
            .ToList();

        return Ok(new
        {
            UserId = userId,
            Email = email,
            Name = name,
            Roles = roles,
            AllClaims = allClaims,
            IsAuthenticated = User.Identity?.IsAuthenticated ?? false,
            AuthenticationType = User.Identity?.AuthenticationType
        });
    }
}

public class UserClaimsDebugInfo
{
    public string? UserId { get; set; }
    public string? Email { get; set; }
    public string? Name { get; set; }
    public List<string> Roles { get; set; } = new();
    public List<UserClaimInfo> AllClaims { get; set; } = new();
    public bool IsAuthenticated { get; set; }
    public string? AuthenticationType { get; set; }
}

public class UserClaimInfo
{
    public string Type { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}
