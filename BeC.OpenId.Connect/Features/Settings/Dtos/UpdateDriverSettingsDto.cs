namespace BeC.OpenId.Connect.Features.Settings.Dtos;

public record UpdateDriverSettingsDto
{
    // Availability Settings
    public bool? AcceptingJobs { get; init; }
    public decimal? MaxServiceRadiusMiles { get; init; }
    public string? WorkingHours { get; init; }
    public string? DaysOff { get; init; }

    // Job Preferences
    public decimal? MinimumJobValue { get; init; }
    public decimal? MaximumJobDistanceMiles { get; init; }
    public string? PreferredJobTypes { get; init; }
    public string? PreferredVehicleTypes { get; init; }
    public bool? AutoAcceptJobs { get; init; }
    public int? AutoAcceptRadiusMiles { get; init; }

    // Payout Settings
    public string? PayoutFrequency { get; init; }
    public string? BankAccountLast4 { get; init; }
    public string? StripeAccountId { get; init; }
    public bool? InstantPayoutEnabled { get; init; }
    public decimal? MinimumPayoutAmount { get; init; }

    // Notification Settings
    public bool? NotifyOnNewJobsNearby { get; init; }
    public bool? NotifyOnJobRequests { get; init; }
    public bool? NotifyOnPayoutProcessed { get; init; }
    public bool? NotifyOnLowRating { get; init; }

    // Vehicle Preferences
    public Guid? DefaultVehicleId { get; init; }

    // Performance Settings
    public bool? SharePerformanceMetrics { get; init; }
    public bool? ParticipateInLeaderboard { get; init; }
}
