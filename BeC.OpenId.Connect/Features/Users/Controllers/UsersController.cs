using System.Security.Claims;
using BeC.OpenId.Connect.Features.ActivityLogs.Services.Interfaces;
using BeC.OpenId.Connect.Features.Users.Dtos;
using BeC.OpenId.Connect.Features.Users.Models;
using BeC.OpenId.Connect.Features.Users.Services.Interfaces;
using BeC.OpenId.Connect.Features.Users.ViewModels;
using BeC.OpenId.Connect.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Validation.AspNetCore;
using AuthRoles = BeC.OpenId.Connect.Infrastructure.Authorization.Roles;

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
    private readonly IUserService _userService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IActivityLogService _activityLogService;
    private readonly ILogger<UsersController> _logger;

    public UsersController(
        IUserService userService,
        UserManager<ApplicationUser> userManager,
        IActivityLogService activityLogService,
        ILogger<UsersController> logger)
    {
        _userService = userService;
        _userManager = userManager;
        _activityLogService = activityLogService;
        _logger = logger;
    }

    #region User Management (Admin)

    /// <summary>
    /// Get all users (Admin only)
    /// </summary>
    [HttpGet]
    [Authorize(Roles = AuthAuthRoles.SuperAdmin + "," + AuthAuthRoles.Admin)]
    [ProducesResponseType(typeof(UserListViewModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<UserListViewModel>> GetUsers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? searchTerm = null)
    {
        try
        {
            var result = await _userService.GetUsersAsync(page, pageSize, searchTerm);

            Response.Headers.Append("X-Total-Count", result.TotalCount.ToString());
            Response.Headers.Append("X-Page", result.Page.ToString());
            Response.Headers.Append("X-Page-Size", result.PageSize.ToString());

            return this.Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting users");
            return this.StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
        }
    }

    /// <summary>
    /// Get user by ID (Admin only)
    /// </summary>
    [HttpGet("{id}")]
    [Authorize(Roles = AuthRoles.SuperAdmin + "," + AuthRoles.Admin)]
    [ProducesResponseType(typeof(UserViewModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<UserViewModel>> GetUser(string id)
    {
        try
        {
            var user = await _userService.GetUserByIdAsync(id);

            if (user == null)
                return this.NotFound(new { message = "User not found" });

            return this.Ok(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user {UserId}", id);
            return this.StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
        }
    }

    /// <summary>
    /// Get current user's profile
    /// </summary>
    [HttpGet("me")]
    [ProducesResponseType(typeof(UserViewModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserViewModel>> GetCurrentUser()
    {
        try
        {
            var user = await _userService.GetCurrentUserAsync(User);

            if (user == null)
                return this.Unauthorized(new { message = "User not authenticated" });

            return this.Ok(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current user");
            return this.StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
        }
    }

    /// <summary>
    /// Create new user (SuperAdmin only)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = AuthRoles.SuperAdmin)]
    [ProducesResponseType(typeof(UserViewModel), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<UserViewModel>> CreateUser([FromBody] CreateUserDto request)
    {
        try
        {
            var adminUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(adminUserId))
                return this.Unauthorized();

            var (success, user, errorMessage) = await _userService.CreateUserAsync(
                request.Email,
                request.UserName,
                request.Password,
                request.PhoneNumber,
                request.EmailConfirmed,
                request.Roles,
                adminUserId);

            if (!success)
                return this.BadRequest(new { message = errorMessage });

            return this.CreatedAtAction(nameof(GetUser), new { id = user!.Id }, user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user");
            return this.StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
        }
    }

    /// <summary>
    /// Update user (Admin only)
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = AuthRoles.SuperAdmin + "," + AuthRoles.Admin)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateUser(string id, [FromBody] UpdateUserModel request)
    {
        try
        {
            var (success, user, errorMessage) = await _userService.UpdateUserAsync(id, request);

            if (!success)
            {
                if (errorMessage == "User not found")
                    return this.NotFound(new { message = errorMessage });

                return this.BadRequest(new { message = errorMessage });
            }

            return this.Ok(new { message = "User updated successfully", user });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user {UserId}", id);
            return this.StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
        }
    }

    /// <summary>
    /// Delete user (SuperAdmin only)
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Policy = Policies.RequireSuperAdminRole)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteUser(string id)
    {
        try
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(currentUserId))
                return this.Unauthorized();

            var (success, errorMessage) = await _userService.DeleteUserAsync(id, currentUserId);

            if (!success)
            {
                if (errorMessage == "User not found")
                    return this.NotFound(new { message = errorMessage });

                return this.BadRequest(new { message = errorMessage });
            }

            return this.NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user {UserId}", id);
            return this.StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
        }
    }

    #endregion

    #region Role Management (SuperAdmin)

    /// <summary>
    /// Assign role to user (SuperAdmin only)
    /// </summary>
    [HttpPost("{id}/roles")]
    [Authorize(Roles = AuthRoles.SuperAdmin)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> AssignRole(string id, [FromBody] AssignRoleModel request)
    {
        try
        {
            var adminUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(adminUserId))
                return this.Unauthorized();

            var (success, errorMessage) = await _userService.AssignRoleAsync(id, request, adminUserId);

            if (!success)
            {
                if (errorMessage == "User not found")
                    return this.NotFound(new { message = errorMessage });

                return this.BadRequest(new { message = errorMessage });
            }

            return this.Ok(new { message = $"Role '{request.RoleName}' assigned successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning role to user {UserId}", id);
            return this.StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
        }
    }

    /// <summary>
    /// Remove role from user (SuperAdmin only)
    /// </summary>
    [HttpDelete("{id}/roles/{roleName}")]
    [Authorize(Roles = AuthRoles.SuperAdmin)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> RemoveRole(string id, string roleName)
    {
        try
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(currentUserId))
                return this.Unauthorized();

            var (success, errorMessage) = await _userService.RemoveRoleAsync(id, roleName, currentUserId);

            if (!success)
            {
                if (errorMessage == "User not found")
                    return this.NotFound(new { message = errorMessage });

                return this.BadRequest(new { message = errorMessage });
            }

            return this.Ok(new { message = $"Role '{roleName}' removed successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing role from user {UserId}", id);
            return this.StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
        }
    }

    #endregion

    #region Account Management (Admin)

    /// <summary>
    /// Lock/Suspend user account (Admin only)
    /// </summary>
    [HttpPost("{id}/lock")]
    [Authorize(Roles = AuthRoles.SuperAdmin + "," + AuthRoles.Admin)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> LockUser(string id, [FromBody] LockUserModel request)
    {
        try
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(currentUserId))
                return this.Unauthorized();

            var (success, errorMessage) = await _userService.LockUserAsync(id, request, currentUserId);

            if (!success)
            {
                if (errorMessage == "User not found")
                    return this.NotFound(new { message = errorMessage });

                return this.BadRequest(new { message = errorMessage });
            }

            return this.Ok(new { message = "User locked successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error locking user {UserId}", id);
            return this.StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
        }
    }

    /// <summary>
    /// Unlock/Unsuspend user account (Admin only)
    /// </summary>
    [HttpPost("{id}/unlock")]
    [Authorize(Roles = AuthRoles.SuperAdmin + "," + AuthRoles.Admin)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UnlockUser(string id)
    {
        try
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(currentUserId))
                return this.Unauthorized();

            var (success, errorMessage) = await _userService.UnlockUserAsync(id, currentUserId);

            if (!success)
            {
                if (errorMessage == "User not found")
                    return this.NotFound(new { message = errorMessage });

                return this.BadRequest(new { message = errorMessage });
            }

            return this.Ok(new { message = "User unlocked successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unlocking user {UserId}", id);
            return this.StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
        }
    }

    #endregion

    #region Password Management

    /// <summary>
    /// Change password (for authenticated users)
    /// </summary>
    [HttpPost("change-password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordModel request)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return this.Unauthorized(new { message = "User not authenticated" });

            var (success, errors) = await _userService.ChangePasswordAsync(userId, request);

            if (!success)
            {
                return this.BadRequest(new
                {
                    message = "Password change failed",
                    errors = errors?.Select(e => e.Description)
                });
            }

            return this.Ok(new { message = "Password changed successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing password");
            return this.StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
        }
    }

    /// <summary>
    /// Change user password (Admin or Self)
    /// </summary>
    [HttpPost("{id}/change-password")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ChangePassword(string id, [FromBody] ChangePasswordDto dto)
    {
        try
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole(AuthRoles.Admin) || User.IsInRole(AuthRoles.SuperAdmin);

            if (currentUserId != id && !isAdmin)
                return this.Forbid();

            var model = new ChangePasswordModel
            {
                CurrentPassword = dto.CurrentPassword,
                NewPassword = dto.NewPassword
            };

            var (success, errors) = await _userService.ChangePasswordAsync(id, model);

            if (!success)
            {
                return this.BadRequest(new
                {
                    message = "Failed to change password",
                    errors = errors?.Select(e => e.Description)
                });
            }

            return this.Ok(new { message = "Password changed successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing password for user {UserId}", id);
            return this.StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
        }
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
    public async Task<IActionResult> Register([FromBody] RegisterUserModel request)
    {
        try
        {
            if (!ModelState.IsValid)
                return this.BadRequest(ModelState);

            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var (success, user, errors) = await _userService.RegisterUserAsync(request, baseUrl);

            if (!success)
            {
                return this.BadRequest(new
                {
                    message = "Registration failed",
                    errors = errors?.Select(e => e.Description)
                });
            }

            return this.Ok(new
            {
                message = "Registration successful. Please check your email to confirm your account.",
                userId = user!.Id,
                email = user.Email
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering user");
            return this.StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
        }
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
        try
        {
            var (success, errorMessage) = await _userService.ConfirmEmailAsync(userId, token);

            if (!success && errorMessage != null)
                return this.BadRequest(new { message = errorMessage });

            return this.Ok(new { message = "Email confirmed successfully! You can now sign in." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error confirming email");
            return this.StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
        }
    }

#if DEBUG
    /// <summary>
    /// Get email confirmation token for testing (DEVELOPMENT ONLY)
    /// </summary>
    [HttpGet("debug/get-confirmation-token/{email}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetConfirmationTokenDebug(string email)
    {
        try
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
                return this.NotFound(new { message = "User not found" });

            if (user.EmailConfirmed)
                return this.BadRequest(new { message = "Email already confirmed" });

            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var encodedToken = System.Text.Encoding.UTF8.GetBytes(token);
            var base64Token = Microsoft.AspNetCore.WebUtilities.WebEncoders.Base64UrlEncode(encodedToken);

            var confirmationLink = Url.Action(
                nameof(ConfirmEmail),
                "Users",
                new { userId = user.Id, token = base64Token },
                Request.Scheme);

            return this.Ok(new
            {
                userId = user.Id,
                token = base64Token,
                confirmationLink = confirmationLink
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting confirmation token");
            return this.StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
        }
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
        try
        {
            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var (success, errorMessage) = await _userService.ResendConfirmationAsync(request.Email, baseUrl);

            if (!success && errorMessage != null)
                return this.BadRequest(new { message = errorMessage });

            return this.Ok(new { message = "Confirmation email sent. Please check your inbox." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resending confirmation");
            return this.StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
        }
    }

    /// <summary>
    /// Request password reset
    /// </summary>
    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        try
        {
            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            await _userService.ForgotPasswordAsync(request.Email, baseUrl);

            return this.Ok(new { message = "If the email exists and is confirmed, a password reset link has been sent." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing forgot password request");
            return this.StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
        }
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
        try
        {
            var (success, errors) = await _userService.ResetPasswordAsync(request.Email, request.Token, request.NewPassword);

            if (!success)
            {
                return this.BadRequest(new
                {
                    message = "Password reset failed. The link may have expired.",
                    errors = errors?.Select(e => e.Description)
                });
            }

            return this.Ok(new
            {
                message = "Password reset successful. You can now sign in with your new password."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting password");
            return this.StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
        }
    }

    #endregion

    #region Profile Pictures

    /// <summary>
    /// Upload user profile picture
    /// </summary>
    [HttpPost("{id}/profile-picture")]
    [Authorize]
    [ProducesResponseType(typeof(ProfilePictureResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UploadProfilePicture(string id, IFormFile file)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return this.NotFound(new { message = "User not found" });

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole(AuthRoles.Admin) || User.IsInRole(AuthRoles.SuperAdmin);

            if (currentUserId != id && !isAdmin)
                return this.Forbid();

            if (file == null || file.Length == 0)
                return this.BadRequest(new { message = "No file uploaded" });

            if (file.Length > 5 * 1024 * 1024)
                return this.BadRequest(new { message = "File size exceeds 5MB limit" });

            var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/gif", "image/webp" };
            if (!allowedTypes.Contains(file.ContentType.ToLower()))
                return this.BadRequest(new { message = "Invalid file type. Allowed types: JPEG, PNG, GIF, WebP" });

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
            user.ProfilePictureUrl = profilePictureUrl;
            await _userManager.UpdateAsync(user);

            _logger.LogInformation("User {UserId} uploaded profile picture: {FileName}", id, fileName);

            // Log profile picture upload
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

            return this.Ok(new ProfilePictureResponseDto
            {
                Message = "Profile picture uploaded successfully",
                Url = profilePictureUrl,
                FileName = fileName
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading profile picture for user {UserId}", id);
            return this.StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
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
                return this.NotFound(new { message = "User not found" });

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole(AuthRoles.Admin) || User.IsInRole(AuthRoles.SuperAdmin);

            if (currentUserId != id && !isAdmin)
                return this.Forbid();

            if (!string.IsNullOrEmpty(user.ProfilePictureUrl))
            {
                var fileName = Path.GetFileName(user.ProfilePictureUrl);
                var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "profiles");
                var filePath = Path.Combine(uploadsPath, fileName);

                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }

                user.ProfilePictureUrl = null;
                await _userManager.UpdateAsync(user);
            }

            _logger.LogInformation("User {UserId} deleted their profile picture", id);

            // Log profile picture deletion
            await _activityLogService.LogActivityAsync(
                action: "USER_UPDATED",
                entityType: "USER",
                entityId: user.Id,
                entityName: user.UserName,
                description: $"Profile picture deleted for user: {user.Email}",
                severity: "INFO"
            );

            return this.Ok(new { message = "Profile picture deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting profile picture for user {UserId}", id);
            return this.StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
        }
    }

    #endregion
}

#region DTOs

public record CreateUserDto
{
    public string Email { get; init; } = string.Empty;
    public string UserName { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string? PhoneNumber { get; init; }
    public bool EmailConfirmed { get; init; } = false;
    public List<string>? Roles { get; init; }
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

#endregion
