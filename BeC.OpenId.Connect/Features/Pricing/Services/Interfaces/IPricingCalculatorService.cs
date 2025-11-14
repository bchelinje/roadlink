using BeC.OpenId.Connect.Features.Pricing.Dtos;

namespace BeC.OpenId.Connect.Features.Pricing.Services.Interfaces;

/// <summary>
/// Service for calculating dynamic pricing
/// </summary>
public interface IPricingCalculatorService
{
    /// <summary>
    /// Calculate price for a job based on all pricing rules
    /// </summary>
    Task<PricingCalculationResult> CalculatePriceAsync(PricingCalculationRequest request);

    /// <summary>
    /// Get quick price estimate (less accurate, faster)
    /// </summary>
    Task<decimal> GetQuickEstimateAsync(double distanceInMiles, string vehicleType);

    /// <summary>
    /// Calculate surge multiplier based on demand
    /// </summary>
    Task<decimal> CalculateSurgeMultiplierAsync(DateTime scheduledDate, string pickupAddress);

    /// <summary>
    /// Save pricing calculation to history
    /// </summary>
    Task SavePricingHistoryAsync(PricingCalculationRequest request, PricingCalculationResult result, Guid? jobId = null, string? customerId = null);
}
