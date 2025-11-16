using System.Text;
using AutoMapper;
using BeC.OpenId.Connect.Features.ActivityLogs.Services.Interfaces;
using BeC.OpenId.Connect.Features.Users.Dtos;
using BeC.OpenId.Connect.Features.Users.Models;
using BeC.OpenId.Connect.Features.Users.Services.Interfaces;
using BeC.OpenId.Connect.Features.Users.ViewModels;
using BeC.OpenId.Connect.Infrastructure.Email;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;

namespace BeC.OpenId.Connect.Features.Users.Services;

/// <summary>
/// Service for managing users, roles, and authentication
/// </summary>
public class UserService : IUserService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IEmailService _emailService;
    private readonly IActivityLogService _activityLogService;
    private readonly IMapper _mapper;
    private readonly ILogger<UserService> _logger;

    public UserService(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IEmailService emailService,
        IActivityLogService activityLogService,
        IMapper mapper,
        ILogger<UserService> logger)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _emailService = emailService;
        _activityLogService = activityLogService;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<UserListViewModel> GetUsersAsync(int page, int pageSize, string? searchTerm)
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

        var userViewModels = new List<UserViewModel>();

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            userViewModels.Add(new UserViewModel
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
                TwoFactorEnabled = user.TwoFactorEnabled
            });
        }

        return new UserListViewModel
        {
            Users = userViewModels,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<UserViewModel?> GetUserByIdAsync(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
            return null;

        var roles = await _userManager.GetRolesAsync(user);
        var claims = await _userManager.GetClaimsAsync(user);

        return new UserViewModel
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
            Claims = claims.Select(c => new UserClaimViewModel
            {
                Type = c.Type,
                Value = c.Value
            }).ToList()
        };
    }

    public async Task<UserViewModel?> GetCurrentUserAsync(System.Security.Claims.ClaimsPrincipal userPrincipal)
    {
        var user = await _userManager.GetUserAsync(userPrincipal);
        if (user == null)
            return null;

        var roles = await _userManager.GetRolesAsync(user);
        var claims = await _userManager.GetClaimsAsync(user);

        return new UserViewModel
        {
            Id = user.Id,
            Email = user.Email!,
            UserName = user.UserName!,
            EmailConfirmed = user.EmailConfirmed,
            PhoneNumber = user.PhoneNumber,
            PhoneNumberConfirmed = user.PhoneNumberConfirmed,
            Roles = roles.ToList(),
            TwoFactorEnabled = user.TwoFactorEnabled,
            Claims = claims.Select(c => new UserClaimViewModel
            {
                Type = c.Type,
                Value = c.Value
            }).ToList()
        };
    }

    public async Task<(bool success, UserViewModel? user, IEnumerable<IdentityError>? errors)> RegisterUserAsync(
        RegisterUserModel model, string? baseUrl = null)
    {
        // Check if user already exists
        var existingUser = await _userManager.FindByEmailAsync(model.Email);
        if (existingUser != null)
        {
            return (false, null, new[]
            {
                new IdentityError { Description = "User with this email already exists" }
            });
        }

        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            PhoneNumber = model.PhoneNumber
        };

        var result = await _userManager.CreateAsync(user, model.Password);

        if (!result.Succeeded)
        {
            return (false, null, result.Errors);
        }

        _logger.LogInformation("User {Email} created successfully", user.Email);

        // Log user registration
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

        // Generate and send email confirmation
        try
        {
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

            if (!string.IsNullOrEmpty(baseUrl))
            {
                var confirmationLink = $"{baseUrl}/api/Users/confirm-email?userId={user.Id}&token={encodedToken}";
                await _emailService.SendEmailConfirmationAsync(
                    user.Email,
                    user.UserName ?? user.Email,
                    confirmationLink);

                _logger.LogInformation("Confirmation email sent to {Email}", user.Email);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send confirmation email to {Email}", user.Email);
        }

        var roles = await _userManager.GetRolesAsync(user);
        var userViewModel = new UserViewModel
        {
            Id = user.Id,
            Email = user.Email!,
            UserName = user.UserName!,
            EmailConfirmed = user.EmailConfirmed,
            PhoneNumber = user.PhoneNumber,
            Roles = roles.ToList()
        };

        return (true, userViewModel, null);
    }

    public async Task<(bool success, UserViewModel? user, string? errorMessage)> UpdateUserAsync(string id, UpdateUserModel model)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
            return (false, null, "User not found");

        var changes = new List<string>();

        if (!string.IsNullOrWhiteSpace(model.Email) && model.Email != user.Email)
        {
            changes.Add($"Email: {user.Email} → {model.Email}");
            user.Email = model.Email;
        }

        if (!string.IsNullOrWhiteSpace(model.UserName) && model.UserName != user.UserName)
        {
            changes.Add($"Username: {user.UserName} → {model.UserName}");
            user.UserName = model.UserName;
        }
        else if (!string.IsNullOrWhiteSpace(model.Email))
        {
            user.UserName = model.Email;
        }

        if (model.PhoneNumber != null && model.PhoneNumber != user.PhoneNumber)
        {
            changes.Add($"Phone: {user.PhoneNumber} → {model.PhoneNumber}");
            user.PhoneNumber = model.PhoneNumber;
        }

        if (model.EmailConfirmed.HasValue && model.EmailConfirmed.Value != user.EmailConfirmed)
        {
            changes.Add($"EmailConfirmed: {user.EmailConfirmed} → {model.EmailConfirmed.Value}");
            user.EmailConfirmed = model.EmailConfirmed.Value;
        }

        if (model.PhoneNumberConfirmed.HasValue && model.PhoneNumberConfirmed.Value != user.PhoneNumberConfirmed)
        {
            changes.Add($"PhoneNumberConfirmed: {user.PhoneNumberConfirmed} → {model.PhoneNumberConfirmed.Value}");
            user.PhoneNumberConfirmed = model.PhoneNumberConfirmed.Value;
        }

        user.UpdatedAt = DateTime.UtcNow;

        var result = await _userManager.UpdateAsync(user);

        if (!result.Succeeded)
        {
            return (false, null, string.Join(", ", result.Errors.Select(e => e.Description)));
        }

        // Log user update
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
                    ["Changes"] = string.Join("; ", changes)
                }
            );
        }

        var roles = await _userManager.GetRolesAsync(user);
        var userViewModel = new UserViewModel
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
            TwoFactorEnabled = user.TwoFactorEnabled
        };

        return (true, userViewModel, null);
    }

    public async Task<(bool success, string? errorMessage)> DeleteUserAsync(string id, string currentUserId)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
            return (false, "User not found");

        // Prevent deleting yourself
        if (currentUserId == id)
            return (false, "You cannot delete your own account");

        // Prevent deleting SuperAdmin users
        if (await _userManager.IsInRoleAsync(user, Infrastructure.Authorization.Roles.SuperAdmin))
            return (false, "Cannot delete SuperAdmin users");

        var userEmail = user.Email;
        var userName = user.UserName;

        var result = await _userManager.DeleteAsync(user);

        if (!result.Succeeded)
        {
            return (false, string.Join(", ", result.Errors.Select(e => e.Description)));
        }

        // Log user deletion
        await _activityLogService.LogActivityAsync(
            action: "USER_DELETED",
            entityType: "USER",
            entityId: id,
            entityName: userName,
            description: $"User deleted: {userEmail}",
            severity: "WARNING",
            metadata: new Dictionary<string, object>
            {
                ["DeletedEmail"] = userEmail ?? "unknown"
            }
        );

        return (true, null);
    }

    public async Task<(bool success, string? errorMessage)> AssignRoleAsync(string userId, AssignRoleModel model, string adminUserId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return (false, "User not found");

        if (!await _roleManager.RoleExistsAsync(model.RoleName))
            return (false, $"Role '{model.RoleName}' does not exist");

        if (await _userManager.IsInRoleAsync(user, model.RoleName))
            return (false, $"User already has role '{model.RoleName}'");

        var result = await _userManager.AddToRoleAsync(user, model.RoleName);

        if (!result.Succeeded)
        {
            return (false, string.Join(", ", result.Errors.Select(e => e.Description)));
        }

        // Log role assignment
        await _activityLogService.LogActivityAsync(
            action: "ROLE_ASSIGNED",
            entityType: "USER",
            entityId: user.Id,
            entityName: user.UserName,
            description: $"Role '{model.RoleName}' assigned to user '{user.Email}'",
            severity: "INFO",
            metadata: new Dictionary<string, object>
            {
                ["RoleName"] = model.RoleName,
                ["UserEmail"] = user.Email ?? "unknown",
                ["UserId"] = user.Id
            }
        );

        return (true, null);
    }

    public async Task<(bool success, string? errorMessage)> RemoveRoleAsync(string userId, string roleName, string currentUserId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return (false, "User not found");

        // Prevent removing SuperAdmin role from yourself
        if (currentUserId == userId && roleName == Infrastructure.Authorization.Roles.SuperAdmin)
            return (false, "You cannot remove SuperAdmin role from yourself");

        if (!await _userManager.IsInRoleAsync(user, roleName))
            return (false, $"User does not have role '{roleName}'");

        var result = await _userManager.RemoveFromRoleAsync(user, roleName);

        if (!result.Succeeded)
        {
            return (false, string.Join(", ", result.Errors.Select(e => e.Description)));
        }

        // Log role removal
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

        return (true, null);
    }

    public async Task<(bool success, string? errorMessage)> LockUserAsync(string userId, LockUserModel model, string currentUserId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return (false, "User not found");

        // Prevent locking SuperAdmin users
        if (await _userManager.IsInRoleAsync(user, Infrastructure.Authorization.Roles.SuperAdmin))
            return (false, "Cannot lock SuperAdmin users");

        // Prevent locking yourself
        if (currentUserId == userId)
            return (false, "You cannot lock your own account");

        var lockoutEnd = model.LockoutDurationMinutes.HasValue
            ? DateTimeOffset.UtcNow.AddMinutes(model.LockoutDurationMinutes.Value)
            : DateTimeOffset.MaxValue; // Permanent lock

        var result = await _userManager.SetLockoutEndDateAsync(user, lockoutEnd);

        if (!result.Succeeded)
        {
            return (false, string.Join(", ", result.Errors.Select(e => e.Description)));
        }

        await _userManager.SetLockoutEnabledAsync(user, true);

        // Log user lockout
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
                ["LockoutDurationMinutes"] = model.LockoutDurationMinutes?.ToString() ?? "Permanent",
                ["Reason"] = model.Reason ?? "No reason provided"
            }
        );

        return (true, null);
    }

    public async Task<(bool success, string? errorMessage)> UnlockUserAsync(string userId, string currentUserId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return (false, "User not found");

        var result = await _userManager.SetLockoutEndDateAsync(user, null);

        if (!result.Succeeded)
        {
            return (false, string.Join(", ", result.Errors.Select(e => e.Description)));
        }

        // Log user unlock
        await _activityLogService.LogActivityAsync(
            action: "USER_UNLOCKED",
            entityType: "USER",
            entityId: user.Id,
            entityName: user.UserName,
            description: $"User '{user.Email}' unlocked successfully",
            severity: "INFO",
            metadata: new Dictionary<string, object>
            {
                ["UserEmail"] = user.Email ?? "unknown"
            }
        );

        return (true, null);
    }

    public async Task<(bool success, IEnumerable<IdentityError>? errors)> ChangePasswordAsync(string userId, ChangePasswordModel model)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return (false, new[]
            {
                new IdentityError { Description = "User not found" }
            });
        }

        var result = await _userManager.ChangePasswordAsync(
            user,
            model.CurrentPassword,
            model.NewPassword);

        if (!result.Succeeded)
        {
            return (false, result.Errors);
        }

        _logger.LogInformation("Password changed for {Email}", user.Email);

        // Log password change
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

        return (true, null);
    }

    public async Task<(bool success, string? errorMessage)> AdminChangePasswordAsync(string userId, string newPassword)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return (false, "User not found");

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, token, newPassword);

        if (!result.Succeeded)
        {
            return (false, string.Join(", ", result.Errors.Select(e => e.Description)));
        }

        _logger.LogInformation("Admin reset password for {Email}", user.Email);

        // Log password change by admin
        await _activityLogService.LogActivityAsync(
            action: "USER_PASSWORD_CHANGED",
            entityType: "USER",
            entityId: user.Id,
            entityName: user.UserName,
            description: $"Password changed for user: {user.Email}",
            severity: "INFO",
            metadata: new Dictionary<string, object>
            {
                ["ChangedBy"] = "Admin"
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

        return (true, null);
    }

    public async Task<(bool success, string? errorMessage)> ConfirmEmailAsync(string userId, string token)
    {
        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(token))
            return (false, "Invalid email confirmation link");

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return (false, "User not found");

        if (user.EmailConfirmed)
            return (true, null); // Already confirmed

        var decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(token));
        var result = await _userManager.ConfirmEmailAsync(user, decodedToken);

        if (!result.Succeeded)
        {
            _logger.LogWarning("Email confirmation failed for {Email}", user.Email);
            return (false, "Email confirmation failed. The link may have expired.");
        }

        _logger.LogInformation("Email confirmed for {Email}", user.Email);

        // Log email verification
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
            await _emailService.SendWelcomeEmailAsync(
                user.Email!,
                user.UserName ?? user.Email!);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send welcome email to {Email}", user.Email);
        }

        return (true, null);
    }

    public async Task<(bool success, string? errorMessage)> ResendConfirmationAsync(string email, string? baseUrl = null)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
        {
            // Don't reveal that user doesn't exist
            return (true, null);
        }

        if (user.EmailConfirmed)
        {
            return (false, "Email is already confirmed");
        }

        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

        if (!string.IsNullOrEmpty(baseUrl))
        {
            var confirmationLink = $"{baseUrl}/api/Users/confirm-email?userId={user.Id}&token={encodedToken}";

            try
            {
                await _emailService.SendEmailConfirmationAsync(
                    user.Email!,
                    user.UserName ?? user.Email!,
                    confirmationLink);

                _logger.LogInformation("Confirmation email resent to {Email}", user.Email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to resend confirmation email to {Email}", user.Email);
                return (false, "Failed to send email. Please try again later.");
            }
        }

        return (true, null);
    }

    public async Task<(bool success, string? errorMessage)> ForgotPasswordAsync(string email, string? baseUrl = null)
    {
        var user = await _userManager.FindByEmailAsync(email);

        if (user == null || !(await _userManager.IsEmailConfirmedAsync(user)))
        {
            // Don't reveal that user doesn't exist or email is not confirmed
            return (true, null);
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

        if (!string.IsNullOrEmpty(baseUrl))
        {
            var resetLink = $"{baseUrl}/api/Users/reset-password?email={user.Email}&token={encodedToken}";

            try
            {
                await _emailService.SendPasswordResetAsync(
                    user.Email!,
                    user.UserName ?? user.Email!,
                    resetLink);

                _logger.LogInformation("Password reset email sent to {Email}", user.Email);

                // Log password reset request
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
                return (false, "Failed to send email. Please try again later.");
            }
        }

        return (true, null);
    }

    public async Task<(bool success, IEnumerable<IdentityError>? errors)> ResetPasswordAsync(string email, string token, string newPassword)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
        {
            return (false, new[]
            {
                new IdentityError { Description = "Invalid request" }
            });
        }

        var decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(token));
        var result = await _userManager.ResetPasswordAsync(user, decodedToken, newPassword);

        if (!result.Succeeded)
        {
            _logger.LogWarning("Password reset failed for {Email}", user.Email);
            return (false, result.Errors);
        }

        _logger.LogInformation("Password reset successful for {Email}", user.Email);

        // Log password reset completion
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

        return (true, null);
    }

    public async Task<(bool success, UserViewModel? user, string? errorMessage)> CreateUserAsync(
        string email, string userName, string password, string? phoneNumber, bool emailConfirmed, List<string>? roles, string adminUserId)
    {
        var existingUser = await _userManager.FindByEmailAsync(email);
        if (existingUser != null)
            return (false, null, "User with this email already exists");

        var user = new ApplicationUser
        {
            UserName = userName,
            Email = email,
            EmailConfirmed = emailConfirmed,
            PhoneNumber = phoneNumber
        };

        var result = await _userManager.CreateAsync(user, password);

        if (!result.Succeeded)
        {
            return (false, null, string.Join(", ", result.Errors.Select(e => e.Description)));
        }

        if (roles != null && roles.Any())
        {
            foreach (var roleId in roles)
            {
                var role = await _roleManager.FindByIdAsync(roleId);
                if (role != null)
                {
                    await _userManager.AddToRoleAsync(user, role.Name!);
                }
            }
        }

        var userRoles = await _userManager.GetRolesAsync(user);

        // Log user creation by admin
        await _activityLogService.LogActivityAsync(
            action: "USER_CREATED",
            entityType: "USER",
            entityId: user.Id,
            entityName: user.UserName,
            description: $"User created by admin: {user.Email}",
            severity: "INFO",
            metadata: new Dictionary<string, object>
            {
                ["Roles"] = string.Join(", ", userRoles),
                ["EmailConfirmed"] = emailConfirmed
            }
        );

        var userViewModel = new UserViewModel
        {
            Id = user.Id,
            Email = user.Email!,
            UserName = user.UserName!,
            EmailConfirmed = user.EmailConfirmed,
            PhoneNumber = user.PhoneNumber,
            Roles = userRoles.ToList()
        };

        return (true, userViewModel, null);
    }
}
