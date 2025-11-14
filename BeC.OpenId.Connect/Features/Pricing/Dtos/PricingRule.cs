using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BeC.OpenId.Connect.Features.Pricing.Dtos;

/// <summary>
/// Pricing rule entity for configurable pricing
/// </summary>
[Table("PricingRules")]
public class PricingRule
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(100)]
    public required string Name { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    // Rule type
    [Required]
    [MaxLength(50)]
    public required string Type { get; set; } // base_fare, per_mile, per_minute, vehicle_type, time_multiplier, service_addon, distance_band

    // Vehicle type (if applicable)
    [MaxLength(50)]
    public string? VehicleType { get; set; } // van, cargo_van, small_truck, etc.

    // Distance bands
    public double? MinDistance { get; set; } // in miles
    public double? MaxDistance { get; set; }

    // Time-based rules
    public TimeSpan? StartTime { get; set; }
    public TimeSpan? EndTime { get; set; }
    public bool? WeekendOnly { get; set; }
    public bool? WeekdayOnly { get; set; }

    // Pricing values
    [Column(TypeName = "decimal(10,2)")]
    public decimal? FixedAmount { get; set; }

    [Column(TypeName = "decimal(10,4)")]
    public decimal? PerMileRate { get; set; }

    [Column(TypeName = "decimal(10,4)")]
    public decimal? PerMinuteRate { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal? MultiplierPercentage { get; set; } // e.g., 1.5 for 50% surge

    // Service add-ons
    [MaxLength(50)]
    public string? ServiceAddonType { get; set; } // helpers, packing, assembly, storage

    // Priority (lower = higher priority)
    public int Priority { get; set; } = 100;

    // Status
    public bool IsActive { get; set; } = true;

    // Audit
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string? CreatedBy { get; set; }
}

/// <summary>
/// Request to calculate pricing for a job
/// </summary>
public class PricingCalculationRequest
{
    public required string PickupAddress { get; set; }
    public required string DeliveryAddress { get; set; }
    public required string VehicleType { get; set; }
    public DateTime ScheduledDate { get; set; }
    public List<string>? ServiceAddons { get; set; }
    public int? NumberOfHelpers { get; set; }

    // Optional: if already calculated
    public double? DistanceInMiles { get; set; }
    public int? EstimatedDurationMinutes { get; set; }
}

/// <summary>
/// Result of pricing calculation
/// </summary>
public class PricingCalculationResult
{
    public decimal BaseFare { get; set; }
    public decimal DistanceCharge { get; set; }
    public decimal TimeCharge { get; set; }
    public decimal VehicleTypeCharge { get; set; }
    public decimal ServiceAddonsCharge { get; set; }
    public decimal SurgeMultiplier { get; set; } = 1.0m;
    public decimal SubTotal { get; set; }
    public decimal PlatformFee { get; set; }
    public decimal TotalPrice { get; set; }

    // Breakdown
    public List<PriceBreakdownItem> Breakdown { get; set; } = new();

    // Distance/duration info
    public double DistanceInMiles { get; set; }
    public int EstimatedDurationMinutes { get; set; }
    public string DistanceText { get; set; } = string.Empty;
    public string DurationText { get; set; } = string.Empty;
}

/// <summary>
/// Individual item in price breakdown
/// </summary>
public class PriceBreakdownItem
{
    public required string Description { get; set; }
    public decimal Amount { get; set; }
    public string? Details { get; set; }
}

/// <summary>
/// Pricing history for auditing
/// </summary>
[Table("PricingHistory")]
public class PricingHistory
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid? JobId { get; set; }
    public string? CustomerId { get; set; }

    [Required]
    [MaxLength(500)]
    public required string PickupAddress { get; set; }

    [Required]
    [MaxLength(500)]
    public required string DeliveryAddress { get; set; }

    [Required]
    [MaxLength(50)]
    public required string VehicleType { get; set; }

    public DateTime ScheduledDate { get; set; }

    public double DistanceInMiles { get; set; }
    public int EstimatedDurationMinutes { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal BaseFare { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal DistanceCharge { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal TimeCharge { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal VehicleTypeCharge { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal ServiceAddonsCharge { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal SurgeMultiplier { get; set; } = 1.0m;

    [Column(TypeName = "decimal(10,2)")]
    public decimal TotalPrice { get; set; }

    [Column(TypeName = "nvarchar(max)")]
    public string? BreakdownJson { get; set; }

    [Column(TypeName = "nvarchar(max)")]
    public string? ServiceAddons { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
