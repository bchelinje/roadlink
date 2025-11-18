using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BeC.OpenId.Connect.Features.Settings.Dtos;

/// <summary>
/// Customer-specific settings for booking preferences and payment methods
/// </summary>
[Table("CustomerSettings")]
public class CustomerSettings
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public required string UserId { get; set; } // FK to AspNetUsers

    // Booking Preferences
    [MaxLength(50)]
    public string? DefaultVehicleType { get; set; } = "van";

    public bool AutoBookFavoriteDriver { get; set; } = false;

    public bool AllowAlternativeDrivers { get; set; } = true;

    [Column(TypeName = "decimal(10,2)")]
    public decimal? PreferredMaxDistance { get; set; }

    [MaxLength(50)]
    public string? DefaultPickupAddress { get; set; }

    [MaxLength(50)]
    public string? DefaultDeliveryAddress { get; set; }

    // Payment Preferences
    [MaxLength(100)]
    public string? DefaultPaymentMethodId { get; set; } // Stripe payment method ID

    public bool SavePaymentMethods { get; set; } = true;

    public bool AutoTipEnabled { get; set; } = true;

    [Column(TypeName = "decimal(5,2)")]
    public decimal? DefaultTipPercentage { get; set; } = 15.0m;

    public bool RequestReceiptByEmail { get; set; } = true;

    // Notification Preferences (Customer-specific)
    public bool NotifyOnDriverAssigned { get; set; } = true;
    public bool NotifyOnDriverArriving { get; set; } = true;
    public bool NotifyOnJobStarted { get; set; } = true;
    public bool NotifyOnJobCompleted { get; set; } = true;
    public bool NotifyOnSpecialOffers { get; set; } = true;

    // Experience Preferences
    public bool ShowDriverRating { get; set; } = true;
    public bool ShowPriceEstimate { get; set; } = true;
    public bool ShowDriverLocation { get; set; } = true;
    public bool EnableJobTracking { get; set; } = true;

    // Accessibility
    public bool RequireAccessibleVehicle { get; set; } = false;
    public bool RequireDriverAssistance { get; set; } = false;

    [Column(TypeName = "nvarchar(max)")]
    public string? SpecialRequirements { get; set; } // JSON array

    // Audit
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
