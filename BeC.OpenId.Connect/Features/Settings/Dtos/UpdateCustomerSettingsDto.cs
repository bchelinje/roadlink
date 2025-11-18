namespace BeC.OpenId.Connect.Features.Settings.Dtos;

public record UpdateCustomerSettingsDto
{
    // Booking Preferences
    public string? DefaultVehicleType { get; init; }
    public bool? AutoBookFavoriteDriver { get; init; }
    public bool? AllowAlternativeDrivers { get; init; }
    public decimal? PreferredMaxDistance { get; init; }
    public string? DefaultPickupAddress { get; init; }
    public string? DefaultDeliveryAddress { get; init; }

    // Payment Preferences
    public string? DefaultPaymentMethodId { get; init; }
    public bool? SavePaymentMethods { get; init; }
    public bool? AutoTipEnabled { get; init; }
    public decimal? DefaultTipPercentage { get; init; }
    public bool? RequestReceiptByEmail { get; init; }

    // Notification Preferences
    public bool? NotifyOnDriverAssigned { get; init; }
    public bool? NotifyOnDriverArriving { get; init; }
    public bool? NotifyOnJobStarted { get; init; }
    public bool? NotifyOnJobCompleted { get; init; }
    public bool? NotifyOnSpecialOffers { get; init; }

    // Experience Preferences
    public bool? ShowDriverRating { get; init; }
    public bool? ShowPriceEstimate { get; init; }
    public bool? ShowDriverLocation { get; init; }
    public bool? EnableJobTracking { get; init; }

    // Accessibility
    public bool? RequireAccessibleVehicle { get; init; }
    public bool? RequireDriverAssistance { get; init; }
    public string? SpecialRequirements { get; init; }
}
