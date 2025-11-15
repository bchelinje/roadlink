namespace BeC.OpenId.Connect.Features.Users.ViewModels;

/// <summary>
/// View model for user response
/// </summary>
public class UserViewModel
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public bool EmailConfirmed { get; set; }
    public string? PhoneNumber { get; set; }
    public bool PhoneNumberConfirmed { get; set; }
    public List<string> Roles { get; set; } = new();
    public bool LockoutEnabled { get; set; }
    public DateTimeOffset? LockoutEnd { get; set; }
    public bool TwoFactorEnabled { get; set; }
    public List<UserClaimViewModel>? Claims { get; set; }
}

/// <summary>
/// View model for user claim
/// </summary>
public class UserClaimViewModel
{
    public string Type { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}
