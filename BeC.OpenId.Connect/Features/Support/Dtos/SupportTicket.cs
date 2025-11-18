using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BeC.OpenId.Connect.Features.Support.Dtos;

/// <summary>
/// Support ticket entity for customer support management
/// </summary>
[Table("SupportTickets")]
public class SupportTicket
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(50)]
    public required string TicketNumber { get; set; }

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
    [MaxLength(200)]
    public required string Subject { get; set; }

    [Required]
    [Column(TypeName = "nvarchar(max)")]
    public required string Description { get; set; }

    [Required]
    [MaxLength(50)]
    public required string Category { get; set; } // payment, job, driver, technical, account, other

    [Required]
    [MaxLength(20)]
    public string Priority { get; set; } = "medium"; // low, medium, high, urgent

    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "open"; // open, in_progress, waiting_customer, resolved, closed, cancelled

    // Related entities
    public Guid? JobId { get; set; }
    public Guid? DriverId { get; set; }
    public Guid? PaymentId { get; set; }

    // Assignment
    [MaxLength(100)]
    public string? AssignedToId { get; set; } // FK to AspNetUsers (support agent)

    [MaxLength(100)]
    public string? AssignedToName { get; set; }

    public DateTime? AssignedAt { get; set; }

    // Resolution
    [Column(TypeName = "nvarchar(max)")]
    public string? Resolution { get; set; }

    public DateTime? ResolvedAt { get; set; }

    [MaxLength(100)]
    public string? ResolvedBy { get; set; }

    public DateTime? ClosedAt { get; set; }

    [MaxLength(100)]
    public string? ClosedBy { get; set; }

    // Ratings
    [Range(1, 5)]
    public int? CustomerSatisfactionRating { get; set; }

    [MaxLength(1000)]
    public string? CustomerFeedback { get; set; }

    // SLA tracking
    public DateTime? FirstResponseAt { get; set; }
    public int? FirstResponseTimeMinutes { get; set; }
    public int? ResolutionTimeMinutes { get; set; }

    // Attachments (JSON array)
    [Column(TypeName = "nvarchar(max)")]
    public string? Attachments { get; set; }

    // Tags for categorization (JSON array)
    [Column(TypeName = "nvarchar(max)")]
    public string? Tags { get; set; }

    // Internal notes for support team
    [Column(TypeName = "nvarchar(max)")]
    public string? InternalNotes { get; set; }

    // Audit
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastActivityAt { get; set; }

    // Escalation
    public bool IsEscalated { get; set; } = false;
    public DateTime? EscalatedAt { get; set; }

    [MaxLength(500)]
    public string? EscalationReason { get; set; }
}
