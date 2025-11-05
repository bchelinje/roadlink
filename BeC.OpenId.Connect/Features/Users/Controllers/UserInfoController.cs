
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Validation.AspNetCore;
using System.Security.Claims;

namespace BeC.OpenId.Connect.Features.Users.Controllers
{
    [Route("connect")]
    [ApiController]
    public class UserInfoController : ControllerBase
    {
        /// <summary>
        /// Get user information from the authenticated token
        /// This endpoint accepts both JWT and JWE tokens
        /// </summary>
        [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
        [HttpGet("userinfo")]
        public IActionResult GetUserInfo()
        {
            // Get all claims from the authenticated user
            var claims = User.Claims.ToList();

            // Log for debugging
            foreach (var claim in claims)
            {
                Console.WriteLine($"Claim: {claim.Type} = {claim.Value}");
            }

            // Extract user information from claims
            var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) 
                     ?? User.FindFirstValue("sub")
                     ?? User.Identity?.Name;

            var email = User.FindFirstValue(ClaimTypes.Email)
                       ?? User.FindFirstValue("email")
                       ?? User.FindFirstValue("preferred_username");

            var name = User.FindFirstValue(ClaimTypes.Name)
                      ?? User.FindFirstValue("name")
                      ?? User.FindFirstValue("given_name")
                      ?? email?.Split('@')[0];

            // Get all roles
            var roles = User.FindAll(ClaimTypes.Role)
                           .Select(c => c.Value)
                           .ToList();

            // If no roles found, try alternative claim type
            if (!roles.Any())
            {
                roles = User.FindAll("role")
                           .Select(c => c.Value)
                           .ToList();
            }

            var userInfo = new
            {
                sub = sub,
                email = email,
                name = name,
                role = roles,
                email_verified = true,
                preferred_username = email
            };

            return Ok(userInfo);
        }
    }
}