using System.ComponentModel.DataAnnotations;

namespace BeC.OpenId.Connect.Features.Drivers.Dtos;

public class EarningDto
{
    public Guid Id { get; set; }
    public Guid DriverId { get; set; }
    public Guid JobId { get; set; }
    public string JobNumber { get; set; } = string.Empty;
    public decimal BaseAmount { get; set; }
    public decimal? BonusAmount { get; set; }
    public decimal? TipAmount { get; set; }
    public decimal? DeductionAmount { get; set; }
    public decimal NetAmount { get; set; }
    public string PaymentStatus { get; set; } = string.Empty;
    public string? PaymentMethod { get; set; }
    public string? PaymentReference { get; set; }
    public DateTime? PaymentDate { get; set; }
    public DateTime? PaymentProcessedDate { get; set; }
    public DateTime JobCompletedDate { get; set; }
    public decimal? JobDistance { get; set; }
    public int? JobDuration { get; set; }
    public string? Notes { get; set; }
    public string? PaymentDetails { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateEarningDto
{
    [Required]
    public Guid JobId { get; set; }

    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Base amount must be greater than zero")]
    public decimal BaseAmount { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Bonus amount must be non-negative")]
    public decimal? BonusAmount { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Tip amount must be non-negative")]
    public decimal? TipAmount { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Deduction amount must be non-negative")]
    public decimal? DeductionAmount { get; set; }

    public string? Notes { get; set; }
}

public class UpdateEarningPaymentDto
{
    [Required]
    [MaxLength(20)]
    public required string PaymentStatus { get; set; } // pending, processing, paid, failed

    [MaxLength(100)]
    public string? PaymentMethod { get; set; }

    [MaxLength(100)]
    public string? PaymentReference { get; set; }

    public DateTime? PaymentDate { get; set; }

    public string? Notes { get; set; }
}
