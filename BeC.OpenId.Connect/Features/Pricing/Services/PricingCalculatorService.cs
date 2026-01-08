using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using BeC.OpenId.Connect.Dto;
using BeC.OpenId.Connect.Features.Pricing.Dtos;
using BeC.OpenId.Connect.Features.Pricing.Services.Interfaces;
using BeC.OpenId.Connect.Infrastructure.Maps;

namespace BeC.OpenId.Connect.Features.Pricing.Services;

/// <summary>
/// Implementation of dynamic pricing calculator
/// </summary>
public class PricingCalculatorService : IPricingCalculatorService
{
    private readonly ApplicationDbContext _context;
    private readonly IGoogleMapsService _mapsService;
    private readonly ILogger<PricingCalculatorService> _logger;

    public PricingCalculatorService(
        ApplicationDbContext context,
        IGoogleMapsService mapsService,
        ILogger<PricingCalculatorService> logger)
    {
        _context = context;
        _mapsService = mapsService;
        _logger = logger;
    }

    public async Task<PricingCalculationResult> CalculatePriceAsync(PricingCalculationRequest request)
    {
        var result = new PricingCalculationResult();
        var breakdown = new List<PriceBreakdownItem>();

        // 1. Get distance and duration if not provided
        if (!request.DistanceInMiles.HasValue || !request.EstimatedDurationMinutes.HasValue)
        {
            var distanceResult = await _mapsService.CalculateDistanceAsync(
                request.PickupAddress,
                request.DeliveryAddress);

            if (distanceResult != null)
            {
                result.DistanceInMiles = distanceResult.DistanceInMiles;
                result.EstimatedDurationMinutes = distanceResult.DurationInMinutes;
                result.DistanceText = distanceResult.DistanceText;
                result.DurationText = distanceResult.DurationText;
            }
            else
            {
                // Fallback to provided values or defaults
                result.DistanceInMiles = request.DistanceInMiles ?? 10.0;
                result.EstimatedDurationMinutes = request.EstimatedDurationMinutes ?? 30;
            }
        }
        else
        {
            result.DistanceInMiles = request.DistanceInMiles.Value;
            result.EstimatedDurationMinutes = request.EstimatedDurationMinutes.Value;
        }

        // 2. Get active pricing rules
        var rules = await _context.PricingRules
            .Where(r => r.IsActive)
            .OrderBy(r => r.Priority)
            .ToListAsync();

        // 3. Calculate base fare
        var baseFareRule = rules.FirstOrDefault(r => r.Type == "base_fare");
        if (baseFareRule?.FixedAmount != null)
        {
            result.BaseFare = baseFareRule.FixedAmount.Value;
            breakdown.Add(new PriceBreakdownItem
            {
                Description = "Base Fare",
                Amount = result.BaseFare
            });
        }

        // 4. Calculate distance-based charges
        var distanceRules = rules.Where(r => r.Type == "per_mile" || r.Type == "distance_band").ToList();
        foreach (var rule in distanceRules)
        {
            if (rule.Type == "per_mile" && rule.PerMileRate.HasValue)
            {
                // Apply vehicle-specific rate if applicable
                if (string.IsNullOrEmpty(rule.VehicleType) || rule.VehicleType == request.VehicleType)
                {
                    var distanceCharge = (decimal)result.DistanceInMiles * rule.PerMileRate.Value;
                    result.DistanceCharge += distanceCharge;
                    breakdown.Add(new PriceBreakdownItem
                    {
                        Description = $"Distance ({result.DistanceInMiles:F2} miles @ ${rule.PerMileRate:F2}/mile)",
                        Amount = distanceCharge,
                        Details = rule.VehicleType
                    });
                }
            }
            else if (rule.Type == "distance_band")
            {
                // Check if distance falls within this band
                if (result.DistanceInMiles >= (rule.MinDistance ?? 0) &&
                    result.DistanceInMiles < (rule.MaxDistance ?? double.MaxValue))
                {
                    if (rule.FixedAmount.HasValue)
                    {
                        result.DistanceCharge += rule.FixedAmount.Value;
                        breakdown.Add(new PriceBreakdownItem
                        {
                            Description = $"Distance Band ({rule.MinDistance}-{rule.MaxDistance} miles)",
                            Amount = rule.FixedAmount.Value
                        });
                    }
                }
            }
        }

        // 5. Calculate time-based charges
        var timeRules = rules.Where(r => r.Type == "per_minute").ToList();
        foreach (var rule in timeRules)
        {
            if (rule.PerMinuteRate.HasValue)
            {
                var timeCharge = result.EstimatedDurationMinutes * rule.PerMinuteRate.Value;
                result.TimeCharge += timeCharge;
                breakdown.Add(new PriceBreakdownItem
                {
                    Description = $"Time ({result.EstimatedDurationMinutes} min @ ${rule.PerMinuteRate:F4}/min)",
                    Amount = timeCharge
                });
            }
        }

        // 6. Calculate vehicle type charges
        var vehicleRules = rules.Where(r => r.Type == "vehicle_type" && r.VehicleType == request.VehicleType).ToList();
        foreach (var rule in vehicleRules)
        {
            if (rule.FixedAmount.HasValue)
            {
                result.VehicleTypeCharge += rule.FixedAmount.Value;
                breakdown.Add(new PriceBreakdownItem
                {
                    Description = $"Vehicle Type - {request.VehicleType}",
                    Amount = rule.FixedAmount.Value
                });
            }
        }

        // 7. Calculate service add-ons
        if (request.ServiceAddons?.Any() == true)
        {
            var addonRules = rules.Where(r => r.Type == "service_addon").ToList();
            foreach (var addon in request.ServiceAddons)
            {
                var addonRule = addonRules.FirstOrDefault(r => r.ServiceAddonType == addon);
                if (addonRule?.FixedAmount != null)
                {
                    var addonCharge = addonRule.FixedAmount.Value;
                    if (addon == "helpers" && request.NumberOfHelpers.HasValue)
                    {
                        addonCharge *= request.NumberOfHelpers.Value;
                    }

                    result.ServiceAddonsCharge += addonCharge;
                    breakdown.Add(new PriceBreakdownItem
                    {
                        Description = $"Service Add-on: {addon}",
                        Amount = addonCharge,
                        Details = request.NumberOfHelpers.HasValue ? $"{request.NumberOfHelpers} helpers" : null
                    });
                }
            }
        }

        // 7a. Calculate stairs/floor charges
        var stairsFloorCharges = 0m;

        // Pickup floor charge (if no elevator and above ground floor)
        if (request.PickupFloorNumber.HasValue && request.PickupFloorNumber > 1 &&
            request.PickupHasElevator == false)
        {
            var floorRule = rules.FirstOrDefault(r => r.Type == "service_addon" &&
                r.ServiceAddonType == "floor_charge");
            if (floorRule?.FixedAmount != null)
            {
                var floorCharge = floorRule.FixedAmount.Value * (request.PickupFloorNumber.Value - 1);
                stairsFloorCharges += floorCharge;
                breakdown.Add(new PriceBreakdownItem
                {
                    Description = $"Pickup Floor Charge (Floor {request.PickupFloorNumber}, No Elevator)",
                    Amount = floorCharge,
                    Details = $"{request.PickupFloorNumber.Value - 1} floors @ ${floorRule.FixedAmount:F2}/floor"
                });
            }
        }

        // Delivery floor charge (if no elevator and above ground floor)
        if (request.DeliveryFloorNumber.HasValue && request.DeliveryFloorNumber > 1 &&
            request.DeliveryHasElevator == false)
        {
            var floorRule = rules.FirstOrDefault(r => r.Type == "service_addon" &&
                r.ServiceAddonType == "floor_charge");
            if (floorRule?.FixedAmount != null)
            {
                var floorCharge = floorRule.FixedAmount.Value * (request.DeliveryFloorNumber.Value - 1);
                stairsFloorCharges += floorCharge;
                breakdown.Add(new PriceBreakdownItem
                {
                    Description = $"Delivery Floor Charge (Floor {request.DeliveryFloorNumber}, No Elevator)",
                    Amount = floorCharge,
                    Details = $"{request.DeliveryFloorNumber.Value - 1} floors @ ${floorRule.FixedAmount:F2}/floor"
                });
            }
        }

        // Additional stairs charge
        if (request.NumberOfStairFlights.HasValue && request.NumberOfStairFlights > 0)
        {
            var stairsRule = rules.FirstOrDefault(r => r.Type == "service_addon" &&
                r.ServiceAddonType == "stairs_charge");
            if (stairsRule?.FixedAmount != null)
            {
                var stairsCharge = stairsRule.FixedAmount.Value * request.NumberOfStairFlights.Value;
                stairsFloorCharges += stairsCharge;
                breakdown.Add(new PriceBreakdownItem
                {
                    Description = $"Stairs Charge",
                    Amount = stairsCharge,
                    Details = $"{request.NumberOfStairFlights} flight(s) @ ${stairsRule.FixedAmount:F2}/flight"
                });
            }
        }

        result.ServiceAddonsCharge += stairsFloorCharges;

        // 7b. Calculate waiting time charges
        if (request.WaitingTimeMinutes.HasValue && request.WaitingTimeMinutes > 0)
        {
            var waitingRule = rules.FirstOrDefault(r => r.Type == "service_addon" &&
                r.ServiceAddonType == "waiting_time");
            if (waitingRule?.PerMinuteRate != null)
            {
                var waitingCharge = waitingRule.PerMinuteRate.Value * request.WaitingTimeMinutes.Value;
                result.ServiceAddonsCharge += waitingCharge;
                breakdown.Add(new PriceBreakdownItem
                {
                    Description = $"Waiting Time Charge",
                    Amount = waitingCharge,
                    Details = $"{request.WaitingTimeMinutes} min @ ${waitingRule.PerMinuteRate:F4}/min"
                });
            }
        }

        // 8. Calculate subtotal
        result.SubTotal = result.BaseFare + result.DistanceCharge + result.TimeCharge +
                         result.VehicleTypeCharge + result.ServiceAddonsCharge;

        // 9. Apply time-based multipliers (surge pricing)
        var timeMultipliers = rules.Where(r => r.Type == "time_multiplier").ToList();
        foreach (var multiplierRule in timeMultipliers)
        {
            bool applies = true;

            // Check time of day
            if (multiplierRule.StartTime.HasValue && multiplierRule.EndTime.HasValue)
            {
                var jobTime = request.ScheduledDate.TimeOfDay;
                applies = jobTime >= multiplierRule.StartTime && jobTime <= multiplierRule.EndTime;
            }

            // Check day of week
            if (applies && multiplierRule.WeekendOnly == true)
            {
                applies = request.ScheduledDate.DayOfWeek == DayOfWeek.Saturday ||
                         request.ScheduledDate.DayOfWeek == DayOfWeek.Sunday;
            }
            else if (applies && multiplierRule.WeekdayOnly == true)
            {
                applies = request.ScheduledDate.DayOfWeek != DayOfWeek.Saturday &&
                         request.ScheduledDate.DayOfWeek != DayOfWeek.Sunday;
            }

            if (applies && multiplierRule.MultiplierPercentage.HasValue)
            {
                result.SurgeMultiplier = Math.Max(result.SurgeMultiplier, multiplierRule.MultiplierPercentage.Value);
                breakdown.Add(new PriceBreakdownItem
                {
                    Description = $"Peak Time Multiplier ({multiplierRule.Name})",
                    Amount = result.SubTotal * (multiplierRule.MultiplierPercentage.Value - 1),
                    Details = $"{(multiplierRule.MultiplierPercentage.Value - 1) * 100:F0}% surge"
                });
            }
        }

        // Apply surge multiplier
        if (result.SurgeMultiplier > 1.0m)
        {
            result.SubTotal *= result.SurgeMultiplier;
        }

        // 10. Calculate platform fee (e.g., 10% of subtotal)
        result.PlatformFee = result.SubTotal * 0.10m;
        breakdown.Add(new PriceBreakdownItem
        {
            Description = "Platform Fee (10%)",
            Amount = result.PlatformFee
        });

        // 11. Calculate total
        result.TotalPrice = result.SubTotal + result.PlatformFee;

        // 12. Enforce Stripe minimum charge amount (£0.30 GBP / $0.50 USD)
        const decimal MINIMUM_CHARGE_GBP = 0.30m;
        const decimal MINIMUM_CHARGE_USD = 0.50m;

        // Use GBP minimum for UK, USD for others
        var minimumCharge = MINIMUM_CHARGE_GBP; // Default to GBP since we're in UK

        if (result.TotalPrice < minimumCharge)
        {
            _logger.LogWarning("Calculated price £{Price} is below Stripe minimum. Adjusting to £{Minimum}",
                result.TotalPrice, minimumCharge);

            var adjustment = minimumCharge - result.TotalPrice;
            breakdown.Add(new PriceBreakdownItem
            {
                Description = "Minimum Charge Adjustment",
                Amount = adjustment,
                Details = $"Stripe requires minimum £{minimumCharge:F2} charge"
            });

            result.TotalPrice = minimumCharge;
        }

        result.Breakdown = breakdown;

        _logger.LogInformation("Calculated price: ${TotalPrice} for {Distance} miles, vehicle: {VehicleType}",
            result.TotalPrice, result.DistanceInMiles, request.VehicleType);

        return result;
    }

