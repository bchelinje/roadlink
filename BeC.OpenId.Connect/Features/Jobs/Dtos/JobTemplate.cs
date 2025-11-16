using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BeC.OpenId.Connect.Features.Jobs.Dtos;

/// <summary>
/// Represents a reusable job template for repeat customers
/// </summary>
[Table("JobTemplates")]
public class JobTemplate
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public required string CustomerId { get; set; }

    [Required]
    [MaxLength(100)]
    public required string TemplateName { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    // Job configuration
    [Required]
    [MaxLength(50)]
    public required string JobType { get; set; }

    [MaxLength(50)]
    public string? VehicleTypeRequired { get; set; }

    [MaxLength(20)]
    public string? Priority { get; set; }

    // Locations
    [Required]
    [MaxLength(500)]
    public required string PickupLocation { get; set; }

    public double? PickupLatitude { get; set; }
    public double? PickupLongitude { get; set; }

    [Required]
    [MaxLength(500)]
    public required string DeliveryLocation { get; set; }

    public double? DeliveryLatitude { get; set; }
    public double? DeliveryLongitude { get; set; }

    public double? EstimatedDistance { get; set; }
    public int? EstimatedDuration { get; set; }

    // Items and details
    [Column(TypeName = "nvarchar(max)")]
    public string? Items { get; set; } // JSON array of items

    [MaxLength(1000)]
    public string? SpecialInstructions { get; set; }

    [MaxLength(1000)]
    public string? CustomerNotes { get; set; }

    // Multi-stop configuration (JSON)
    [Column(TypeName = "nvarchar(max)")]
    public string? StopsConfiguration { get; set; } // JSON array of stop definitions

    // Pricing
    public decimal? BasePrice { get; set; }

    // Usage tracking
    public int TimesUsed { get; set; } = 0;
    public DateTime? LastUsedDate { get; set; }

    // Tags for categorization
    [MaxLength(500)]
    public string? Tags { get; set; } // Comma-separated tags

    // Status
    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "active"; // active, archived

    public bool IsDefault { get; set; } = false;
    public bool IsShared { get; set; } = false; // For business accounts with multiple users

    // Audit
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
