using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Validation.AspNetCore;
using System.Security.Claims;
using BeC.OpenId.Connect.Features.Users.Dtos;
using BeC.OpenId.Connect.Features.ActivityLogs.Services.Interfaces;

namespace BeC.OpenId.Connect.Features.Roles.Controllers;

/// <summary>
/// Role management endpoints
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(
    AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme,
    Roles = Infrastructure.Authorization.Roles.SuperAdmin)]
[Produces("application/json")]
public class RolesController : ControllerBase
{
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IActivityLogService _activityLogService;
    private readonly ILogger<RolesController> _logger;

    public RolesController(
        RoleManager<IdentityRole> roleManager,
        UserManager<ApplicationUser> userManager,
        IActivityLogService activityLogService,
        ILogger<RolesController> logger)
    {
        _roleManager = roleManager;
        _userManager = userManager;
        _activityLogService = activityLogService;
        _logger = logger;
    }

    /// <summary>
    /// Get all roles
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<RoleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<List<RoleDto>>> GetRoles()
    {
        var roles = await _roleManager.Roles.ToListAsync();
        var roleDtos = new List<RoleDto>();

        foreach (var role in roles)
        {
            var claims = await _roleManager.GetClaimsAsync(role);
            var userCount = await _userManager.GetUsersInRoleAsync(role.Name!);

            roleDtos.Add(new RoleDto
            {
                Id = role.Id,
                Name = role.Name!,
                NormalizedName = role.NormalizedName!,
                UserCount = userCount.Count,
                Description = claims.FirstOrDefault(c => c.Type == "description")?.Value
            });
        }

        return Ok(roleDtos);
    }

    /// <summary>
    /// Get role by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(RoleDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<RoleDetailDto>> GetRole(string id)
    {
        var role = await _roleManager.FindByIdAsync(id);
        if (role == null)
            return NotFound(new { message = "Role not found" });

        var claims = await _roleManager.GetClaimsAsync(role);
        var usersInRole = await _userManager.GetUsersInRoleAsync(role.Name!);

        return Ok(new RoleDetailDto
        {
            Id = role.Id,
            Name = role.Name!,
            NormalizedName = role.NormalizedName!,
            UserCount = usersInRole.Count,
            Description = claims.FirstOrDefault(c => c.Type == "description")?.Value,
            Claims = claims.Select(c => new ClaimDto 
            { 
                Type = c.Type, 
                Value = c.Value 
            }).ToList(),
            Users = usersInRole.Select(u => new UserSummaryDto
            {
                Id = u.Id,
                Email = u.Email!,
                UserName = u.UserName!
            }).ToList()
        });
    }

