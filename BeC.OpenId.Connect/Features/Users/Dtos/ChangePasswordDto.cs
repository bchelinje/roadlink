namespace BeC.OpenId.Connect.Features.Users.Dtos;

public class ChangePasswordDto
{
    /// <summary>
    /// Current password for verification
    /// </summary>
    public required string CurrentPassword { get; set; }

    /// <summary>
    /// New password to set
    /// </summary>
    public required string NewPassword { get; set; }
}