using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BeC.OpenId.Connect.Dto;
using BeC.OpenId.Connect.Features.Pricing.Dtos;
using BeC.OpenId.Connect.Features.Pricing.Services.Interfaces;
using BeC.OpenId.Connect.Infrastructure.Authorization;

namespace BeC.OpenId.Connect.Features.Pricing.Controllers;

/// <summary>
/// API endpoints for dynamic pricing and calculations
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PricingController : ControllerBase
{
    private readonly IPricingCalculatorService _pricingService;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<PricingController> _logger;

    public PricingController(
        IPricingCalculatorService pricingService,
        ApplicationDbContext context,
        ILogger<PricingController> logger)
    {
        _pricingService = pricingService;
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Calculate full pricing for a job with detailed breakdown
    /// </summary>
    /// <param name="request">Pricing calculation request with pickup/delivery addresses</param>
    /// <returns>Detailed pricing calculation with breakdown</returns>
    [HttpPost("calculate")]
    [ProducesResponseType(typeof(PricingCalculationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PricingCalculationResult>> CalculatePrice([FromBody] PricingCalculationRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.PickupAddress) || string.IsNullOrWhiteSpace(request.DeliveryAddress))
            {
                return BadRequest(new { message = "Pickup and delivery addresses are required" });
            }

            if (string.IsNullOrWhiteSpace(request.VehicleType))
            {
                return BadRequest(new { message = "Vehicle type is required" });
            }

            var result = await _pricingService.CalculatePriceAsync(request);

            _logger.LogInformation("Calculated price: ${TotalPrice} for route {Pickup} -> {Delivery}",
                result.TotalPrice, request.PickupAddress, request.DeliveryAddress);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating price");
            return StatusCode(500, new { message = "Error calculating price", error = ex.Message });
        }
    }

    /// <summary>
    /// Get a quick price estimate based on distance and vehicle type
    /// </summary>
    /// <param name="distanceInMiles">Distance in miles</param>
    /// <param name="vehicleType">Vehicle type (van, cargo_van, small_truck, etc.)</param>
    /// <returns>Quick estimated price</returns>
    [HttpGet("estimate")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> GetQuickEstimate([FromQuery] double distanceInMiles, [FromQuery] string vehicleType)
    {
        try
        {
            if (distanceInMiles <= 0)
            {
                return BadRequest(new { message = "Distance must be greater than 0" });
            }

            if (string.IsNullOrWhiteSpace(vehicleType))
            {
                return BadRequest(new { message = "Vehicle type is required" });
            }

            var estimate = await _pricingService.GetQuickEstimateAsync(distanceInMiles, vehicleType);

            return Ok(new
            {
                estimatedPrice = estimate,
                distanceInMiles = distanceInMiles,
                vehicleType = vehicleType,
                note = "This is a quick estimate. Final price may vary based on time, add-ons, and surge pricing."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting quick estimate");
            return StatusCode(500, new { message = "Error getting estimate", error = ex.Message });
        }
    }

    /// <summary>
    /// Calculate surge multiplier for a specific date and location
    /// </summary>
    /// <param name="scheduledDate">Scheduled date for the job</param>
    /// <param name="pickupAddress">Pickup address</param>
    /// <returns>Surge multiplier (1.0 = no surge, 1.5 = 50% surge)</returns>
    [HttpGet("surge")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<ActionResult> GetSurgeMultiplier([FromQuery] DateTime scheduledDate, [FromQuery] string pickupAddress)
    {
        try
        {
            var multiplier = await _pricingService.CalculateSurgeMultiplierAsync(scheduledDate, pickupAddress);

            return Ok(new
            {
                surgeMultiplier = multiplier,
                surgePercentage = (multiplier - 1.0m) * 100,
                scheduledDate = scheduledDate,
                hasSurge = multiplier > 1.0m
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating surge");
            return StatusCode(500, new { message = "Error calculating surge", error = ex.Message });
        }
    }

    /// <summary>
    /// Get pricing history for a specific job (Admin only)
    /// </summary>
    /// <param name="jobId">Job ID</param>
    /// <returns>Pricing history for the job</returns>
    [HttpGet("history/{jobId}")]
    [Authorize(Policy = Policies.RequireAdminOrSuperAdmin)]
    [ProducesResponseType(typeof(List<PricingHistory>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<PricingHistory>>> GetPricingHistory(Guid jobId)
    {
        var history = await _context.PricingHistory
            .Where(h => h.JobId == jobId)
            .OrderByDescending(h => h.CreatedAt)
            .ToListAsync();

        if (!history.Any())
        {
            return NotFound(new { message = "No pricing history found for this job" });
        }

        return Ok(history);
    }

    /// <summary>
    /// Get all active pricing rules (Admin only)
    /// </summary>
    /// <returns>List of active pricing rules</returns>
    [HttpGet("rules")]
    [Authorize(Policy = Policies.RequireAdminOrSuperAdmin)]
    [ProducesResponseType(typeof(List<PricingRule>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<PricingRule>>> GetPricingRules([FromQuery] bool? isActive = null)
    {
        var query = _context.PricingRules.AsQueryable();

        if (isActive.HasValue)
        {
            query = query.Where(r => r.IsActive == isActive.Value);
        }

        var rules = await query
            .OrderBy(r => r.Priority)
            .ThenBy(r => r.Type)
            .ToListAsync();

        return Ok(rules);
    }

    /// <summary>
    /// Get a specific pricing rule by ID (Admin only)
    /// </summary>
    /// <param name="id">Rule ID</param>
    /// <returns>Pricing rule</returns>
    [HttpGet("rules/{id}")]
    [Authorize(Policy = Policies.RequireAdminOrSuperAdmin)]
    [ProducesResponseType(typeof(PricingRule), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PricingRule>> GetPricingRule(Guid id)
    {
        var rule = await _context.PricingRules.FindAsync(id);

        if (rule == null)
        {
            return NotFound(new { message = "Pricing rule not found" });
        }

        return Ok(rule);
    }

    /// <summary>
    /// Create a new pricing rule (Admin only)
    /// </summary>
    /// <param name="rule">Pricing rule to create</param>
    /// <returns>Created pricing rule</returns>
    [HttpPost("rules")]
    [Authorize(Policy = Policies.RequireAdminOrSuperAdmin)]
    [ProducesResponseType(typeof(PricingRule), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PricingRule>> CreatePricingRule([FromBody] PricingRule rule)
    {
        try
        {
            // Validate rule type
            var validTypes = new[] { "base_fare", "per_mile", "per_minute", "vehicle_type", "time_multiplier", "service_addon", "distance_band" };
            if (!validTypes.Contains(rule.Type))
            {
                return BadRequest(new { message = $"Invalid rule type. Must be one of: {string.Join(", ", validTypes)}" });
            }

            rule.Id = Guid.NewGuid();
            rule.CreatedAt = DateTime.UtcNow;
            rule.UpdatedAt = DateTime.UtcNow;

            _context.PricingRules.Add(rule);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created pricing rule {RuleId} - {RuleName}", rule.Id, rule.Name);

            return CreatedAtAction(nameof(GetPricingRule), new { id = rule.Id }, rule);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating pricing rule");
            return StatusCode(500, new { message = "Error creating pricing rule", error = ex.Message });
        }
    }

    /// <summary>
    /// Update an existing pricing rule (Admin only)
    /// </summary>
    /// <param name="id">Rule ID</param>
    /// <param name="updatedRule">Updated rule data</param>
    /// <returns>Updated pricing rule</returns>
    [HttpPut("rules/{id}")]
    [Authorize(Policy = Policies.RequireAdminOrSuperAdmin)]
    [ProducesResponseType(typeof(PricingRule), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PricingRule>> UpdatePricingRule(Guid id, [FromBody] PricingRule updatedRule)
    {
        try
        {
            var existingRule = await _context.PricingRules.FindAsync(id);

            if (existingRule == null)
            {
                return NotFound(new { message = "Pricing rule not found" });
            }

            // Update fields
            existingRule.Name = updatedRule.Name;
            existingRule.Description = updatedRule.Description;
            existingRule.Type = updatedRule.Type;
            existingRule.VehicleType = updatedRule.VehicleType;
            existingRule.MinDistance = updatedRule.MinDistance;
            existingRule.MaxDistance = updatedRule.MaxDistance;
            existingRule.StartTime = updatedRule.StartTime;
            existingRule.EndTime = updatedRule.EndTime;
            existingRule.WeekendOnly = updatedRule.WeekendOnly;
            existingRule.WeekdayOnly = updatedRule.WeekdayOnly;
            existingRule.FixedAmount = updatedRule.FixedAmount;
            existingRule.PerMileRate = updatedRule.PerMileRate;
            existingRule.PerMinuteRate = updatedRule.PerMinuteRate;
            existingRule.MultiplierPercentage = updatedRule.MultiplierPercentage;
            existingRule.ServiceAddonType = updatedRule.ServiceAddonType;
            existingRule.Priority = updatedRule.Priority;
            existingRule.IsActive = updatedRule.IsActive;
            existingRule.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated pricing rule {RuleId} - {RuleName}", id, existingRule.Name);

            return Ok(existingRule);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating pricing rule");
            return StatusCode(500, new { message = "Error updating pricing rule", error = ex.Message });
        }
    }

    /// <summary>
    /// Delete a pricing rule (Admin only)
    /// </summary>
    /// <param name="id">Rule ID</param>
    /// <returns>No content</returns>
    [HttpDelete("rules/{id}")]
    [Authorize(Policy = Policies.RequireAdminOrSuperAdmin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeletePricingRule(Guid id)
    {
        try
        {
            var rule = await _context.PricingRules.FindAsync(id);

            if (rule == null)
            {
                return NotFound(new { message = "Pricing rule not found" });
            }

            _context.PricingRules.Remove(rule);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Deleted pricing rule {RuleId} - {RuleName}", id, rule.Name);

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting pricing rule");
            return StatusCode(500, new { message = "Error deleting pricing rule", error = ex.Message });
        }
    }

    /// <summary>
    /// Toggle a pricing rule's active status (Admin only)
    /// </summary>
    /// <param name="id">Rule ID</param>
    /// <returns>Updated pricing rule</returns>
    [HttpPatch("rules/{id}/toggle")]
    [Authorize(Policy = Policies.RequireAdminOrSuperAdmin)]
    [ProducesResponseType(typeof(PricingRule), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PricingRule>> TogglePricingRule(Guid id)
    {
        try
        {
            var rule = await _context.PricingRules.FindAsync(id);

            if (rule == null)
            {
                return NotFound(new { message = "Pricing rule not found" });
            }

            rule.IsActive = !rule.IsActive;
            rule.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Toggled pricing rule {RuleId} - Active: {IsActive}", id, rule.IsActive);

            return Ok(rule);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling pricing rule");
            return StatusCode(500, new { message = "Error toggling pricing rule", error = ex.Message });
        }
    }
}
