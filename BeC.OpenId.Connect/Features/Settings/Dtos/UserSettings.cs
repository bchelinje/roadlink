using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BeC.OpenId.Connect.Features.Settings.Dtos;

/// <summary>
/// User profile, security, and privacy settings
/// </summary>
[Table("UserSettings")]
public class UserSettings
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public required string UserId { get; set; } // FK to AspNetUsers

    // Profile Settings
    [MaxLength(100)]
    public string? PreferredLanguage { get; set; } = "en";

    [MaxLength(50)]
    public string? TimeZone { get; set; } = "UTC";

    [MaxLength(10)]
    public string? Currency { get; set; } = "USD";

    [MaxLength(20)]
    public string? DateFormat { get; set; } = "MM/DD/YYYY";

    [MaxLength(20)]
    public string? TimeFormat { get; set; } = "12h"; // 12h or 24h

    // Privacy Settings
    public bool ShowProfileToPublic { get; set; } = false;
    public bool AllowDataSharing { get; set; } = false;
    public bool ShareLocationWithDriver { get; set; } = true;
    public bool ShowOnlineStatus { get; set; } = true;
    public bool AllowMarketingEmails { get; set; } = true;

    // Security Settings
    public bool TwoFactorEnabled { get; set; } = false;
    public bool EmailVerificationRequired { get; set; } = true;
    public bool PhoneVerificationRequired { get; set; } = false;
    public int SessionTimeoutMinutes { get; set; } = 60;
    public bool RequirePasswordChangeEvery90Days { get; set; } = false;

    // Communication Preferences
    [MaxLength(20)]
    public string? PreferredContactMethod { get; set; } = "email"; // email, sms, push

    // Display Preferences
    [MaxLength(20)]
    public string? Theme { get; set; } = "light"; // light, dark, auto
    public bool HighContrastMode { get; set; } = false;
    public bool ReducedMotion { get; set; } = false;

    // Audit
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
