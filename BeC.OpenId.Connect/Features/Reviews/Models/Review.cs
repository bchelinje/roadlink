using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BeC.OpenId.Connect.Features.Drivers.Dtos;

namespace BeC.OpenId.Connect.Features.Reviews.Models;

[Table("Reviews")]
public class Review
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(100)]
    public required string ReviewerId { get; set; }

    [Required]
    [MaxLength(100)]
    public required string ReviewerName { get; set; }

    [Required]
    [MaxLength(20)]
    public required string ReviewerType { get; set; }

    [Required]
    [MaxLength(100)]
    public required string RevieweeId { get; set; }

    [Required]
    [MaxLength(100)]
    public required string RevieweeName { get; set; }

    [Required]
    [MaxLength(20)]
    public required string RevieweeType { get; set; }

    public Guid? JobId { get; set; }

    [Required]
    public Guid DriverId { get; set; }

    [Required]
    [MaxLength(255)]
    [EmailAddress]
    public required string CustomerEmail { get; set; }

    [Required]
    [MaxLength(100)]
    public required string CustomerName { get; set; }

    [Required]
    [Range(1, 5)]
    public int Rating { get; set; }

    [MaxLength(1000)]
    public string? Comment { get; set; }

    [Column(TypeName = "nvarchar(max)")]
    public string? Photos { get; set; }

    // Individual rating categories (optional, for more detailed feedback)
    [Range(1, 5)]
    public int? PunctualityRating { get; set; }

    [Range(1, 5)]
    public int? ProfessionalismRating { get; set; }

    [Range(1, 5)]
    public int? CareOfItemsRating { get; set; }

    [Range(1, 5)]
    public int? CommunicationRating { get; set; }

    // Review status
    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "published"; // published, flagged, removed

    // Response from driver (optional)
    [MaxLength(500)]
    public string? DriverResponse { get; set; }

    [MaxLength(1000)]
    public string? Response { get; set; }

    public DateTime? ResponseDate { get; set; }

    [MaxLength(100)]
    public string? RespondedBy { get; set; }

    // Flagging properties
    public bool IsFlagged { get; set; } = false;

    public DateTime? FlaggedDate { get; set; }

    [MaxLength(500)]
    public string? FlagReason { get; set; }

    [MaxLength(100)]
    public string? FlaggedBy { get; set; }

    // Moderation fields
    [MaxLength(1000)]
    public string? ModeratorNotes { get; set; }

    public bool IsHidden { get; set; } = false;

    [MaxLength(100)]
    public string? ModeratedBy { get; set; }

    public DateTime? ModeratedDate { get; set; }

    // Helpful votes
    public int HelpfulVotes { get; set; } = 0;

    // Audit
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey(nameof(JobId))]
    public virtual Job? Job { get; set; }

    [ForeignKey(nameof(DriverId))]
    public virtual Driver? Driver { get; set; }
}
