using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BeC.OpenId.Connect.Features.Drivers.Dtos;
using BeC.OpenId.Connect.Features.Users.Dtos;

namespace BeC.OpenId.Connect.Features.Reviews.Dtos;

[Table("Reviews")]
public class Review
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    // Reviewer (who gives the review)
    [Required]
    public required string ReviewerId { get; set; } // FK to AspNetUsers

    [Required]
    [MaxLength(255)]
    public required string ReviewerName { get; set; }

    [Required]
    [MaxLength(50)]
    public required string ReviewerType { get; set; } // "customer" or "driver"

    // Reviewee (who receives the review)
    [Required]
    public required string RevieweeId { get; set; } // FK to AspNetUsers or DriverId

    [Required]
    [MaxLength(255)]
    public required string RevieweeName { get; set; }

    [Required]
    [MaxLength(50)]
    public required string RevieweeType { get; set; } // "customer" or "driver"

    // Job Reference
    public Guid? JobId { get; set; }

    [ForeignKey(nameof(JobId))]
    public virtual Job? Job { get; set; }

    // Review Content
    [Required]
    [Range(1, 5)]
    public int Rating { get; set; } // 1-5 stars

    [Required]
    [MaxLength(1000)]
    public required string Comment { get; set; }

    // Photos (JSON array)
    [Column(TypeName = "nvarchar(max)")]
    public string? Photos { get; set; }

    // Response from reviewee
    [MaxLength(1000)]
    public string? Response { get; set; }

    public DateTime? ResponseDate { get; set; }

    // Moderation
    [MaxLength(20)]
    public string Status { get; set; } = "active"; // active, reported, hidden, deleted

    public bool IsFlagged { get; set; } = false;

    [MaxLength(500)]
    public string? FlagReason { get; set; }

    public string? FlaggedBy { get; set; }
    public DateTime? FlaggedDate { get; set; }

    // Audit
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
