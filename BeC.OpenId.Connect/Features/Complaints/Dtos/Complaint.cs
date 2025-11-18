using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BeC.OpenId.Connect.Features.Complaints.Dtos;

/// <summary>
/// Formal complaints lodged by customers or drivers
/// </summary>
[Table("Complaints")]
public class Complaint
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(50)]
    public required string ComplaintNumber { get; set; }

    [Required]
    [MaxLength(100)]
    public required string ComplainantId { get; set; } // FK to AspNetUsers (person filing complaint)

    [Required]
    [MaxLength(100)]
    public required string ComplainantName { get; set; }

    [Required]
    [MaxLength(20)]
    public required string ComplainantType { get; set; } // customer, driver

    [MaxLength(255)]
    [EmailAddress]
    public string? ComplainantEmail { get; set; }

    // Who/what the complaint is about
    [MaxLength(100)]
    public string? SubjectId { get; set; } // FK to AspNetUsers or other entity

    [MaxLength(100)]
    public string? SubjectName { get; set; }

    [MaxLength(20)]
    public string? SubjectType { get; set; } // customer, driver, job, service, platform

    [Required]
    [MaxLength(200)]
    public required string Title { get; set; }

    [Required]
    [Column(TypeName = "nvarchar(max)")]
    public required string Description { get; set; }

    [Required]
    [MaxLength(50)]
    public required string Category { get; set; } // service_quality, driver_behavior, customer_behavior,
                                                    // payment_issue, safety_concern, property_damage,
                                                    // discrimination, harassment, fraud, other

    [Required]
    [MaxLength(20)]
    public string Severity { get; set; } = "medium"; // low, medium, high, critical

    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "submitted"; // submitted, under_review, investigating,
                                                       // resolved, dismissed, escalated

    // Related entities
    public Guid? JobId { get; set; }
    public Guid? PaymentId { get; set; }

    // Incident details
    public DateTime? IncidentDate { get; set; }

    [MaxLength(500)]
    public string? IncidentLocation { get; set; }

    // Evidence (JSON array of file URLs and descriptions)
    [Column(TypeName = "nvarchar(max)")]
    public string? Evidence { get; set; }

    // Witnesses (JSON array)
    [Column(TypeName = "nvarchar(max)")]
    public string? Witnesses { get; set; }

    // Investigation
    [MaxLength(100)]
    public string? AssignedToId { get; set; } // FK to AspNetUsers (investigator)

    [MaxLength(100)]
    public string? AssignedToName { get; set; }

    public DateTime? AssignedAt { get; set; }

    [Column(TypeName = "nvarchar(max)")]
    public string? InvestigationNotes { get; set; }

    // Resolution
    [Column(TypeName = "nvarchar(max)")]
    public string? Resolution { get; set; }

    [MaxLength(50)]
    public string? ResolutionType { get; set; } // refund_issued, warning_given, account_suspended,
                                                  // no_action_required, policy_clarified, training_provided

    public DateTime? ResolvedAt { get; set; }

    [MaxLength(100)]
    public string? ResolvedBy { get; set; }

    // Actions taken (JSON array)
    [Column(TypeName = "nvarchar(max)")]
    public string? ActionsTaken { get; set; }

    // Escalation
    public bool IsEscalated { get; set; } = false;
    public DateTime? EscalatedAt { get; set; }

    [MaxLength(500)]
    public string? EscalationReason { get; set; }

    // Follow-up
    public bool RequiresFollowUp { get; set; } = false;
    public DateTime? FollowUpDate { get; set; }

    [Column(TypeName = "nvarchar(max)")]
    public string? FollowUpNotes { get; set; }

    // Complainant satisfaction
    [Range(1, 5)]
    public int? SatisfactionRating { get; set; }

    [MaxLength(1000)]
    public string? ComplainantFeedback { get; set; }

    // Audit
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastActivityAt { get; set; }

    // Legal/compliance flags
    public bool IsLegalMatter { get; set; } = false;
    public bool IsConfidential { get; set; } = false;

    [Column(TypeName = "nvarchar(max)")]
    public string? LegalNotes { get; set; }
}
