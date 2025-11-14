using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Validation.AspNetCore;
using BeC.OpenId.Connect.Infrastructure.Authorization;
using BeC.OpenId.Connect.Infrastructure.Email;
using System.Text;
using BeC.OpenId.Connect.Features.Users.Dtos;
using BeC.OpenId.Connect.Features.ActivityLogs.Services.Interfaces;

namespace BeC.OpenId.Connect.Features.Users.Controllers;

/// <summary>
/// User management endpoints for administrators and authentication
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
[Produces("application/json")]
public class UsersController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IEmailService _emailService;
    private readonly ILogger<UsersController> _logger;
    private readonly IActivityLogService _activityLogService;

    public UsersController(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        SignInManager<ApplicationUser> signInManager,
        IEmailService emailService,
        ILogger<UsersController> logger,
        IActivityLogService activityLogService)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _signInManager = signInManager;
        _emailService = emailService;
        _logger = logger;
        _activityLogService = activityLogService;
    }

    #region User Management (Admin)

    /// <summary>
    /// Get all users (Admin only)
    /// </summary>
    [HttpGet]
    [Authorize(Roles = Infrastructure.Authorization.Roles.SuperAdmin + "," + Infrastructure.Authorization.Roles.Admin)]
    [ProducesResponseType(typeof(List<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<List<UserDto>>> GetUsers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? searchTerm = null)
    {
        var query = _userManager.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(u => 
                u.Email!.Contains(searchTerm) || 
                u.UserName!.Contains(searchTerm));
        }

        var totalCount = await query.CountAsync();
        var users = await query
            .OrderBy(u => u.Email)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var userDtos = new List<UserDto>();

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            userDtos.Add(new UserDto
            {
                Id = user.Id,
                Email = user.Email!,
                UserName = user.UserName!,
                EmailConfirmed = user.EmailConfirmed,
                PhoneNumber = user.PhoneNumber,
                Roles = roles.ToList(),
                LockoutEnabled = user.LockoutEnabled,
                LockoutEnd = user.LockoutEnd
            });
        }

        Response.Headers.Append("X-Total-Count", totalCount.ToString());
        Response.Headers.Append("X-Page", page.ToString());
        Response.Headers.Append("X-Page-Size", pageSize.ToString());

        return Ok(userDtos);
    }

    /// <summary>
    /// Get user by ID (Admin only)
    /// </summary>
    [HttpGet("{id}")]
    [Authorize(Roles = Infrastructure.Authorization.Roles.SuperAdmin + "," + Infrastructure.Authorization.Roles.Admin)]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<UserDto>> GetUser(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        
        if (user == null)
            return NotFound(new { message = "User not found" });

        var roles = await _userManager.GetRolesAsync(user);
        var claims = await _userManager.GetClaimsAsync(user);

        return Ok(new UserDto
        {
            Id = user.Id,
            Email = user.Email!,
            UserName = user.UserName!,
            EmailConfirmed = user.EmailConfirmed,
            PhoneNumber = user.PhoneNumber,
            PhoneNumberConfirmed = user.PhoneNumberConfirmed,
            Roles = roles.ToList(),
            LockoutEnabled = user.LockoutEnabled,
            LockoutEnd = user.LockoutEnd,
            TwoFactorEnabled = user.TwoFactorEnabled,
            Claims = claims.Select(c => new UserClaimDto
            { 
                Type = c.Type, 
                Value = c.Value 
            }).ToList()
        });
    }

    /// <summary>
    /// Get current user's profile
    /// </summary>
    [HttpGet("me")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserDto>> GetCurrentUser()
    {
        var user = await _userManager.GetUserAsync(User);
        
        if (user == null)
            return Unauthorized(new { message = "User not authenticated" });

        var roles = await _userManager.GetRolesAsync(user);
        var claims = await _userManager.GetClaimsAsync(user);

        return Ok(new UserDto
        {
            Id = user.Id,
            Email = user.Email!,
            UserName = user.UserName!,
            EmailConfirmed = user.EmailConfirmed,
            PhoneNumber = user.PhoneNumber,
            PhoneNumberConfirmed = user.PhoneNumberConfirmed,
            Roles = roles.ToList(),
            TwoFactorEnabled = user.TwoFactorEnabled,
            Claims = claims.Select(c => new UserClaimDto
            { 
                Type = c.Type, 
                Value = c.Value 
            }).ToList()
        });
    }

    /// <summary>
    /// Assign role to user (SuperAdmin only)
    /// </summary>
    [HttpPost("{id}/roles")]
    [Authorize(Roles = Infrastructure.Authorization.Roles.SuperAdmin)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> AssignRole(string id, [FromBody] AssignRoleRequest request)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
            return NotFound(new { message = "User not found" });

        if (!await _roleManager.RoleExistsAsync(request.RoleName))
            return BadRequest(new { message = $"Role '{request.RoleName}' does not exist" });

        if (await _userManager.IsInRoleAsync(user, request.RoleName))
            return BadRequest(new { message = $"User already has role '{request.RoleName}'" });

        var result = await _userManager.AddToRoleAsync(user, request.RoleName);

        if (result.Succeeded)
        {
            // 🎯 LOG: Role assigned
            await _activityLogService.LogActivityAsync(
                action: "ROLE_ASSIGNED",
                entityType: "USER",
                entityId: user.Id,
                entityName: user.UserName,
                description: $"Role '{request.RoleName}' assigned to user '{user.Email}'",
                severity: "INFO",
                metadata: new Dictionary<string, object>
                {
                    ["RoleName"] = request.RoleName,
                    ["UserEmail"] = user.Email ?? "unknown",
                    ["UserId"] = user.Id
                }
            );
            
            return Ok(new { message = $"Role '{request.RoleName}' assigned successfully" });
        }

        return BadRequest(new { 
            message = "Failed to assign role", 
            errors = result.Errors.Select(e => e.Description) 
        });
    }

    /// <summary>
    /// Remove role from user (SuperAdmin only)
    /// </summary>
    [HttpDelete("{id}/roles/{roleName}")]
    [Authorize(Roles = Infrastructure.Authorization.Roles.SuperAdmin)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> RemoveRole(string id, string roleName)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
            return NotFound(new { message = "User not found" });

        // Prevent removing SuperAdmin role from yourself
        var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser?.Id == id && roleName == Infrastructure.Authorization.Roles.SuperAdmin)
            return BadRequest(new { message = "You cannot remove SuperAdmin role from yourself" });

        if (!await _userManager.IsInRoleAsync(user, roleName))
            return BadRequest(new { message = $"User does not have role '{roleName}'" });

        var result = await _userManager.RemoveFromRoleAsync(user, roleName);

        if (result.Succeeded)
        {
            // 🎯 LOG: Role removed
            await _activityLogService.LogActivityAsync(
                action: "ROLE_REMOVED",
                entityType: "USER",
                entityId: user.Id,
                entityName: user.UserName,
                description: $"Role '{roleName}' removed from user '{user.Email}'",
                severity: "INFO",
                metadata: new Dictionary<string, object>
                {
                    ["RoleName"] = roleName,
                    ["UserEmail"] = user.Email ?? "unknown",
                    ["UserId"] = user.Id
                }
            );
            
            return Ok(new { message = $"Role '{roleName}' removed successfully" });
        }

        return BadRequest(new { 
            message = "Failed to remove role", 
            errors = result.Errors.Select(e => e.Description) 
        });
    }

    /// <summary>
    /// Lock/Suspend user account (Admin only)
    /// </summary>
    [HttpPost("{id}/lock")]
    [Authorize(Roles = Infrastructure.Authorization.Roles.SuperAdmin + "," + Infrastructure.Authorization.Roles.Admin)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> LockUser(string id, [FromBody] LockUserRequest request)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
            return NotFound(new { message = "User not found" });

        // Prevent locking SuperAdmin users
        if (await _userManager.IsInRoleAsync(user, Infrastructure.Authorization.Roles.SuperAdmin))
            return BadRequest(new { message = "Cannot lock SuperAdmin users" });

        // Prevent locking yourself
        var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser?.Id == id)
            return BadRequest(new { message = "You cannot lock your own account" });

        var lockoutEnd = request.LockoutDurationDays.HasValue
            ? DateTimeOffset.UtcNow.AddDays(request.LockoutDurationDays.Value)
            : DateTimeOffset.MaxValue; // Permanent lock

        var result = await _userManager.SetLockoutEndDateAsync(user, lockoutEnd);
        
        if (result.Succeeded)
        {
            await _userManager.SetLockoutEnabledAsync(user, true);
            
            // 🎯 LOG: User locked
            await _activityLogService.LogActivityAsync(
                action: "USER_LOCKED",
                entityType: "USER",
                entityId: user.Id,
                entityName: user.UserName,
                description: $"User '{user.Email}' locked until {lockoutEnd:yyyy-MM-dd HH:mm}",
                severity: "WARNING",
                metadata: new Dictionary<string, object>
                {
                    ["UserEmail"] = user.Email ?? "unknown",
                    ["LockoutEnd"] = lockoutEnd.ToString("o"),
                    ["LockoutDurationDays"] = request.LockoutDurationDays?.ToString() ?? "Permanent",
                    ["LockedBy"] = currentUser?.Email ?? "system"
                }
            );
            
            return Ok(new { 
                message = "User locked successfully",
                lockoutEnd = lockoutEnd
            });
        }

        return BadRequest(new { 
            message = "Failed to lock user", 
            errors = result.Errors.Select(e => e.Description) 
        });
    }

    /// <summary>
    /// Unlock/Unsuspend user account (Admin only)
    /// </summary>
    [HttpPost("{id}/unlock")]
    [Authorize(Roles = Infrastructure.Authorization.Roles.SuperAdmin + "," + Infrastructure.Authorization.Roles.Admin)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UnlockUser(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
            return NotFound(new { message = "User not found" });

        var result = await _userManager.SetLockoutEndDateAsync(user, null);
        
        if (result.Succeeded)
        {
            // 🎯 LOG: User unlocked
            await _activityLogService.LogActivityAsync(
                action: "USER_UNLOCKED",
                entityType: "USER",
                entityId: user.Id,
                entityName: user.UserName,
                description: $"User '{user.Email}' unlocked successfully",
                severity: "INFO",
                metadata: new Dictionary<string, object>
                {
                    ["UserEmail"] = user.Email ?? "unknown",
                    ["UnlockedBy"] = User.Identity?.Name ?? "system"
                }
            );
            
            return Ok(new { message = "User unlocked successfully" });
        }

        return BadRequest(new { 
            message = "Failed to unlock user", 
            errors = result.Errors.Select(e => e.Description) 
        });
    }

    #endregion

    #region Authentication & Email Workflows

    /// <summary>
    /// Register a new user
    /// </summary>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        // Check if user already exists
        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
            return BadRequest(new { message = "User with this email already exists" });

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email
        };

        var result = await _userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
        {
            return BadRequest(new
            {
                message = "Registration failed",
                errors = result.Errors.Select(e => e.Description)
            });
        }

        _logger.LogInformation("User {Email} created successfully", user.Email);

        // 🎯 LOG: User registration
        await _activityLogService.LogActivityAsync(
            action: "USER_CREATED",
            entityType: "USER",
            entityId: user.Id,
            entityName: user.UserName,
            description: $"New user registered: {user.Email}",
            severity: "INFO",
            userId: user.Id,
            userName: user.UserName,
            userEmail: user.Email,
            metadata: new Dictionary<string, object>
            {
                ["RegistrationMethod"] = "Self-Registration",
                ["EmailConfirmed"] = false
            }
        );

        // Generate email confirmation token
        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

        // Create confirmation link
        var confirmationLink = Url.Action(
            nameof(ConfirmEmail),
            "Users",
            new { userId = user.Id, token = encodedToken },
            Request.Scheme);

        // Send confirmation email
        try
        {
            await _emailService.SendEmailConfirmationAsync(
                user.Email,
                user.UserName ?? user.Email,
                confirmationLink!);

            _logger.LogInformation("Confirmation email sent to {Email}", user.Email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send confirmation email to {Email}", user.Email);
        }

        return Ok(new
        {
            message = "Registration successful. Please check your email to confirm your account.",
            userId = user.Id,
            email = user.Email
        });
    }

    /// <summary>
    /// Confirm email address
    /// </summary>
    [HttpGet("confirm-email")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ConfirmEmail([FromQuery] string userId, [FromQuery] string token)
    {
        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(token))
            return BadRequest(new { message = "Invalid email confirmation link" });

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return NotFound(new { message = "User not found" });

        if (user.EmailConfirmed)
            return Ok(new { message = "Email already confirmed" });

        var decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(token));
        var result = await _userManager.ConfirmEmailAsync(user, decodedToken);

        if (!result.Succeeded)
        {
            _logger.LogWarning("Email confirmation failed for {Email}", user.Email);
            return BadRequest(new
            {
                message = "Email confirmation failed. The link may have expired.",
                errors = result.Errors.Select(e => e.Description)
            });
        }

        _logger.LogInformation("Email confirmed for {Email}", user.Email);

        // 🎯 LOG: Email verified
        await _activityLogService.LogActivityAsync(
            action: "USER_EMAIL_VERIFIED",
            entityType: "USER",
            entityId: user.Id,
            entityName: user.UserName,
            description: $"Email verified for user: {user.Email}",
            severity: "INFO",
            userId: user.Id,
            userName: user.UserName,
            userEmail: user.Email
        );

        // Send welcome email
        try
        {
            var roles = await _userManager.GetRolesAsync(user);
            var primaryRole = roles.FirstOrDefault() ?? "User";

            await _emailService.SendWelcomeEmailAsync(
                user.Email!,
                user.UserName ?? user.Email!,
                primaryRole);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send welcome email to {Email}", user.Email);
        }

        return Ok(new
        {
            message = "Email confirmed successfully! You can now sign in.",
            email = user.Email
        });
    }
    
    