    public async Task<decimal> GetQuickEstimateAsync(double distanceInMiles, string vehicleType)
    {
        // Simple estimation without full calculation
        // Base fare + distance rate
        var baseFare = 15.0m; // Default base fare
        var perMileRate = 2.5m; // Default per mile rate

        // Get actual rates from database
        var baseFareRule = await _context.PricingRules
            .Where(r => r.Type == "base_fare" && r.IsActive)
            .FirstOrDefaultAsync();

        if (baseFareRule?.FixedAmount != null)
        {
            baseFare = baseFareRule.FixedAmount.Value;
        }

        var perMileRule = await _context.PricingRules
            .Where(r => r.Type == "per_mile" && r.IsActive &&
                       (r.VehicleType == null || r.VehicleType == vehicleType))
            .FirstOrDefaultAsync();

        if (perMileRule?.PerMileRate != null)
        {
            perMileRate = perMileRule.PerMileRate.Value;
        }

        // Calculate estimate
        var estimate = baseFare + ((decimal)distanceInMiles * perMileRate);

        // Stripe minimum charge requirement (£0.30 for GBP)
        const decimal MINIMUM_CHARGE = 0.30m;
        if (estimate < MINIMUM_CHARGE)
        {
            _logger.LogWarning("Quick estimate £{Estimate} is below Stripe minimum. Returning £{Minimum}",
                estimate, MINIMUM_CHARGE);
            estimate = MINIMUM_CHARGE;
        }

        return estimate;
    }

