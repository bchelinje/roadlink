using Microsoft.AspNetCore.Identity;

namespace BeC.OpenId.Connect.Features.Users.Dtos;

public class ApplicationUser : IdentityUser
{
    /// <summary>
    /// User's display name
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// URL to user's profile picture
    /// </summary>
    public string? ProfilePictureUrl { get; set; }

    /// <summary>
    /// Date when account was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Date when profile was last updated
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Soft delete flag - user account is anonymized/deleted
    /// </summary>
    public bool IsDeleted { get; set; } = false;

    /// <summary>
    /// Date when account was deleted/anonymized
    /// </summary>
    public DateTime? DeletedAt { get; set; }

    /// <summary>
    /// Admin or system user who processed the deletion
    /// </summary>
    public string? DeletedBy { get; set; }

    /// <summary>
    /// Type of deletion performed (soft/hard)
    /// </summary>
    public string? DeletionType { get; set; }
}