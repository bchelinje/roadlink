using BeC.OpenId.Connect.Features.Users.Models;
using BeC.OpenId.Connect.Features.Users.ViewModels;
using Microsoft.AspNetCore.Identity;

namespace BeC.OpenId.Connect.Features.Users.Services.Interfaces;

/// <summary>
/// Service for managing users, roles, and authentication
/// </summary>
public interface IUserService
{
    /// <summary>
    /// Get all users with pagination and search (Admin)
    /// </summary>
    Task<UserListViewModel> GetUsersAsync(int page, int pageSize, string? searchTerm);

    /// <summary>
    /// Get user by ID (Admin)
    /// </summary>
    Task<UserViewModel?> GetUserByIdAsync(string id);

    /// <summary>
    /// Get current user profile
    /// </summary>
    Task<UserViewModel?> GetCurrentUserAsync(System.Security.Claims.ClaimsPrincipal userPrincipal);

    /// <summary>
    /// Register new user
    /// </summary>
    Task<(bool success, UserViewModel? user, IEnumerable<IdentityError>? errors)> RegisterUserAsync(RegisterUserModel model, string? baseUrl = null);

    /// <summary>
    /// Update user information
    /// </summary>
    Task<(bool success, UserViewModel? user, string? errorMessage)> UpdateUserAsync(string id, UpdateUserModel model);

    /// <summary>
    /// Delete user
    /// </summary>
    Task<(bool success, string? errorMessage)> DeleteUserAsync(string id, string currentUserId);

    /// <summary>
    /// Assign role to user (SuperAdmin)
    /// </summary>
    Task<(bool success, string? errorMessage)> AssignRoleAsync(string userId, AssignRoleModel model, string adminUserId);

    /// <summary>
    /// Remove role from user (SuperAdmin)
    /// </summary>
    Task<(bool success, string? errorMessage)> RemoveRoleAsync(string userId, string roleName, string currentUserId);

    /// <summary>
    /// Lock user account (Admin)
    /// </summary>
    Task<(bool success, string? errorMessage)> LockUserAsync(string userId, LockUserModel model, string currentUserId);

    /// <summary>
    /// Unlock user account (Admin)
    /// </summary>
    Task<(bool success, string? errorMessage)> UnlockUserAsync(string userId, string currentUserId);

    /// <summary>
    /// Change password for current user
    /// </summary>
    Task<(bool success, IEnumerable<IdentityError>? errors)> ChangePasswordAsync(string userId, ChangePasswordModel model);

    /// <summary>
    /// Admin change password for user
    /// </summary>
    Task<(bool success, string? errorMessage)> AdminChangePasswordAsync(string userId, string newPassword);

    /// <summary>
    /// Confirm email with token
    /// </summary>
    Task<(bool success, string? errorMessage)> ConfirmEmailAsync(string userId, string token);

    /// <summary>
    /// Resend email confirmation
    /// </summary>
    Task<(bool success, string? errorMessage)> ResendConfirmationAsync(string email, string? baseUrl = null);

    /// <summary>
    /// Request password reset
    /// </summary>
    Task<(bool success, string? errorMessage)> ForgotPasswordAsync(string email, string? baseUrl = null);

    /// <summary>
    /// Reset password with token
    /// </summary>
    Task<(bool success, IEnumerable<IdentityError>? errors)> ResetPasswordAsync(string email, string token, string newPassword);

    /// <summary>
    /// Create new user (Admin)
    /// </summary>
    Task<(bool success, UserViewModel? user, string? errorMessage)> CreateUserAsync(string email, string userName, string password, string? phoneNumber, bool emailConfirmed, List<string>? roles, string adminUserId);
}