    public async Task<decimal> CalculateSurgeMultiplierAsync(DateTime scheduledDate, string pickupAddress)
    {
        // Check for active time-based multipliers
        var multipliers = await _context.PricingRules
            .Where(r => r.Type == "time_multiplier" && r.IsActive)
            .ToListAsync();

        decimal maxMultiplier = 1.0m;

        foreach (var rule in multipliers)
        {
            bool applies = true;

            if (rule.StartTime.HasValue && rule.EndTime.HasValue)
            {
                var jobTime = scheduledDate.TimeOfDay;
                applies = jobTime >= rule.StartTime && jobTime <= rule.EndTime;
            }

            if (applies && rule.WeekendOnly == true)
            {
                applies = scheduledDate.DayOfWeek == DayOfWeek.Saturday ||
                         scheduledDate.DayOfWeek == DayOfWeek.Sunday;
            }
            else if (applies && rule.WeekdayOnly == true)
            {
                applies = scheduledDate.DayOfWeek != DayOfWeek.Saturday &&
                         scheduledDate.DayOfWeek != DayOfWeek.Sunday;
            }

            if (applies && rule.MultiplierPercentage.HasValue)
            {
                maxMultiplier = Math.Max(maxMultiplier, rule.MultiplierPercentage.Value);
            }
        }

        // Could also factor in current demand here
        // e.g., count active jobs in the area

        return maxMultiplier;
    }

