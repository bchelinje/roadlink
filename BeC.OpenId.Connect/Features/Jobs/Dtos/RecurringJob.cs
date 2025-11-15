using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BeC.OpenId.Connect.Features.Jobs.Dtos;

/// <summary>
/// Represents a recurring/scheduled job template
/// </summary>
[Table("RecurringJobs")]
public class RecurringJob
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public required string CustomerId { get; set; }

    [Required]
    [MaxLength(100)]
    public required string Name { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    // Job details (same as regular job)
    [Required]
    [MaxLength(50)]
    public required string JobType { get; set; }

    [MaxLength(50)]
    public string? VehicleTypeRequired { get; set; }

    [MaxLength(20)]
    public string? Priority { get; set; }

    [Required]
    [MaxLength(500)]
    public required string PickupLocation { get; set; }

    [Required]
    [MaxLength(500)]
    public required string DeliveryLocation { get; set; }

    public double? Distance { get; set; }

    [Column(TypeName = "nvarchar(max)")]
    public string? Items { get; set; }

    [MaxLength(1000)]
    public string? SpecialInstructions { get; set; }

    // Recurrence settings
    [Required]
    [MaxLength(20)]
    public required string Frequency { get; set; } // daily, weekly, biweekly, monthly, custom

    // For weekly: JSON array like ["monday", "wednesday", "friday"]
    [Column(TypeName = "nvarchar(max)")]
    public string? RecurrenceDays { get; set; }

    // For monthly: day of month (e.g., 1, 15, 28)
    public int? DayOfMonth { get; set; }

    // Preferred time
    [MaxLength(10)]
    public string? PreferredTime { get; set; } // e.g., "09:00"

    // Duration settings
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int? OccurrenceCount { get; set; } // Alternative to end date

    // Status
    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "active"; // active, paused, completed, cancelled

    public DateTime? LastGeneratedDate { get; set; }
    public DateTime? NextScheduledDate { get; set; }

    public int JobsCreated { get; set; } = 0;

    // Linked template (optional)
    public Guid? TemplateId { get; set; }

    // Audit
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
