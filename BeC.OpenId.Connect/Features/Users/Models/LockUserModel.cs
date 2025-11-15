namespace BeC.OpenId.Connect.Features.Users.Models;

/// <summary>
/// Model for locking user account
/// </summary>
public class LockUserModel
{
    public int? LockoutDurationMinutes { get; set; }
    public string? Reason { get; set; }
}
