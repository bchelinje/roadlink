namespace BeC.OpenId.Connect.Features.Users.Models;

/// <summary>
/// Model for updating user information
/// </summary>
public class UpdateUserModel
{
    public string? Email { get; set; }
    public string? UserName { get; set; }
    public string? PhoneNumber { get; set; }
    public bool? EmailConfirmed { get; set; }
    public bool? PhoneNumberConfirmed { get; set; }
    public List<string>? Roles { get; set; }
}
