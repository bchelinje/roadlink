using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BeC.OpenId.Connect.Features.Drivers.Dtos;

namespace BeC.OpenId.Connect.Features.Payments.Dtos;

[Table("Payments")]
public class Payment
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(50)]
    public required string PaymentNumber { get; set; } // Unique payment reference

    // Customer
    [Required]
    public required string CustomerId { get; set; } // FK to AspNetUsers

    [Required]
    [MaxLength(255)]
    public required string CustomerName { get; set; }

    [Required]
    [MaxLength(255)]
    public required string CustomerEmail { get; set; }

    // Driver
    public Guid? DriverId { get; set; }

    [ForeignKey(nameof(DriverId))]
    public virtual Driver? Driver { get; set; }

    [MaxLength(255)]
    public string? DriverName { get; set; }

    // Job Reference
    public Guid? JobId { get; set; }

    [ForeignKey(nameof(JobId))]
    public virtual Job? Job { get; set; }

    [MaxLength(50)]
    public string? JobNumber { get; set; }

    // Amount Details
    [Required]
    [Column(TypeName = "decimal(10,2)")]
    public decimal Amount { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal? TipAmount { get; set; } = 0;

    [Column(TypeName = "decimal(10,2)")]
    public decimal PlatformFee { get; set; } = 0;

    [Column(TypeName = "decimal(10,2)")]
    public decimal DriverEarnings { get; set; } = 0;

    [Column(TypeName = "decimal(10,2)")]
    public decimal? RefundAmount { get; set; } = 0;

    [Required]
    [Column(TypeName = "decimal(10,2)")]
    public decimal TotalAmount { get; set; }

    [Required]
    [MaxLength(3)]
    public string Currency { get; set; } = "GBP";

    // Payment Method
    [Required]
    [MaxLength(50)]
    public required string PaymentMethod { get; set; } // card, bank_transfer, cash, wallet

    [MaxLength(100)]
    public string? PaymentMethodDetails { get; set; } // e.g., "Visa ****1234"

    // Status
    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "pending"; // pending, processing, completed, failed, refunded, partially_refunded, cancelled

    // Payment Gateway Integration
    [MaxLength(100)]
    public string? StripePaymentIntentId { get; set; }

    [MaxLength(100)]
    public string? StripeChargeId { get; set; }

    [MaxLength(100)]
    public string? StripeRefundId { get; set; }

    // Transaction Details
    public DateTime? PaidAt { get; set; }
    public DateTime? RefundedAt { get; set; }
    public DateTime? PayoutProcessedAt { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }

    // Failure Information
    [MaxLength(100)]
    public string? FailureCode { get; set; }

    [MaxLength(500)]
    public string? FailureMessage { get; set; }

    // Metadata (JSON)
    [Column(TypeName = "nvarchar(max)")]
    public string? Metadata { get; set; }

    // Receipt
    public string? ReceiptUrl { get; set; }

    // Audit
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

[Table("Payouts")]
public class Payout
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(50)]
    public required string PayoutNumber { get; set; }

    // Driver
    [Required]
    public Guid DriverId { get; set; }

    [ForeignKey(nameof(DriverId))]
    public virtual Driver Driver { get; set; } = null!;

    [Required]
    [MaxLength(255)]
    public required string DriverName { get; set; }

    // Amount
    [Required]
    [Column(TypeName = "decimal(10,2)")]
    public decimal Amount { get; set; }

    [Required]
    [MaxLength(3)]
    public string Currency { get; set; } = "GBP";

    // Status
    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "pending"; // pending, processing, paid, failed, cancelled

    // Payment Method
    [Required]
    [MaxLength(50)]
    public required string PaymentMethod { get; set; } // bank_transfer, paypal, stripe_connect

    [MaxLength(255)]
    public string? BankAccountDetails { get; set; }

    // Integration
    [MaxLength(100)]
    public string? StripePayoutId { get; set; }

    [MaxLength(100)]
    public string? StripeAccountId { get; set; }

    // Period
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }

    // Payments included in this payout
    [Column(TypeName = "nvarchar(max)")]
    public string? PaymentIds { get; set; } // JSON array of payment IDs

    public int TotalJobs { get; set; } = 0;

    // Dates
    public DateTime? ProcessedAt { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime? FailedAt { get; set; }

    [MaxLength(500)]
    public string? FailureReason { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }

    // Audit
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
