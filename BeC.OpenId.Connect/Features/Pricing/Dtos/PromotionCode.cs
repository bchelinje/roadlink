using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BeC.OpenId.Connect.Features.Pricing.Dtos;

/// <summary>
/// Promotion code entity for discounts and special offers
/// </summary>
[Table("PromotionCodes")]
public class PromotionCode
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(50)]
    public required string Code { get; set; }

    [MaxLength(200)]
    public string? Description { get; set; }

    // Discount type
    [Required]
    [MaxLength(20)]
    public required string DiscountType { get; set; } // percentage, fixed_amount

    // Discount value
    [Column(TypeName = "decimal(10,2)")]
    public decimal DiscountValue { get; set; }

    // Maximum discount (for percentage discounts)
    [Column(TypeName = "decimal(10,2)")]
    public decimal? MaxDiscountAmount { get; set; }

    // Minimum order value
    [Column(TypeName = "decimal(10,2)")]
    public decimal? MinOrderValue { get; set; }

    // Usage limits
    public int? MaxTotalUses { get; set; }
    public int? MaxUsesPerCustomer { get; set; }
    public int CurrentUses { get; set; } = 0;

    // Validity period
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidUntil { get; set; }

    // Restrictions
    [MaxLength(50)]
    public string? VehicleType { get; set; } // Specific vehicle type requirement

    [MaxLength(20)]
    public string? CustomerType { get; set; } // new, returning, vip

    public bool FirstTimeCustomersOnly { get; set; } = false;

    // Status
    public bool IsActive { get; set; } = true;

    // Audit
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string? CreatedBy { get; set; }
}

/// <summary>
/// Promotion code usage tracking
/// </summary>
[Table("PromotionCodeUsage")]
public class PromotionCodeUsage
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid PromotionCodeId { get; set; }

    [ForeignKey(nameof(PromotionCodeId))]
    public virtual PromotionCode PromotionCode { get; set; } = null!;

    [Required]
    public required string CustomerId { get; set; }

    public Guid? JobId { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal OriginalAmount { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal DiscountAmount { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal FinalAmount { get; set; }

    public DateTime UsedAt { get; set; } = DateTime.UtcNow;
}
