using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BeC.OpenId.Connect.Features.Customers.Dtos;

/// <summary>
/// Represents a customer's favorite driver
/// </summary>
[Table("FavoriteDrivers")]
public class FavoriteDriver
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public required string CustomerId { get; set; } // FK to AspNetUsers

    [Required]
    public Guid DriverId { get; set; } // FK to Drivers

    [MaxLength(500)]
    public string? Notes { get; set; } // Customer's private notes about this driver

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    [ForeignKey(nameof(DriverId))]
    public virtual BeC.OpenId.Connect.Features.Drivers.Dtos.Driver? Driver { get; set; }
}
