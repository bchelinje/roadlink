using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BeC.OpenId.Connect.Features.Debug.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DebugAuthController : ControllerBase
{
    /// <summary>
    /// Debug endpoint to check authentication status and claims
    /// </summary>
    [HttpGet("whoami")]
    [Authorize]
    public IActionResult WhoAmI()
    {
        var claims = User.Claims.Select(c => new
        {
            Type = c.Type,
            Value = c.Value
        }).ToList();

        var roles = User.Claims
            .Where(c => c.Type == ClaimTypes.Role || c.Type == "role")
            .Select(c => c.Value)
            .ToList();

        return Ok(new
        {
            IsAuthenticated = User.Identity?.IsAuthenticated ?? false,
            AuthenticationType = User.Identity?.AuthenticationType,
            Name = User.Identity?.Name,
            UserId = User.FindFirstValue(ClaimTypes.NameIdentifier),
            Roles = roles,
            AllClaims = claims
        });
    }

    /// <summary>
    /// Test admin authorization
    /// </summary>
    [HttpGet("test-admin")]
    [Authorize(Policy = Infrastructure.Authorization.Policies.RequireAdminOrSuperAdmin)]
    public IActionResult TestAdmin()
    {
        return Ok(new { Message = "You have admin access!", User = User.Identity?.Name });
    }
}