    /// <summary>
    /// Create new role
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(RoleDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<RoleDto>> CreateRole([FromBody] CreateRoleRequest request)
    {
        if (await _roleManager.RoleExistsAsync(request.Name))
            return BadRequest(new { message = $"Role '{request.Name}' already exists" });

        var role = new IdentityRole(request.Name);
        var result = await _roleManager.CreateAsync(role);

        if (!result.Succeeded)
        {
            return BadRequest(new
            {
                message = "Failed to create role",
                errors = result.Errors.Select(e => e.Description)
            });
        }

        // Add description claim if provided
        if (!string.IsNullOrWhiteSpace(request.Description))
        {
            await _roleManager.AddClaimAsync(role, 
                new Claim("description", request.Description));
        }

        _logger.LogInformation("Role '{RoleName}' created successfully", role.Name);

        // 🎯 LOG: Role created
        await _activityLogService.LogActivityAsync(
            action: "ROLE_CREATED",
            entityType: "ROLE",
            entityId: role.Id,
            entityName: role.Name,
            description: $"Role '{role.Name}' created",
            severity: "INFO",
            metadata: new Dictionary<string, object>
            {
                ["RoleName"] = role.Name ?? "unknown",
                ["Description"] = request.Description ?? "none",
                ["CreatedBy"] = User.Identity?.Name ?? "system"
            }
        );

        return CreatedAtAction(
            nameof(GetRole),
            new { id = role.Id },
            new RoleDto
            {
                Id = role.Id,
                Name = role.Name!,
                NormalizedName = role.NormalizedName!,
                UserCount = 0,
                Description = request.Description
            });
    }

    /// <summary>
    /// Update role
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateRole(string id, [FromBody] UpdateRoleRequest request)
    {
        var role = await _roleManager.FindByIdAsync(id);
        if (role == null)
            return NotFound(new { message = "Role not found" });

        // Prevent modifying system roles
        if (Infrastructure.Authorization.Roles.All.Contains(role.Name))
            return BadRequest(new { message = "Cannot modify system roles" });

        var oldRoleName = role.Name;
        var changes = new List<string>();

        // Update name if provided
        if (!string.IsNullOrWhiteSpace(request.Name) && request.Name != role.Name)
        {
            changes.Add($"Name: {role.Name} → {request.Name}");
            role.Name = request.Name;
            var result = await _roleManager.UpdateAsync(role);
            
            if (!result.Succeeded)
            {
                return BadRequest(new
                {
                    message = "Failed to update role",
                    errors = result.Errors.Select(e => e.Description)
                });
            }
        }

        // Update description if provided
        if (request.Description != null)
        {
            var claims = await _roleManager.GetClaimsAsync(role);
            var descriptionClaim = claims.FirstOrDefault(c => c.Type == "description");
            
            var oldDescription = descriptionClaim?.Value ?? "none";
            changes.Add($"Description: {oldDescription} → {request.Description}");
            
            if (descriptionClaim != null)
                await _roleManager.RemoveClaimAsync(role, descriptionClaim);
            
            if (!string.IsNullOrWhiteSpace(request.Description))
                await _roleManager.AddClaimAsync(role, new Claim("description", request.Description));
        }

        _logger.LogInformation("Role '{RoleName}' updated successfully", role.Name);

        // 🎯 LOG: Role updated
        if (changes.Any())
        {
            await _activityLogService.LogActivityAsync(
                action: "ROLE_UPDATED",
                entityType: "ROLE",
                entityId: role.Id,
                entityName: role.Name,
                description: $"Role '{oldRoleName}' updated",
                severity: "INFO",
                metadata: new Dictionary<string, object>
                {
                    ["RoleId"] = role.Id,
                    ["OldName"] = oldRoleName ?? "unknown",
                    ["NewName"] = role.Name ?? "unknown",
                    ["Changes"] = string.Join("; ", changes),
                    ["UpdatedBy"] = User.Identity?.Name ?? "system"
                }
            );
        }

        return Ok(new { message = "Role updated successfully" });
    }

    /// <summary>
    /// Delete role
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteRole(string id)
    {
        var role = await _roleManager.FindByIdAsync(id);
        if (role == null)
            return NotFound(new { message = "Role not found" });

        var roleName = role.Name;

        // Prevent deleting system roles
        if (Infrastructure.Authorization.Roles.All.Contains(role.Name))
            return BadRequest(new { message = "Cannot delete system roles" });

        // Check if role has users
        var usersInRole = await _userManager.GetUsersInRoleAsync(role.Name!);
        if (usersInRole.Any())
        {
            // 🎯 LOG: Role deletion attempt blocked
            await _activityLogService.LogActivityAsync(
                action: "ROLE_DELETE_BLOCKED",
                entityType: "ROLE",
                entityId: role.Id,
                entityName: role.Name,
                description: $"Attempt to delete role '{role.Name}' blocked - role has {usersInRole.Count} assigned users",
                severity: "WARNING",
                metadata: new Dictionary<string, object>
                {
                    ["RoleId"] = role.Id,
                    ["RoleName"] = role.Name ?? "unknown",
                    ["UserCount"] = usersInRole.Count,
                    ["AttemptedBy"] = User.Identity?.Name ?? "system"
                }
            );

            return BadRequest(new 
            { 
                message = "Cannot delete role with assigned users",
                userCount = usersInRole.Count
            });
        }

        var result = await _roleManager.DeleteAsync(role);
        
        if (result.Succeeded)
        {
            _logger.LogInformation("Role '{RoleName}' deleted successfully", roleName);

            // 🎯 LOG: Role deleted
            await _activityLogService.LogActivityAsync(
                action: "ROLE_DELETED",
                entityType: "ROLE",
                entityId: id,
                entityName: roleName,
                description: $"Role '{roleName}' deleted",
                severity: "WARNING",
                metadata: new Dictionary<string, object>
                {
                    ["RoleId"] = id,
                    ["RoleName"] = roleName ?? "unknown",
                    ["DeletedBy"] = User.Identity?.Name ?? "system"
                }
            );

            return Ok(new { message = "Role deleted successfully" });
        }

        return BadRequest(new
        {
            message = "Failed to delete role",
            errors = result.Errors.Select(e => e.Description)
        });
    }

    /// <summary>
    /// Get users in a specific role
    /// </summary>
    [HttpGet("{roleName}/users")]
    [ProducesResponseType(typeof(List<UserSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<List<UserSummaryDto>>> GetUsersInRole(string roleName)
    {
        if (!await _roleManager.RoleExistsAsync(roleName))
            return NotFound(new { message = $"Role '{roleName}' not found" });

        var users = await _userManager.GetUsersInRoleAsync(roleName);

        var userDtos = users.Select(u => new UserSummaryDto
        {
            Id = u.Id,
            Email = u.Email!,
            UserName = u.UserName!,
            EmailConfirmed = u.EmailConfirmed,
            LockoutEnd = u.LockoutEnd
        }).ToList();

        return Ok(userDtos);
    }
}

#region DTOs

public record RoleDto
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string NormalizedName { get; init; } = string.Empty;
    public int UserCount { get; init; }
    public string? Description { get; init; }
}

public record RoleDetailDto
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string NormalizedName { get; init; } = string.Empty;
    public int UserCount { get; init; }
    public string? Description { get; init; }
    public List<ClaimDto> Claims { get; init; } = new();
    public List<UserSummaryDto> Users { get; init; } = new();
}

public record ClaimDto
{
    public string Type { get; init; } = string.Empty;
    public string Value { get; init; } = string.Empty;
}

public record UserSummaryDto
{
    public string Id { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string UserName { get; init; } = string.Empty;
    public bool EmailConfirmed { get; init; }
    public DateTimeOffset? LockoutEnd { get; init; }
}

public record CreateRoleRequest
{
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
}

public record UpdateRoleRequest
{
    public string? Name { get; init; }
    public string? Description { get; init; }
}

#endregion