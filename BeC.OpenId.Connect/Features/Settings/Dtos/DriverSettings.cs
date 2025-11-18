using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BeC.OpenId.Connect.Features.Settings.Dtos;

/// <summary>
/// Driver-specific settings for availability, payouts, and job preferences
/// </summary>
[Table("DriverSettings")]
public class DriverSettings
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public required string UserId { get; set; } // FK to AspNetUsers

    // Availability Settings
    public bool AcceptingJobs { get; set; } = true;

    [Column(TypeName = "decimal(10,2)")]
    public decimal? MaxServiceRadiusMiles { get; set; } = 25.0m;

    // Working hours stored as JSON: { "monday": { "enabled": true, "start": "09:00", "end": "17:00" }, ... }
    [Column(TypeName = "nvarchar(max)")]
    public string? WorkingHours { get; set; }

    // Days off stored as JSON array: ["2025-01-01", "2025-12-25"]
    [Column(TypeName = "nvarchar(max)")]
    public string? DaysOff { get; set; }

    // Job Preferences
    [Column(TypeName = "decimal(10,2)")]
    public decimal? MinimumJobValue { get; set; } = 0.0m;

    [Column(TypeName = "decimal(10,2)")]
    public decimal? MaximumJobDistanceMiles { get; set; }

    // Preferred job types stored as JSON array: ["local_move", "long_distance", "commercial"]
    [Column(TypeName = "nvarchar(max)")]
    public string? PreferredJobTypes { get; set; }

    // Preferred vehicle types stored as JSON array: ["van", "small_truck"]
    [Column(TypeName = "nvarchar(max)")]
    public string? PreferredVehicleTypes { get; set; }

    public bool AutoAcceptJobs { get; set; } = false;
    public int AutoAcceptRadiusMiles { get; set; } = 10;

    // Payout Settings
    [MaxLength(20)]
    public string? PayoutFrequency { get; set; } = "weekly"; // daily, weekly, biweekly, monthly

    [MaxLength(100)]
    public string? BankAccountLast4 { get; set; }

    [MaxLength(50)]
    public string? StripeAccountId { get; set; }

    public bool InstantPayoutEnabled { get; set; } = false;

    [Column(TypeName = "decimal(5,2)")]
    public decimal? MinimumPayoutAmount { get; set; } = 10.0m;

    // Notification Settings (Driver-specific)
    public bool NotifyOnNewJobsNearby { get; set; } = true;
    public bool NotifyOnJobRequests { get; set; } = true;
    public bool NotifyOnPayoutProcessed { get; set; } = true;
    public bool NotifyOnLowRating { get; set; } = true;

    // Vehicle Preferences
    public Guid? DefaultVehicleId { get; set; } // FK to Vehicles

    // Performance Settings
    public bool SharePerformanceMetrics { get; set; } = true;
    public bool ParticipateInLeaderboard { get; set; } = true;

    // Audit
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