#if DEBUG
    /// <summary>
    /// Get email confirmation token for testing (DEVELOPMENT ONLY)
    /// </summary>
    [HttpGet("debug/get-confirmation-token/{email}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetConfirmationTokenDebug(string email)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
            return NotFound(new { message = "User not found" });

        if (user.EmailConfirmed)
            return BadRequest(new { message = "Email already confirmed" });

        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

        var confirmationLink = Url.Action(
            nameof(ConfirmEmail),
            "Users",
            new { userId = user.Id, token = encodedToken },
            Request.Scheme);

        return Ok(new
        {
            userId = user.Id,
            token = encodedToken,
            confirmationLink = confirmationLink
        });
    }
#endif

    /// <summary>
    /// Resend email confirmation
    /// </summary>
    [HttpPost("resend-confirmation")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResendConfirmation([FromBody] ResendConfirmationRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            return Ok(new { message = "If the email exists, a confirmation link has been sent." });
        }

        if (user.EmailConfirmed)
        {
            return BadRequest(new { message = "Email is already confirmed" });
        }

        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

        var confirmationLink = Url.Action(
            nameof(ConfirmEmail),
            "Users",
            new { userId = user.Id, token = encodedToken },
            Request.Scheme);

        try
        {
            await _emailService.SendEmailConfirmationAsync(
                user.Email!,
                user.UserName ?? user.Email!,
                confirmationLink!);

            _logger.LogInformation("Confirmation email resent to {Email}", user.Email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to resend confirmation email to {Email}", user.Email);
            return StatusCode(500, new { message = "Failed to send email. Please try again later." });
        }

        return Ok(new { message = "Confirmation email sent. Please check your inbox." });
    }

    /// <summary>
    /// Request password reset
    /// </summary>
    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        
        if (user == null || !(await _userManager.IsEmailConfirmedAsync(user)))
        {
            return Ok(new { message = "If the email exists and is confirmed, a password reset link has been sent." });
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

        var resetLink = Url.Action(
            nameof(ResetPassword),
            "Users",
            new { email = user.Email, token = encodedToken },
            Request.Scheme);

        try
        {
            await _emailService.SendPasswordResetEmailAsync(
                user.Email!,
                user.UserName ?? user.Email!,
                resetLink!);

            _logger.LogInformation("Password reset email sent to {Email}", user.Email);
            
            // 🎯 LOG: Password reset requested
            await _activityLogService.LogActivityAsync(
                action: "PASSWORD_RESET_REQUESTED",
                entityType: "USER",
                entityId: user.Id,
                entityName: user.UserName,
                description: $"Password reset requested for user: {user.Email}",
                severity: "INFO",
                userId: user.Id,
                userName: user.UserName,
                userEmail: user.Email
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send password reset email to {Email}", user.Email);
        }

        return Ok(new { message = "If the email exists and is confirmed, a password reset link has been sent." });
    }

    /// <summary>
    /// Reset password with token
    /// </summary>
    [HttpPost("reset-password")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            return BadRequest(new { message = "Invalid request" });
        }

        var decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(request.Token));
        var result = await _userManager.ResetPasswordAsync(user, decodedToken, request.NewPassword);

        if (!result.Succeeded)
        {
            _logger.LogWarning("Password reset failed for {Email}", user.Email);
            return BadRequest(new
            {
                message = "Password reset failed. The link may have expired.",
                errors = result.Errors.Select(e => e.Description)
            });
        }

        _logger.LogInformation("Password reset successful for {Email}", user.Email);

        // 🎯 LOG: Password reset completed
        await _activityLogService.LogActivityAsync(
            action: "PASSWORD_RESET_COMPLETED",
            entityType: "USER",
            entityId: user.Id,
            entityName: user.UserName,
            description: $"Password reset completed for user: {user.Email}",
            severity: "INFO",
            userId: user.Id,
            userName: user.UserName,
            userEmail: user.Email
        );

        // Send password changed notification
        try
        {
            await _emailService.SendPasswordChangedNotificationAsync(
                user.Email!,
                user.UserName ?? user.Email!);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send password changed notification to {Email}", user.Email);
        }

        return Ok(new
        {
            message = "Password reset successful. You can now sign in with your new password."
        });
    }

    /// <summary>
    /// Change password (for authenticated users)
    /// </summary>
    [HttpPost("change-password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Unauthorized(new { message = "User not authenticated" });

        var result = await _userManager.ChangePasswordAsync(
            user,
            request.CurrentPassword,
            request.NewPassword);

        if (!result.Succeeded)
        {
            return BadRequest(new
            {
                message = "Password change failed",
                errors = result.Errors.Select(e => e.Description)
            });
        }

        _logger.LogInformation("Password changed for {Email}", user.Email);

        // 🎯 LOG: Password changed
        await _activityLogService.LogActivityAsync(
            action: "USER_PASSWORD_CHANGED",
            entityType: "USER",
            entityId: user.Id,
            entityName: user.UserName,
            description: $"Password changed for user: {user.Email}",
            severity: "INFO",
            metadata: new Dictionary<string, object>
            {
                ["ChangedBy"] = "Self"
            }
        );

        // Send password changed notification
        try
        {
            await _emailService.SendPasswordChangedNotificationAsync(
                user.Email!,
                user.UserName ?? user.Email!);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send password changed notification to {Email}", user.Email);
        }

        await _signInManager.RefreshSignInAsync(user);

        return Ok(new { message = "Password changed successfully" });
    }
    
    /// <summary>
    /// Change user password
    /// </summary>
    [HttpPost("{id}/change-password")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ChangePassword(
        string id,
        [FromBody] ChangePasswordDto dto)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound(new { message = "User not found" });

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin") || User.IsInRole("SuperAdmin");
            
            if (currentUserId != id && !isAdmin)
                return Forbid();

            var isCurrentPasswordValid = await _userManager.CheckPasswordAsync(user, dto.CurrentPassword);
            if (!isCurrentPasswordValid)
                return BadRequest(new { message = "Current password is incorrect" });

            var result = await _userManager.ChangePasswordAsync(
                user,
                dto.CurrentPassword,
                dto.NewPassword
            );

            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description);
                return BadRequest(new { 
                    message = "Failed to change password", 
                    errors 
                });
            }

            _logger.LogInformation("User {UserId} changed their password", id);

            // 🎯 LOG: Password changed by admin
            await _activityLogService.LogActivityAsync(
                action: "USER_PASSWORD_CHANGED",
                entityType: "USER",
                entityId: user.Id,
                entityName: user.UserName,
                description: $"Password changed for user: {user.Email}",
                severity: "INFO",
                metadata: new Dictionary<string, object>
                {
                    ["ChangedBy"] = currentUserId == id ? "Self" : "Admin",
                    ["AdminId"] = currentUserId ?? "unknown"
                }
            );

            return Ok(new { message = "Password changed successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing password for user {UserId}", id);
            return StatusCode(500, new { message = "An error occurred while changing the password" });
        }
    }

    /// <summary>
    /// Upload user profile picture
    /// </summary>
    [HttpPost("{id}/profile-picture")]
    [Authorize]
    [ProducesResponseType(typeof(ProfilePictureResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UploadProfilePicture(
        string id,
        IFormFile file)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound(new { message = "User not found" });

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin") || User.IsInRole("SuperAdmin");
            
            if (currentUserId != id && !isAdmin)
                return Forbid();

            if (file == null || file.Length == 0)
                return BadRequest(new { message = "No file uploaded" });

            if (file.Length > 5 * 1024 * 1024)
                return BadRequest(new { message = "File size exceeds 5MB limit" });

            var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/gif", "image/webp" };
            if (!allowedTypes.Contains(file.ContentType.ToLower()))
                return BadRequest(new { message = "Invalid file type. Allowed types: JPEG, PNG, GIF, WebP" });

            var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "profiles");
            Directory.CreateDirectory(uploadsPath);

            var fileExtension = Path.GetExtension(file.FileName);
            var fileName = $"{id}_{Guid.NewGuid()}{fileExtension}";
            var filePath = Path.Combine(uploadsPath, fileName);

            if (!string.IsNullOrEmpty(user.ProfilePictureUrl))
            {
                var oldFileName = Path.GetFileName(user.ProfilePictureUrl);
                var oldFilePath = Path.Combine(uploadsPath, oldFileName);
                if (System.IO.File.Exists(oldFilePath))
                {
                    System.IO.File.Delete(oldFilePath);
                }
            }

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var profilePictureUrl = $"/uploads/profiles/{fileName}";

            _logger.LogInformation("User {UserId} uploaded profile picture: {FileName}", id, fileName);

            // 🎯 LOG: Profile picture uploaded
            await _activityLogService.LogActivityAsync(
                action: "USER_UPDATED",
                entityType: "USER",
                entityId: user.Id,
                entityName: user.UserName,
                description: $"Profile picture uploaded for user: {user.Email}",
                severity: "INFO",
                metadata: new Dictionary<string, object>
                {
                    ["FileName"] = fileName,
                    ["FileSize"] = file.Length,
                    ["ContentType"] = file.ContentType
                }
            );

            return Ok(new ProfilePictureResponseDto
            {
                Message = "Profile picture uploaded successfully",
                Url = profilePictureUrl,
                FileName = fileName
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading profile picture for user {UserId}", id);
            return StatusCode(500, new { message = "An error occurred while uploading the profile picture" });
        }
    }

    /// <summary>
    /// Delete user profile picture
    /// </summary>
    [HttpDelete("{id}/profile-picture")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteProfilePicture(string id)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound(new { message = "User not found" });

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin") || User.IsInRole("SuperAdmin");
            
            if (currentUserId != id && !isAdmin)
                return Forbid();

            if (!string.IsNullOrEmpty(user.ProfilePictureUrl))
            {
                var fileName = Path.GetFileName(user.ProfilePictureUrl);
                var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "profiles");
                var filePath = Path.Combine(uploadsPath, fileName);
                
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
            }

            _logger.LogInformation("User {UserId} deleted their profile picture", id);

            // 🎯 LOG: Profile picture deleted
            await _activityLogService.LogActivityAsync(
                action: "USER_UPDATED",
                entityType: "USER",
                entityId: user.Id,
                entityName: user.UserName,
                description: $"Profile picture deleted for user: {user.Email}",
                severity: "INFO"
            );

            return Ok(new { message = "Profile picture deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting profile picture for user {UserId}", id);
            return StatusCode(500, new { message = "An error occurred while deleting the profile picture" });
        }
    }

    
    /// <summary>
    /// Create new user (SuperAdmin only)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = Infrastructure.Authorization.Roles.SuperAdmin)]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<UserDto>> CreateUser([FromBody] CreateUserDto request)
    {
        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
            return BadRequest(new { message = "User with this email already exists" });

        var user = new ApplicationUser
        {
            UserName = request.UserName,
            Email = request.Email,
            EmailConfirmed = request.EmailConfirmed,
            PhoneNumber = request.PhoneNumber
        };

        var result = await _userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
        {
            return BadRequest(new
            {
                message = "Failed to create user",
                errors = result.Errors.Select(e => e.Description)
            });
        }

        if (request.Roles != null && request.Roles.Any())
        {
            foreach (var roleId in request.Roles)
            {
                var role = await _roleManager.FindByIdAsync(roleId);
                if (role != null)
                {
                    await _userManager.AddToRoleAsync(user, role.Name!);
                }
            }
        }

        var userRoles = await _userManager.GetRolesAsync(user);

        // 🎯 LOG: User created by admin
        await _activityLogService.LogActivityAsync(
            action: "USER_CREATED",
            entityType: "USER",
            entityId: user.Id,
            entityName: user.UserName,
            description: $"User created by admin: {user.Email}",
            severity: "INFO",
            metadata: new Dictionary<string, object>
            {
                ["CreatedBy"] = User.Identity?.Name ?? "admin",
                ["Roles"] = string.Join(", ", userRoles),
                ["EmailConfirmed"] = request.EmailConfirmed
            }
        );

        var userDto = new UserDto
        {
            Id = user.Id,
            Email = user.Email!,
            UserName = user.UserName!,
            EmailConfirmed = user.EmailConfirmed,
            PhoneNumber = user.PhoneNumber,
            Roles = userRoles.ToList()
        };

        return CreatedAtAction(nameof(GetUser), new { id = user.Id }, userDto);
    }

    /// <summary>
    /// Update user (Admin only)
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = Infrastructure.Authorization.Roles.SuperAdmin + "," + Infrastructure.Authorization.Roles.Admin)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateUser(string id, [FromBody] UpdateUserDto request)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
            return NotFound(new { message = "User not found" });

        var changes = new List<string>();

        if (!string.IsNullOrWhiteSpace(request.Email) && request.Email != user.Email)
        {
            changes.Add($"Email: {user.Email} → {request.Email}");
            user.Email = request.Email;
        }

        if (!string.IsNullOrWhiteSpace(request.UserName) && request.UserName != user.UserName)
        {
            changes.Add($"Username: {user.UserName} → {request.UserName}");
            user.UserName = request.UserName;
        }
        else if (!string.IsNullOrWhiteSpace(request.Email))
        {
            user.UserName = request.Email;
        }

        if (request.PhoneNumber != null && request.PhoneNumber != user.PhoneNumber)
        {
            changes.Add($"Phone: {user.PhoneNumber} → {request.PhoneNumber}");
            user.PhoneNumber = request.PhoneNumber;
        }

        if (request.EmailConfirmed.HasValue && request.EmailConfirmed.Value != user.EmailConfirmed)
        {
            changes.Add($"EmailConfirmed: {user.EmailConfirmed} → {request.EmailConfirmed.Value}");
            user.EmailConfirmed = request.EmailConfirmed.Value;
        }

        var result = await _userManager.UpdateAsync(user);

        if (!result.Succeeded)
        {
            return BadRequest(new
            {
                message = "Failed to update user",
                errors = result.Errors.Select(e => e.Description)
            });
        }

        if (request.Roles != null)
        {
            var currentRoles = await _userManager.GetRolesAsync(user);
            await _userManager.RemoveFromRolesAsync(user, currentRoles);
            
            if (request.Roles.Any())
            {
                await _userManager.AddToRolesAsync(user, request.Roles);
                changes.Add($"Roles updated: {string.Join(", ", request.Roles)}");
            }
        }

        // 🎯 LOG: User updated
        if (changes.Any())
        {
            await _activityLogService.LogActivityAsync(
                action: "USER_UPDATED",
                entityType: "USER",
                entityId: user.Id,
                entityName: user.UserName,
                description: $"User updated: {user.Email}",
                severity: "INFO",
                metadata: new Dictionary<string, object>
                {
                    ["Changes"] = string.Join("; ", changes),
                    ["UpdatedBy"] = User.Identity?.Name ?? "admin"
                }
            );
        }

        return Ok(new { message = "User updated successfully" });
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = Policies.RequireSuperAdminRole)]
    public async Task<IActionResult> DeleteUser(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
            return NotFound();

        var userEmail = user.Email;
        var userName = user.UserName;
        
        var result = await _userManager.DeleteAsync(user);
        
        if (!result.Succeeded)
            return BadRequest(result.Errors);

        // 🎯 LOG: User deleted
        await _activityLogService.LogActivityAsync(
            action: "USER_DELETED",
            entityType: "USER",
            entityId: id,
            entityName: userName,
            description: $"User deleted: {userEmail}",
            severity: "WARNING",
            metadata: new Dictionary<string, object>
            {
                ["DeletedBy"] = User.Identity?.Name ?? "admin",
                ["DeletedEmail"] = userEmail ?? "unknown"
            }
        );

        return NoContent();
    }

    #endregion
}

