using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BeC.OpenId.Connect.Features.DataPrivacy.Dtos;

/// <summary>
/// Tracks user requests for account deletion (GDPR Right to be Forgotten)
/// </summary>
[Table("DataDeletionRequests")]
public class DataDeletionRequest
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(100)]
    public required string UserId { get; set; } // FK to AspNetUsers

    [Required]
    [MaxLength(100)]
    public required string UserName { get; set; }

    [Required]
    [MaxLength(255)]
    [EmailAddress]
    public required string UserEmail { get; set; }

    [Required]
    [MaxLength(20)]
    public required string UserType { get; set; } // customer, driver

    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "pending"; // pending, approved, rejected, completed, cancelled

    [Required]
    [MaxLength(20)]
    public string DeletionType { get; set; } = "soft"; // soft (anonymize), hard (complete removal)

    [Required]
    [Column(TypeName = "nvarchar(max)")]
    public required string Reason { get; set; }

    // Grace period (default 30 days)
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ScheduledDeletionDate { get; set; }
    public int GracePeriodDays { get; set; } = 30;

    // User confirmation
    public bool UserConfirmed { get; set; } = false;
    public DateTime? ConfirmedAt { get; set; }
    public string? ConfirmationToken { get; set; }

    // Data export
    public bool DataExportRequested { get; set; } = false;
    public bool DataExportCompleted { get; set; } = false;
    public DateTime? DataExportedAt { get; set; }
    public string? ExportFileUrl { get; set; }

    // Admin review
    [MaxLength(100)]
    public string? ReviewedBy { get; set; } // FK to AspNetUsers (admin)

    public DateTime? ReviewedAt { get; set; }

    [Column(TypeName = "nvarchar(max)")]
    public string? ReviewNotes { get; set; }

    [Column(TypeName = "nvarchar(max)")]
    public string? RejectionReason { get; set; }

    // Processing
    [MaxLength(100)]
    public string? ProcessedBy { get; set; } // FK to AspNetUsers (admin/system)

    public DateTime? ProcessedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    [Column(TypeName = "nvarchar(max)")]
    public string? ProcessingNotes { get; set; }

    // Related data counts (for auditing)
    public int? RelatedJobsCount { get; set; }
    public int? RelatedPaymentsCount { get; set; }
    public int? RelatedReviewsCount { get; set; }
    public int? RelatedMessagesCount { get; set; }

    // Audit
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Legal compliance
    public bool IsGDPRRequest { get; set; } = true;
    public string? LegalBasis { get; set; } = "GDPR Article 17 - Right to Erasure";

    [Column(TypeName = "nvarchar(max)")]
    public string? ComplianceNotes { get; set; }
}
