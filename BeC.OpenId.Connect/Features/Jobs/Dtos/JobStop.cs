using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BeC.OpenId.Connect.Features.Jobs.Dtos;

/// <summary>
/// Represents a stop/location in a multi-stop delivery job
/// </summary>
[Table("JobStops")]
public class JobStop
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid JobId { get; set; }

    [Required]
    public int StopOrder { get; set; } // 1, 2, 3, etc.

    [Required]
    [MaxLength(20)]
    public required string StopType { get; set; } // pickup, delivery, waypoint

    // Location details
    [Required]
    [MaxLength(500)]
    public required string Location { get; set; }

    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    // Contact information
    [MaxLength(100)]
    public string? ContactName { get; set; }

    [MaxLength(20)]
    public string? ContactPhone { get; set; }

    [MaxLength(1000)]
    public string? SpecialInstructions { get; set; }

    // Items (JSON)
    [Column(TypeName = "nvarchar(max)")]
    public string? Items { get; set; }

    // Time windows
    public DateTime? ScheduledArrival { get; set; }
    public DateTime? ActualArrival { get; set; }
    public DateTime? ActualDeparture { get; set; }

    // Status
    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "pending"; // pending, arrived, completed, skipped, failed

    // Proof of delivery
    [Column(TypeName = "nvarchar(max)")]
    public string? Photos { get; set; } // JSON array of photo URLs

    public string? Signature { get; set; } // Signature image URL or data

    [MaxLength(1000)]
    public string? Notes { get; set; }

    // Audit
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    [ForeignKey(nameof(JobId))]
    public virtual Job? Job { get; set; }
}