#region DTOs

public record UpdateUserDto
{
    public string? Email { get; init; }
    public string? UserName { get; init; }
    public string? PhoneNumber { get; init; }
    public bool? EmailConfirmed { get; init; }
    public List<string>? Roles { get; init; }
}
public record CreateUserDto
{
    public string Email { get; init; } = string.Empty;
    public string UserName { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string? PhoneNumber { get; init; }
    public bool EmailConfirmed { get; init; } = false;
    public List<string>? Roles { get; init; }
}

public record UserDto
{
    public string Id { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string UserName { get; init; } = string.Empty;
    public bool EmailConfirmed { get; init; }
    public string? PhoneNumber { get; init; }
    public bool PhoneNumberConfirmed { get; init; }
    public List<string> Roles { get; init; } = new();
    public bool LockoutEnabled { get; init; }
    public DateTimeOffset? LockoutEnd { get; init; }
    public bool TwoFactorEnabled { get; init; }
    public List<UserClaimDto>? Claims { get; init; }
}

public record UserClaimDto
{
    public string Type { get; init; } = string.Empty;
    public string Value { get; init; } = string.Empty;
}

public record AssignRoleRequest
{
    public string RoleName { get; init; } = string.Empty;
}

public record LockUserRequest
{
    public int? LockoutDurationDays { get; init; }
}

public record RegisterRequest
{
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}

public record ResendConfirmationRequest
{
    public string Email { get; init; } = string.Empty;
}

public record ForgotPasswordRequest
{
    public string Email { get; init; } = string.Empty;
}

public record ResetPasswordRequest
{
    public string Email { get; init; } = string.Empty;
    public string Token { get; init; } = string.Empty;
    public string NewPassword { get; init; } = string.Empty;
}

public record ChangePasswordRequest
{
    public string CurrentPassword { get; init; } = string.Empty;
    public string NewPassword { get; init; } = string.Empty;
}

#endregion