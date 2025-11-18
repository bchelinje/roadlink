using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BeC.OpenId.Connect.Dto;
using BeC.OpenId.Connect.Features.Pricing.Dtos;
using System.Security.Claims;
using BeC.OpenId.Connect.Features.ActivityLogs.Services.Interfaces;
using OpenIddict.Validation.AspNetCore;

namespace BeC.OpenId.Connect.Features.Pricing.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
public class PromotionCodesController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<PromotionCodesController> _logger;
    private readonly IActivityLogService _activityLogService;

    public PromotionCodesController(
        ApplicationDbContext context,
        ILogger<PromotionCodesController> logger,
        IActivityLogService activityLogService)
    {
        _context = context;
        _logger = logger;
        _activityLogService = activityLogService;
    }

    /// <summary>
    /// Validate a promotion code
    /// </summary>
    [HttpPost("validate")]
    [ProducesResponseType(typeof(PromotionCodeValidationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PromotionCodeValidationResult>> ValidatePromoCode([FromBody] ValidatePromoCodeDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var promo = await _context.PromotionCodes
            .FirstOrDefaultAsync(p => p.Code.ToLower() == dto.Code.ToLower());

        if (promo == null)
            return Ok(new PromotionCodeValidationResult
            {
                IsValid = false,
                Message = "Invalid promotion code"
            });

        // Check if active
        if (!promo.IsActive)
            return Ok(new PromotionCodeValidationResult
            {
                IsValid = false,
                Message = "This promotion code is no longer active"
            });

        // Check validity period
        var now = DateTime.UtcNow;
        if (promo.ValidFrom.HasValue && now < promo.ValidFrom)
            return Ok(new PromotionCodeValidationResult
            {
                IsValid = false,
                Message = $"This promotion code is not valid until {promo.ValidFrom:yyyy-MM-dd}"
            });

        if (promo.ValidUntil.HasValue && now > promo.ValidUntil)
            return Ok(new PromotionCodeValidationResult
            {
                IsValid = false,
                Message = "This promotion code has expired"
            });

        // Check total usage limit
        if (promo.MaxTotalUses.HasValue && promo.CurrentUses >= promo.MaxTotalUses)
            return Ok(new PromotionCodeValidationResult
            {
                IsValid = false,
                Message = "This promotion code has reached its usage limit"
            });

        // Check per-customer usage limit
        if (promo.MaxUsesPerCustomer.HasValue)
        {
            var customerUsageCount = await _context.PromotionCodeUsages
                .CountAsync(u => u.PromotionCodeId == promo.Id && u.CustomerId == userId);

            if (customerUsageCount >= promo.MaxUsesPerCustomer)
                return Ok(new PromotionCodeValidationResult
                {
                    IsValid = false,
                    Message = "You have already used this promotion code the maximum number of times"
                });
        }

        // Check first-time customer restriction
        if (promo.FirstTimeCustomersOnly)
        {
            var hasCompletedJobs = await _context.Jobs
                .AnyAsync(j => j.CustomerId == userId && j.Status == "completed");

            if (hasCompletedJobs)
                return Ok(new PromotionCodeValidationResult
                {
                    IsValid = false,
                    Message = "This promotion code is only valid for first-time customers"
                });
        }

        // Check minimum order value
        if (promo.MinOrderValue.HasValue && dto.OrderAmount < promo.MinOrderValue)
            return Ok(new PromotionCodeValidationResult
            {
                IsValid = false,
                Message = $"Minimum order value of ${promo.MinOrderValue:F2} required for this promotion code"
            });

        // Check vehicle type restriction
        if (!string.IsNullOrEmpty(promo.VehicleType) &&
            !string.IsNullOrEmpty(dto.VehicleType) &&
            promo.VehicleType != dto.VehicleType)
            return Ok(new PromotionCodeValidationResult
            {
                IsValid = false,
                Message = $"This promotion code is only valid for {promo.VehicleType} vehicles"
            });

        // Calculate discount
        decimal discountAmount = 0;
        if (promo.DiscountType == "percentage")
        {
            discountAmount = dto.OrderAmount * (promo.DiscountValue / 100);
            if (promo.MaxDiscountAmount.HasValue && discountAmount > promo.MaxDiscountAmount)
            {
                discountAmount = promo.MaxDiscountAmount.Value;
            }
        }
        else if (promo.DiscountType == "fixed_amount")
        {
            discountAmount = promo.DiscountValue;
        }

        return Ok(new PromotionCodeValidationResult
        {
            IsValid = true,
            Message = "Promotion code applied successfully",
            PromotionCodeId = promo.Id,
            Code = promo.Code,
            Description = promo.Description,
            DiscountType = promo.DiscountType,
            DiscountValue = promo.DiscountValue,
            DiscountAmount = discountAmount,
            FinalAmount = Math.Max(0, dto.OrderAmount - discountAmount)
        });
    }

    /// <summary>
    /// Apply a promotion code (record usage)
    /// </summary>
    [HttpPost("apply")]
    [ProducesResponseType(typeof(PromotionCodeUsage), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PromotionCodeUsage>> ApplyPromoCode([FromBody] ApplyPromoCodeDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var promo = await _context.PromotionCodes
            .FirstOrDefaultAsync(p => p.Id == dto.PromotionCodeId);

        if (promo == null)
            return NotFound("Promotion code not found");

        // Create usage record
        var usage = new PromotionCodeUsage
        {
            PromotionCodeId = dto.PromotionCodeId,
            CustomerId = userId,
            JobId = dto.JobId,
            OriginalAmount = dto.OriginalAmount,
            DiscountAmount = dto.DiscountAmount,
            FinalAmount = dto.FinalAmount,
            UsedAt = DateTime.UtcNow
        };

        _context.PromotionCodeUsages.Add(usage);

        // Increment usage count
        promo.CurrentUses++;
        promo.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        await _activityLogService.LogActivityAsync(
            action: "promotion_code.applied",
            entityType: "PromotionCode",
            entityId: promo.Id.ToString(),
            entityName: promo.Code,
            description: $"Customer applied promotion code {promo.Code} - Discount: ${dto.DiscountAmount:F2}",
            severity: "INFO",
            metadata: new Dictionary<string, object>
            {
                { "CustomerId", userId },
                { "Code", promo.Code },
                { "OriginalAmount", dto.OriginalAmount },
                { "DiscountAmount", dto.DiscountAmount },
                { "FinalAmount", dto.FinalAmount },
                { "JobId", dto.JobId?.ToString() ?? "N/A" }
            }
        );

        return Ok(usage);
    }

    /// <summary>
    /// Get all promotion codes (Admin only)
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(typeof(List<PromotionCodeDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<PromotionCodeDto>>> GetPromoCodes(
        [FromQuery] bool? activeOnly = null)
    {
        var query = _context.PromotionCodes.AsQueryable();

        if (activeOnly == true)
            query = query.Where(p => p.IsActive);

        var promoCodes = await query
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        return Ok(promoCodes.Select(MapToDto).ToList());
    }

    /// <summary>
    /// Get a specific promotion code (Admin only)
    /// </summary>
    [HttpGet("{id}")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(typeof(PromotionCodeDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<PromotionCodeDto>> GetPromoCode(Guid id)
    {
        var promo = await _context.PromotionCodes.FindAsync(id);
        if (promo == null)
            return NotFound();

        return Ok(MapToDto(promo));
    }

    /// <summary>
    /// Create a new promotion code (Admin only)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(typeof(PromotionCodeDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<PromotionCodeDto>> CreatePromoCode([FromBody] CreatePromotionCodeDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        // Check if code already exists
        var existingCode = await _context.PromotionCodes
            .FirstOrDefaultAsync(p => p.Code.ToLower() == dto.Code.ToLower());

        if (existingCode != null)
            return BadRequest("A promotion code with this code already exists");

        var promo = new PromotionCode
        {
            Code = dto.Code.ToUpper(),
            Description = dto.Description,
            DiscountType = dto.DiscountType,
            DiscountValue = dto.DiscountValue,
            MaxDiscountAmount = dto.MaxDiscountAmount,
            MinOrderValue = dto.MinOrderValue,
            MaxTotalUses = dto.MaxTotalUses,
            MaxUsesPerCustomer = dto.MaxUsesPerCustomer,
            ValidFrom = dto.ValidFrom,
            ValidUntil = dto.ValidUntil,
            VehicleType = dto.VehicleType,
            CustomerType = dto.CustomerType,
            FirstTimeCustomersOnly = dto.FirstTimeCustomersOnly,
            IsActive = dto.IsActive ?? true,
            CreatedBy = userId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.PromotionCodes.Add(promo);
        await _context.SaveChangesAsync();

        await _activityLogService.LogActivityAsync(
            action: "promotion_code.created",
            entityType: "PromotionCode",
            entityId: promo.Id.ToString(),
            entityName: promo.Code,
            description: $"Created promotion code {promo.Code}",
            severity: "INFO"
        );

        return CreatedAtAction(nameof(GetPromoCode), new { id = promo.Id }, MapToDto(promo));
    }

    /// <summary>
    /// Update a promotion code (Admin only)
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(typeof(PromotionCodeDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<PromotionCodeDto>> UpdatePromoCode(Guid id, [FromBody] UpdatePromotionCodeDto dto)
    {
        var promo = await _context.PromotionCodes.FindAsync(id);
        if (promo == null)
            return NotFound();

        // Update fields
        if (dto.Description != null) promo.Description = dto.Description;
        if (dto.DiscountValue.HasValue) promo.DiscountValue = dto.DiscountValue.Value;
        if (dto.MaxDiscountAmount.HasValue) promo.MaxDiscountAmount = dto.MaxDiscountAmount;
        if (dto.MinOrderValue.HasValue) promo.MinOrderValue = dto.MinOrderValue;
        if (dto.MaxTotalUses.HasValue) promo.MaxTotalUses = dto.MaxTotalUses;
        if (dto.MaxUsesPerCustomer.HasValue) promo.MaxUsesPerCustomer = dto.MaxUsesPerCustomer;
        if (dto.ValidFrom.HasValue) promo.ValidFrom = dto.ValidFrom;
        if (dto.ValidUntil.HasValue) promo.ValidUntil = dto.ValidUntil;
        if (dto.IsActive.HasValue) promo.IsActive = dto.IsActive.Value;

        promo.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        await _activityLogService.LogActivityAsync(
            action: "promotion_code.updated",
            entityType: "PromotionCode",
            entityId: promo.Id.ToString(),
            entityName: promo.Code,
            description: $"Updated promotion code {promo.Code}",
            severity: "INFO"
        );

        return Ok(MapToDto(promo));
    }

    /// <summary>
    /// Delete a promotion code (Admin only)
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult> DeletePromoCode(Guid id)
    {
        var promo = await _context.PromotionCodes.FindAsync(id);
        if (promo == null)
            return NotFound();

        _context.PromotionCodes.Remove(promo);
        await _context.SaveChangesAsync();

        await _activityLogService.LogActivityAsync(
            action: "promotion_code.deleted",
            entityType: "PromotionCode",
            entityId: promo.Id.ToString(),
            entityName: promo.Code,
            description: $"Deleted promotion code {promo.Code}",
            severity: "INFO"
        );

        return NoContent();
    }

    private static PromotionCodeDto MapToDto(PromotionCode promo)
    {
        return new PromotionCodeDto
        {
            Id = promo.Id,
            Code = promo.Code,
            Description = promo.Description,
            DiscountType = promo.DiscountType,
            DiscountValue = promo.DiscountValue,
            MaxDiscountAmount = promo.MaxDiscountAmount,
            MinOrderValue = promo.MinOrderValue,
            MaxTotalUses = promo.MaxTotalUses,
            MaxUsesPerCustomer = promo.MaxUsesPerCustomer,
            CurrentUses = promo.CurrentUses,
            ValidFrom = promo.ValidFrom,
            ValidUntil = promo.ValidUntil,
            VehicleType = promo.VehicleType,
            CustomerType = promo.CustomerType,
            FirstTimeCustomersOnly = promo.FirstTimeCustomersOnly,
            IsActive = promo.IsActive,
            CreatedAt = promo.CreatedAt,
            UpdatedAt = promo.UpdatedAt
        };
    }
}

#region DTOs

public class ValidatePromoCodeDto
{
    public required string Code { get; set; }
    public decimal OrderAmount { get; set; }
    public string? VehicleType { get; set; }
}

public class ApplyPromoCodeDto
{
    public Guid PromotionCodeId { get; set; }
    public Guid? JobId { get; set; }
    public decimal OriginalAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal FinalAmount { get; set; }
}

public class PromotionCodeValidationResult
{
    public bool IsValid { get; set; }
    public string Message { get; set; } = "";
    public Guid? PromotionCodeId { get; set; }
    public string? Code { get; set; }
    public string? Description { get; set; }
    public string? DiscountType { get; set; }
    public decimal? DiscountValue { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal FinalAmount { get; set; }
}

public class CreatePromotionCodeDto
{
    public required string Code { get; set; }
    public string? Description { get; set; }
    public required string DiscountType { get; set; }
    public decimal DiscountValue { get; set; }
    public decimal? MaxDiscountAmount { get; set; }
    public decimal? MinOrderValue { get; set; }
    public int? MaxTotalUses { get; set; }
    public int? MaxUsesPerCustomer { get; set; }
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidUntil { get; set; }
    public string? VehicleType { get; set; }
    public string? CustomerType { get; set; }
    public bool FirstTimeCustomersOnly { get; set; }
    public bool? IsActive { get; set; }
}

public class UpdatePromotionCodeDto
{
    public string? Description { get; set; }
    public decimal? DiscountValue { get; set; }
    public decimal? MaxDiscountAmount { get; set; }
    public decimal? MinOrderValue { get; set; }
    public int? MaxTotalUses { get; set; }
    public int? MaxUsesPerCustomer { get; set; }
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidUntil { get; set; }
    public bool? IsActive { get; set; }
}

public class PromotionCodeDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = "";
    public string? Description { get; set; }
    public string DiscountType { get; set; } = "";
    public decimal DiscountValue { get; set; }
    public decimal? MaxDiscountAmount { get; set; }
    public decimal? MinOrderValue { get; set; }
    public int? MaxTotalUses { get; set; }
    public int? MaxUsesPerCustomer { get; set; }
    public int CurrentUses { get; set; }
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidUntil { get; set; }
    public string? VehicleType { get; set; }
    public string? CustomerType { get; set; }
    public bool FirstTimeCustomersOnly { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

#endregion