    public async Task SavePricingHistoryAsync(PricingCalculationRequest request, PricingCalculationResult result, Guid? jobId = null, string? customerId = null)
    {
        try
        {
            var history = new PricingHistory
            {
                JobId = jobId,
                CustomerId = customerId,
                PickupAddress = request.PickupAddress,
                DeliveryAddress = request.DeliveryAddress,
                VehicleType = request.VehicleType,
                ScheduledDate = request.ScheduledDate,
                DistanceInMiles = result.DistanceInMiles,
                EstimatedDurationMinutes = result.EstimatedDurationMinutes,
                BaseFare = result.BaseFare,
                DistanceCharge = result.DistanceCharge,
                TimeCharge = result.TimeCharge,
                VehicleTypeCharge = result.VehicleTypeCharge,
                ServiceAddonsCharge = result.ServiceAddonsCharge,
                SurgeMultiplier = result.SurgeMultiplier,
                TotalPrice = result.TotalPrice,
                BreakdownJson = JsonSerializer.Serialize(result.Breakdown),
                ServiceAddons = request.ServiceAddons != null ? JsonSerializer.Serialize(request.ServiceAddons) : null
            };

            _context.PricingHistory.Add(history);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Saved pricing history {HistoryId} for job {JobId}", history.Id, jobId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving pricing history");
        }
    }
}
