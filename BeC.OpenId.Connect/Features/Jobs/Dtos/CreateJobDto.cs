using System.ComponentModel.DataAnnotations;

namespace BeC.OpenId.Connect.Features.Jobs.Dtos;

public class CreateJobDto
{
    [Required]
    [MaxLength(255)]
    public required string CustomerName { get; set; }

    [Required]
    [Phone]
    [MaxLength(20)]
    public required string CustomerPhone { get; set; }

    [Required]
    [EmailAddress]
    [MaxLength(255)]
    public required string CustomerEmail { get; set; }

    [Required]
    [MaxLength(50)]
    public required string JobType { get; set; } // local_move, long_distance, commercial, etc.

    [MaxLength(50)]
    public string? VehicleTypeRequired { get; set; } // van, cargo_van, small_truck, large_truck, etc.

    [Required]
    [MaxLength(20)]
    public string Priority { get; set; } = "normal"; // low, normal, high, urgent

    [Required]
    public DateTime ScheduledDate { get; set; }

    [Required]
    [MaxLength(10)]
    public required string ScheduledTime { get; set; } // HH:mm format

    public int EstimatedDuration { get; set; } // minutes

    [Required]
    public required string PickupLocation { get; set; } // JSON: { address, city, state, zip, lat?, lng? }

    [Required]
    public required string DeliveryLocation { get; set; } // JSON: { address, city, state, zip, lat?, lng? }

    public decimal? Distance { get; set; } // miles

    [Required]
    public required string Items { get; set; } // JSON array: [{ name, quantity, weight?, dimensions? }]

    public string? SpecialInstructions { get; set; }

    public string? InternalNotes { get; set; }

    // Optional: Assign to driver on creation
    public Guid? DriverId { get; set; }
}
