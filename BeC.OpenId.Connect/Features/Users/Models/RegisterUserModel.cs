using System.ComponentModel.DataAnnotations;

namespace BeC.OpenId.Connect.Features.Users.Models;

/// <summary>
/// Model for user registration
/// </summary>
public class RegisterUserModel
{
    [Required]
    [EmailAddress]
    public required string Email { get; set; }

    [Required]
    [MinLength(8)]
    public required string Password { get; set; }

    public string? PhoneNumber { get; set; }
}
