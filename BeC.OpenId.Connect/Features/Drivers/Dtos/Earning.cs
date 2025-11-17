// Models/Earning.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BeC.OpenId.Connect.Features.Drivers.Dtos;

[Table("Earnings")]
public class Earning
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid DriverId { get; set; }

    [ForeignKey(nameof(DriverId))]
    public virtual Driver Driver { get; set; } = null!;

    [Required]
    public Guid JobId { get; set; }

    [ForeignKey(nameof(JobId))]
    public virtual Job Job { get; set; } = null!;

    [Required]
    [MaxLength(50)]
    public required string JobNumber { get; set; }

    // Payment tracking
    public Guid? PaymentId { get; set; }
    public Guid? PayoutId { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime? EarnedAt { get; set; }

    // Earning Details
    [Required]
    [Column(TypeName = "decimal(10,2)")]
    public decimal BaseAmount { get; set; } // Base payment for the job

    [Column(TypeName = "decimal(10,2)")]
    public decimal? BonusAmount { get; set; } // Any bonus or incentive

    [Column(TypeName = "decimal(10,2)")]
    public decimal? TipAmount { get; set; } // Customer tips

    [Column(TypeName = "decimal(10,2)")]
    public decimal? DeductionAmount { get; set; } // Any deductions (fees, etc.)

    [Required]
    [Column(TypeName = "decimal(10,2)")]
    public decimal NetAmount { get; set; } // Final amount after bonuses/deductions

    // Payment Information
    [Required]
    [MaxLength(20)]
    public string PaymentStatus { get; set; } = "pending"; // pending, processing, paid, failed

    [MaxLength(100)]
    public string? PaymentMethod { get; set; } // bank_transfer, paypal, etc.

    [MaxLength(100)]
    public string? PaymentReference { get; set; } // Transaction ID or reference

    public DateTime? PaymentDate { get; set; }
    public DateTime? PaymentProcessedDate { get; set; }

    // Job Context
    [Required]
    public DateTime JobCompletedDate { get; set; } // When the job was completed

    [Column(TypeName = "decimal(10,2)")]
    public decimal? JobDistance { get; set; } // Distance covered

    public int? JobDuration { get; set; } // Duration in minutes

    // Metadata
    [Column(TypeName = "nvarchar(max)")]
    public string? Notes { get; set; }

    [Column(TypeName = "nvarchar(max)")]
    public string? PaymentDetails { get; set; } // Additional payment info as JSON

    // Audit
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
