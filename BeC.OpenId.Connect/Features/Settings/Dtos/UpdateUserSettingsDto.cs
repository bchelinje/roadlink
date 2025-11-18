namespace BeC.OpenId.Connect.Features.Settings.Dtos;

public record UpdateUserSettingsDto
{
    // Profile Settings
    public string? PreferredLanguage { get; init; }
    public string? TimeZone { get; init; }
    public string? Currency { get; init; }
    public string? DateFormat { get; init; }
    public string? TimeFormat { get; init; }

    // Privacy Settings
    public bool? ShowProfileToPublic { get; init; }
    public bool? AllowDataSharing { get; init; }
    public bool? ShareLocationWithDriver { get; init; }
    public bool? ShowOnlineStatus { get; init; }
    public bool? AllowMarketingEmails { get; init; }

    // Security Settings
    public bool? TwoFactorEnabled { get; init; }
    public bool? EmailVerificationRequired { get; init; }
    public bool? PhoneVerificationRequired { get; init; }
    public int? SessionTimeoutMinutes { get; init; }
    public bool? RequirePasswordChangeEvery90Days { get; init; }

    // Communication Preferences
    public string? PreferredContactMethod { get; init; }

    // Display Preferences
    public string? Theme { get; init; }
    public bool? HighContrastMode { get; init; }
    public bool? ReducedMotion { get; init; }
}
