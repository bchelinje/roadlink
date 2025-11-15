using System.ComponentModel.DataAnnotations;

namespace BeC.OpenId.Connect.Features.Users.Models;

/// <summary>
/// Model for changing password
/// </summary>
public class ChangePasswordModel
{
    [Required]
    public required string CurrentPassword { get; set; }

    [Required]
    [MinLength(8)]
    public required string NewPassword { get; set; }
}
